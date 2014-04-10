#include "pcheader.h"
#include "hmm.h"

namespace handinput {
#define EPS 10e-32

  const double HMM::kMinGamma = 10e-17;

  HMM* HMM::CreateFromMxArray(mxArray* mx_model, int lag) {
    using std::vector;
    using std::unique_ptr;
    using Eigen::Map;
    using Eigen::VectorXd;
    using Eigen::MatrixXd;
    using Eigen::VectorXf;
    using Eigen::MatrixXf;
    using Eigen::InnerStride;
    using std::vector;
    using std::string;

    mxArray* mx_hmm_model = mxGetField(mx_model, 0, "model");
    mxArray* mx_prior = mxGetField(mx_hmm_model, 0, "prior");
    mxArray* mx_transmat = mxGetField(mx_hmm_model, 0, "transmat");
    mxArray* mx_mu = mxGetField(mx_hmm_model, 0, "mu");
    mxArray* mx_sigma = mxGetField(mx_hmm_model, 0, "Sigma");
    mxArray* mx_mixmat = mxGetField(mx_hmm_model, 0, "mixmat");
    mxArray* mx_map = mxGetField(mx_hmm_model, 0, "labelMap");
    mxArray* mx_stage_map = mxGetField(mx_hmm_model, 0, "stageMap");

    const double* prior_data = (const double*) mxGetData(mx_prior);
    const double* transmat_data = (const double*) mxGetData(mx_transmat);
    float* mixmat_data = (float*) mxGetData(mx_mixmat);
    float* mu_data = (float*) mxGetData(mx_mu);
    float* sigma_data = (float*) mxGetData(mx_sigma);

    int n_states = (int) mxGetNumberOfElements(mx_prior);
    // Get number of columns.
    int n_mixtures = (int) mxGetN(mx_mixmat);
    size_t feature_len = mxGetM(mx_mu);
    size_t sigma_len = feature_len * feature_len;

    const int* map_data = (const int*) mxGetData(mx_map);
    size_t map_len = mxGetNumberOfElements(mx_map);
    vector<int> map(map_data, map_data + map_len);

    vector<string> stage_map;
    for (int i = 0; i < mxGetNumberOfElements(mx_stage_map); i++) {
      string s = string(mxArrayToString(mxGetCell(mx_stage_map, i)));
      stage_map.push_back(s);
    }

    vector<unique_ptr<const MixGaussian>> mixgaussians;
    for (int i = 0; i < n_states; i++) {
      // mixmat_data is n_states x n_mixtures matrix where each row is the mixture probability for 
      // a state.
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
      MatrixXd::Map(transmat_data, n_states, n_states), mixgaussians, map, stage_map, lag + 1);
  }

  HMM::HMM(const Eigen::Ref<const Eigen::VectorXd> prior, 
    Eigen::MatrixXd transmat, 
    std::vector<std::unique_ptr<const MixGaussian>>& mixgaussians, 
    const std::vector<int> state_to_label_map, 
    const std::vector<std::string> stage_map, int smooth_win) 
    : mixgaussians_(std::move(mixgaussians)), state_to_label_map_(state_to_label_map),
    stage_map_(std::move(stage_map)), smooth_win_(smooth_win), transmat_(transmat) {

      using Eigen::VectorXd;

      n_states_ = (int) prior.size();
      rest_state_ = n_states_ - 1;
      feature_len_ = mixgaussians_[0]->feature_len();
      prior_ = prior;
      transmat_t_ = transmat.transpose();
      loglik_ = 0;

      Reset();

      CheckRI();
  }

  // Resets most likely state to rest state.
  void HMM::Reset() {
#ifdef _DEBUG
    std::cout << "HMM reset." << std::endl;
#endif
    alpha_.clear();
    obslik_.clear();
    most_likely_hidden_state_ = rest_state_;
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

  void HMM::Fwdback(const Eigen::Ref<const Eigen::VectorXf> x) {
    using Eigen::VectorXd;

    ComputeObslik(x);

    VectorXd alpha1;
    if (alpha_.size() <= 0) {
      alpha1 = prior_.cwiseProduct(obslik_.back());
      reset_ = false;
    } else {
      alpha1 = (transmat_t_ * alpha_.back()).cwiseProduct(obslik_.back());
    }

    double norm = Normalize(&alpha1);

    if (norm <= EPS) {
      Reset();
    } else {
      if (alpha_.size() >= smooth_win_)
        alpha_.pop_front();
      alpha_.push_back(std::move(alpha1));

      Back();
    }
  }

  void HMM::Back() {
    using Eigen::VectorXd;

    VectorXd beta = VectorXd::Ones(n_states_);
    for (std::deque<VectorXd>::const_reverse_iterator rit = obslik_.rbegin(); 
      rit != obslik_.rend() - 1; rit++) {
        beta = transmat_ * (beta.cwiseProduct(*rit));
        double norm = Normalize(&beta);
        if (norm <= EPS) {
          Reset();
          return;
        }
    }
    gamma_ = alpha_.front().cwiseProduct(beta);
    ComputeMostLikelyState();
#ifdef _DEBUG
    std::cout << "gamma = " << std::endl;
    std::cout << gamma_ << std::endl;
    std::cout << "most likely state = " << most_likely_hidden_state_ << std::endl;
#endif
  }

  void HMM::ComputeMostLikelyState() {
    using Eigen::VectorXf;
    VectorXf::Index maxIndex;
    double max_coeff = gamma_.maxCoeff(&maxIndex);
    if (max_coeff > kMinGamma) {
      most_likely_hidden_state_ = (int) maxIndex;
    } else {
      Reset();
    }
  }

  int HMM::MostLikelyLabelIndex() {
    return state_to_label_map_[most_likely_hidden_state_];
  }

  std::string HMM::MostLikelyStage() {
    return stage_map_[most_likely_hidden_state_];
  }

  void HMM::ComputeObslik(const Eigen::Ref<const Eigen::VectorXf> x) {
    using Eigen::VectorXd;

    VectorXd obs1 = VectorXd::Zero(n_states_);
    for (int i = 0; i < n_states_; i++) {
      obs1(i) = mixgaussians_[i]->Prob(x);
    }

    if (obslik_.size() >= smooth_win_) {
      obslik_.pop_front();
    }
    obslik_.push_back(std::move(obs1));
  }

  // Normalizes the alpha.
  double HMM::Normalize(Eigen::VectorXd* x) {
    double norm = x->norm();
    if (norm > EPS) {
      x->normalize();
    }
    return norm;
  }
}