#pragma once
#include "pcheader.h"
#include "mixgaussian.h"

namespace handinput {
  class HMM {
  public:
    HMM(const Eigen::Ref<const Eigen::VectorXf> prior, 
      const Eigen::Ref<const Eigen::MatrixXf> transmat, 
      std::vector<const MixGaussian*> mixgaussians);
    void Fwdback(const Eigen::Ref<const Eigen::VectorXf> x);
  private:
    Eigen::VectorXf alpha_, prior_;
    std::vector<const MixGaussian*> mixgaussians_;
    int n_states_;
    Eigen::VectorXf obslik_;
    Eigen::MatrixXf transmat_t_;
    float loglik_;

    void ComputeObslik(const Eigen::Ref<const Eigen::VectorXf> x);
    float Normalize();
  };
}