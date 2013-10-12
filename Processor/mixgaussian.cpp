#include "mixgaussian.h"

namespace handinput {
  // gaussians: the referenced object is moved to the member variable of MixGaussian and it becomes 
  //            in a unspecified state.
  MixGaussian::MixGaussian(const Eigen::Ref<const Eigen::VectorXf> mix, 
    std::vector<std::unique_ptr<const Gaussian>>& gaussians) : gaussians_(std::move(gaussians)) {
    mix_ = mix;
  }

  float MixGaussian::Prob(const Eigen::Ref<const Eigen::VectorXf> x) const {
    using std::vector;

    float sum = 0.0f;
    for (int i = 0; i < gaussians_.size(); i++) {
      sum += gaussians_[i]->Prob(x) * mix_(i);
    }
    return sum;
  }
}