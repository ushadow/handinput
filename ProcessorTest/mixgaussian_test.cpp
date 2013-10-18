#include "gtest\gtest.h"
#include "mixgaussian.h"

#define ABS_ERROR 0.0001

TEST(MixGaussianTest, ProbOneMixture) {
  using handinput::MixGaussian;
  using handinput::Gaussian;
  using Eigen::VectorXf;
  using Eigen::MatrixXf;
  using std::vector;
  using std::unique_ptr;

  VectorXf mean(1);
  mean << 0;
  MatrixXf cov(1, 1);
  cov << 1;

  VectorXf mix(1);
  mix << 1;
 
  vector<unique_ptr<const Gaussian>> gaussians;
  unique_ptr<const Gaussian> gaussian(new Gaussian(mean, cov));
  gaussians.push_back(std::move(gaussian));
  MixGaussian mixgaussian(mix, gaussians);

  ASSERT_NEAR(0.3989, mixgaussian.Prob(VectorXf::Zero(1)), ABS_ERROR);
}

TEST(MixGaussianTest, ProbThreeMixtures) {
  using handinput::MixGaussian;
  using handinput::Gaussian;
  using Eigen::VectorXf;
  using Eigen::MatrixXf;
  using std::vector;
  using std::unique_ptr;

  vector<unique_ptr<const Gaussian>> gaussians;
  for (int i = 0; i < 3; i++) {
    VectorXf mean(2);
    mean << (float) i, (float) i;
    MatrixXf cov(2, 2);
    cov << 2, 1, 1, 2;
    unique_ptr<const Gaussian> gaussian(new Gaussian(mean, cov));
    gaussians.push_back(std::move(gaussian));
  }
  

  VectorXf mix(3);
  mix << 0.5, 0.25, 0.25;
 
  MixGaussian mixgaussian(mix, gaussians);

  ASSERT_NEAR(0.0685, mixgaussian.Prob(VectorXf::Zero(2)), ABS_ERROR);
}