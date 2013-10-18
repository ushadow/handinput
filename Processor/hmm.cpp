#include "pcheader.h"
#include "hmm.h"

namespace handinput {
  HMM* HMM::CreateFromMxArray(mxArray* mx_model) {
    using std::vector;
    using std::unique_ptr;
    using Eigen::Map;
    using Eigen::VectorXf;
    using Eigen::MatrixXf;

    mxArray* mx_hmm_model = mxGetField(mx_model, 0, "model");
    mxArray* mx_prior = mxGetField(mx_hmm_model, 0, "prior");
    mxArray* mx_transmat = mxGetField(mx_hmm_model, 0, "transmat");
    mxArray* mx_mu = mxGetField(mx_hmm_model, 0, "mu");
    mxArray* mx_sigma = mxGetField(mx_hmm_model, 0, "Sigma");
    mxArray* mx_mixmat = mxGetField(mx_hmm_model, 0, "mixmat");

    const float* prior_data = (const float*) mxGetData(mx_prior);
    const float* transmat_data = (const float*) mxGetData(mx_transmat);
    float* mixmat_data = (float*) mxGetData(mx_mixmat);
    float* mu_data = (float*) mxGetData(mx_mu);
    float* sigma_data = (float*) mxGetData(mx_sigma);

    int n_states = (int) mxGetNumberOfElements(mx_prior);
    int n_mixtures = (int) mxGetM(mx_mixmat);
    size_t feature_len = mxGetM(mx_mu);
    size_t sigma_len = feature_len * feature_len;

    vector<unique_ptr<const MixGaussian>> mixgaussians;
    for (int i = 0; i < n_states; i++) {
      // mixmat_data is m x n matrix where each colum is the mixture probability for a state.
      Map<VectorXf> mix(mixmat_data + i * n_mixtures, n_mixtures);  
      vector<unique_ptr<const Gaussian>> gaussians;
      for (int j = 0; j < n_mixtures; j++) {
        Map<VectorXf> mu(mu_data + feature_len * (i * n_mixtures + j), feature_len);
        Map<MatrixXf> sigma(sigma_data + sigma_len * (i * n_mixtures + j), feature_len, 
          feature_len);
        unique_ptr<const Gaussian> p(new Gaussian(mu, sigma));
        gaussians.push_back(std::move(p));
      }
      unique_ptr<const MixGaussian> mixgauss(new MixGaussian(mix, gaussians));
      mixgaussians.push_back(std::move(mixgauss));
    }
    return new HMM(VectorXf::Map(prior_data, n_states),
      MatrixXf::Map(transmat_data, n_states, n_states), mixgaussians);
  }

  HMM::HMM(const Eigen::Ref<const Eigen::VectorXf> prior, 
    const Eigen::Ref<const Eigen::MatrixXf> transmat, 
    std::vector<std::unique_ptr<const MixGaussian>>& mixgaussians) 
    : mixgaussians_(std::move(mixgaussians)) {

      using Eigen::VectorXf;

      n_states_ = (int) prior.size();
      feature_len_ = mixgaussians_[0]->feature_len();
      obslik_ = VectorXf::Zero(n_states_);
      prior_ = prior;
      transmat_t_ = transmat.transpose();
      loglik_ = 0;
      CheckRI();
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

  float HMM::Fwdback(const Eigen::Ref<const Eigen::VectorXf> x) {
    ComputeObslik(x);
    if (alpha_.size() == 0) {
      alpha_ = prior_.cwiseProduct(obslik_);
    } else {
      alpha_ = (transmat_t_ * alpha_).cwiseProduct(obslik_).eval();
    }
    float norm = Normalize();
    if (norm == 0)
      loglik_ = -FLT_MAX;
    else
      loglik_ += log(norm);
    return loglik_;
  }

  void HMM::ComputeObslik(const Eigen::Ref<const Eigen::VectorXf> x) {
    for (int i = 0; i < n_states_; i++) {
      obslik_(i) = mixgaussians_[i]->Prob(x);
    }
  }

  float HMM::Normalize() {
    float norm = alpha_.norm();
    if (norm == 0)
      norm = 1;
    alpha_ = alpha_ / norm;
    return norm;
  }
}