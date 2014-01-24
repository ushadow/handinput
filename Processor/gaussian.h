#pragma once
#include "pcheader.h"

namespace handinput {
  class PROCESSOR_API Gaussian {
  public:

    // mean: makes a copy of mean.
    // cov : creates a inverse of cov.
    Gaussian(const Eigen::Ref<const Eigen::VectorXf> mean, 
             const Eigen::Ref<const Eigen::MatrixXf> cov);
    const Eigen::VectorXf* mean() const { return &mean_; }
    const Eigen::MatrixXf* inv_cov() const { return &inv_cov_; }

    double Prob(const Eigen::Ref<const Eigen::VectorXf> v) const;
  private:
    Eigen::VectorXf mean_;
    Eigen::MatrixXf inv_cov_;
    float b_;

    void CheckRI();
  };
}