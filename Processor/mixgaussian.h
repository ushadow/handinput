#pragma once
#include "pcheader.h"
#include "gaussian.h"

namespace handinput {
  class PROCESSOR_API MixGaussian {
  public:
    // mix: makes a copy of mix vector.
    MixGaussian(const Eigen::Ref<const Eigen::VectorXf> mix, 
                std::vector<std::unique_ptr<const Gaussian>>& gaussians);
    int n_mixtures() const { return (int) mix_.size(); }
    int feature_len() const { return feature_len_; }
    const Eigen::VectorXf* mix() const { return &mix_; }
    const Gaussian* GaussianAt(int index) const;

    double Prob(const Eigen::Ref<const Eigen::VectorXf> x) const;
  private:
    Eigen::VectorXf mix_;
    std::vector<std::unique_ptr<const Gaussian>> gaussians_;
    int feature_len_;

    MixGaussian(const MixGaussian&) {}
    MixGaussian& operator=(const MixGaussian&) { return *this; }
    void CheckRI();
  };
}