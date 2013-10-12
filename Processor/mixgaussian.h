#pragma once
#include "pcheader.h"
#include "gaussian.h"

namespace handinput {
  class MixGaussian {
  public:
    // mix: makes a copy of mix vector.
    MixGaussian(const Eigen::Ref<const Eigen::VectorXf> mix, 
                std::vector<std::unique_ptr<const Gaussian>>& gaussians);
    float Prob(const Eigen::Ref<const Eigen::VectorXf> x) const;
  private:
    Eigen::VectorXf mix_;
    std::vector<std::unique_ptr<const Gaussian>> gaussians_;

    MixGaussian(const MixGaussian&) {}
    MixGaussian(MixGaussian&&) {}
    MixGaussian& operator=(const MixGaussian&) { return *this; }
    MixGaussian& operator=(MixGaussian&&) { return *this; }
  };
}