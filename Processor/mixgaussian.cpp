#include "mixgaussian.h"

namespace handinput {
  MixGaussian::MixGaussian(const Eigen::Ref<const Eigen::VectorXf> mix, 
        const std::vector<const Gaussian*> gaussians) {
    gaussians_ = gaussians;
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