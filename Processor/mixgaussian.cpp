#include "pcheader.h"
#include "mixgaussian.h"

#define ABS_ERROR 1e-9

namespace handinput {
  // gaussians: the referenced object is moved to the member variable of MixGaussian and it becomes 
  //            in a unspecified state.
  MixGaussian::MixGaussian(const Eigen::Ref<const Eigen::VectorXf> mix, 
    std::vector<std::unique_ptr<const Gaussian>>& gaussians) : gaussians_(std::move(gaussians)) {
    mix_ = mix;
    feature_len_ = (int) gaussians_[0]->mean()->size();
    CheckRI();
  }

  const Gaussian* MixGaussian::GaussianAt(int index) const {
    if (index >= gaussians_.size())
      throw std::invalid_argument("The index exceeds the number of gaussians.");
    return gaussians_[index].get();
  }

  float MixGaussian::Prob(const Eigen::Ref<const Eigen::VectorXf> x) const {
    using std::vector;

    float sum = 0.0f;
    for (int i = 0; i < gaussians_.size(); i++) {
      sum += gaussians_[i]->Prob(x) * mix_(i);
    }
    return sum;
  }

  void MixGaussian::CheckRI() {
    if (mix_.size() != gaussians_.size())
      throw std::invalid_argument("Invalid arguments: number of mixtures is not consistent");
    if (abs(mix_.sum() - 1) > ABS_ERROR)
      throw std::invalid_argument("The sum of mixture probabilities is not 1.");
  }
}