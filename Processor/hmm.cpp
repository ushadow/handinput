#include "pcheader.h"
#include "hmm.h"

namespace handinput {
  HMM::HMM(const Eigen::Ref<const Eigen::VectorXf> prior, 
    const Eigen::Ref<const Eigen::MatrixXf> transmat, std::vector<const MixGaussian*> mixgaussians) {
      using Eigen::VectorXf;
      mixgaussians_ = mixgaussians;
      n_states_ = mixgaussians.size();
      obslik_ = VectorXf::Zero(n_states_);
      prior_ = prior;
      transmat_t_ = transmat.transpose();
      loglik_ = 0;
  }

  void HMM::Fwdback(const Eigen::Ref<const Eigen::VectorXf> x) {
    ComputeObslik(x);
    if (alpha_.size() == 0) {
      alpha_ = prior_.cwiseProduct(x);
    } else {
      alpha_ = (transmat_t_ * alpha_).cwiseProduct(obslik_);
    }
    float norm = Normalize();
    if (norm == 0)
      loglik_ = -FLT_MAX;
    else
      loglik_ += log(norm);
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