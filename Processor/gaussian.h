#pragma once
#include "pcheader.h"

namespace handinput {
  class Gaussian {
  public:

    // mean: makes a copy of mean.
    // cov : creates a inverse of cov.
    Gaussian(const Eigen::Ref<const Eigen::VectorXf> mean, 
             const Eigen::Ref<const Eigen::MatrixXf> cov);
    float Prob(const Eigen::Ref<const Eigen::VectorXf> v) const;
  private:
    Eigen::VectorXf mean_;
    Eigen::MatrixXf inv_cov_;
    float b_;
  };
}