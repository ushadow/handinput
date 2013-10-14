#include "pcheader.h"
#include "gaussian.h"

namespace handinput {
#define EPS 1e-9f

  Gaussian::Gaussian(const Eigen::Ref<const Eigen::VectorXf> mean, 
                     const Eigen::Ref<const Eigen::MatrixXf> cov) {
    mean_ = mean;
    float d = sqrt(cov.determinant());
    int dim = (int) mean.size();
    b_ = 1.0f / (pow(2 * (float) M_PI, dim / 2.0f) * d + EPS);
    inv_cov_ = cov.inverse();
  }

  // p(x) = 1/sqrt((2pi)^k * det(cov))*exp(-0.5(x - mean)'cov^-1(x - mean))
  float Gaussian::Prob(const Eigen::Ref<const Eigen::VectorXf> x) const {
    using Eigen::VectorXf;
    if (x.size() != mean_.size())
      throw std::invalid_argument("The size of the input is incorrect.");
    VectorXf v = x - mean_;
    float mahal = (inv_cov_ * v).dot(v);
    return (float) (b_ * exp(-0.5 * mahal));
  }

  void Gaussian::CheckRI() {
    if (mean_.size() != inv_cov_.rows() || mean_.size() != inv_cov_.cols())
      throw std::invalid_argument("The input arguments are not valid.");
  }
}