#pragma once
#include "pcheader.h"
#include "gaussian.h"

namespace handinput {
  class MixGaussian {
  public:
    MixGaussian(const Eigen::Ref<const Eigen::VectorXf> mix, 
                const std::vector<const Gaussian*> gaussians);
    float Prob(const Eigen::Ref<const Eigen::VectorXf> x) const;
  private:
    Eigen::VectorXf mix_;
    std::vector<const Gaussian*> gaussians_;
  };
}