#include "pcheader.h"
#include "hmm.h"

namespace handinput {
  HMM* HMM::CreateFromMxArray(mxArray* mx_model) {
    using std::vector;
    using std::unique_ptr;
    using Eigen::Map;
    using Eigen::VectorXd;
    using Eigen::MatrixXd;
    using Eigen::VectorXf;
    using Eigen::MatrixXf;
    using Eigen::InnerStride;

    mxArray* mx_hmm_model = mxGetField(mx_model, 0, "model");
    mxArray* mx_prior = mxGetField(mx_hmm_model, 0, "prior");
    mxArray* mx_transmat = mxGetField(mx_hmm_model, 0, "transmat");
    mxArray* mx_mu = mxGetField(mx_hmm_model, 0, "mu");
    mxArray* mx_sigma = mxGetField(mx_hmm_model, 0, "Sigma");
    mxArray* mx_mixmat = mxGetField(mx_hmm_model, 0, "mixmat");
    mxArray* mx_map = mxGetField(mx_hmm_model, 0, "map");

    const double* prior_data = (const double*) mxGetData(mx_prior);
    const double* transmat_data = (const double*) mxGetData(mx_transmat);
    float* mixmat_data = (float*) mxGetData(mx_mixmat);
    float* mu_data = (float*) mxGetData(mx_mu);
    float* sigma_data = (float*) mxGetData(mx_sigma);

    int n_states = (int) mxGetNumberOfElements(mx_prior);
    int n_mixtures = (int) mxGetN(mx_mixmat);
    size_t feature_len = mxGetM(mx_mu);
    size_t sigma_len = feature_len * feature_len;

    const int* map_data = (const int*) mxGetData(mx_map);
    size_t map_len = mxGetNumberOfElements(mx_map);
    std::vector<int> map(map_data, map_data + map_len);

    vector<unique_ptr<const MixGaussian>> mixgaussians;
    for (int i = 0; i < n_states; i++) {
      // mixmat_data is m x n matrix where each colum is the mixture probability for a state.
      Map<VectorXf, 0, InnerStride<>> mix(mixmat_data + i, n_mixtures, 
                                          InnerStride<>(n_states));  
      vector<unique_ptr<const Gaussian>> gaussians;
      for (int j = 0; j < n_mixtures; j++) {
        Map<VectorXf> mu(mu_data + feature_len * (j * n_states + i), feature_len);
        Map<MatrixXf> sigma(sigma_data + sigma_len * (j * n_states + i), feature_len, 
          feature_len);
        unique_ptr<const Gaussian> p(new Gaussian(mu, sigma));
        gaussians.push_back(std::move(p));
      }
      unique_ptr<const MixGaussian> mixgauss(new MixGaussian(mix, gaussians));
      mixgaussians.push_back(std::move(mixgauss));
    }
    return new HMM(VectorXd::Map(prior_data, n_states),
      MatrixXd::Map(transmat_data, n_states, n_states), map, mixgaussians);
  }

  HMM::HMM(const Eigen::Ref<const Eigen::VectorXd> prior, 
    const Eigen::Ref<const Eigen::MatrixXd> transmat, 
    const std::vector<int> state_to_label_map,
    std::vector<std::unique_ptr<const MixGaussian>>& mixgaussians) 
    : mixgaussians_(std::move(mixgaussians)), state_to_label_map_(state_to_label_map) {

      using Eigen::VectorXd;

      n_states_ = (int) prior.size();
      feature_len_ = mixgaussians_[0]->feature_len();
      obslik_ = VectorXd::Zero(n_states_);
      alpha_ = VectorXd::Zero(n_states_);
      prior_ = prior;
      transmat_t_ = transmat.transpose();
      loglik_ = 0;

      Reset();
      
      CheckRI();
  }

  void HMM::Reset() {
    alpha_.setZero();
    reset_ = true;
  }

  void HMM::CheckRI() {
    if (mixgaussians_.size() != n_states_ || transmat_t_.rows() != n_states_ ||
      transmat_t_.cols() != n_states_)
      throw std::invalid_argument("The input is not valid.");
  }

  const MixGaussian* HMM::MixGaussianAt(int index) const {
    if (index >= mixgaussians_.size())
      throw std::invalid_argument("The index exceeds the number of mixtures.");
    return mixgaussians_[index].get();
  }

  double HMM::Fwdback(const Eigen::Ref<const Eigen::VectorXf> x) {
    ComputeObslik(x);
    if (reset_) {
      alpha_ = prior_.cwiseProduct(obslik_);
      reset_ = false;
    } else {
      alpha_ = (transmat_t_ * alpha_).cwiseProduct(obslik_).eval();
    }
    double norm = Normalize();
    if (norm == 0)
      loglik_ = -FLT_MAX;
    else
      loglik_ += log(norm);
    return loglik_;
  }

  int HMM::MostLikelyState() {
    using Eigen::VectorXf;
    VectorXf::Index maxIndex;
    alpha_.maxCoeff(&maxIndex);
    return (int) maxIndex;
  }

  void HMM::ComputeObslik(const Eigen::Ref<const Eigen::VectorXf> x) {
    for (int i = 0; i < n_states_; i++) {
      obslik_(i) = mixgaussians_[i]->Prob(x);
    }
  }

  // Normalizes the alpha.
  double HMM::Normalize() {
    double norm = alpha_.norm();
    if (norm == 0) {
      norm = 1;
      reset_ = true;
    }
    alpha_ = alpha_ / norm;
    return norm;
  }
}