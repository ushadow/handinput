#include "gtest\gtest.h"
#include "gaussian.h"

#define ABS_ERROR 0.0001

TEST(GaussianTest, ProbZeroOne) {
  using Eigen::VectorXf;
  using Eigen::MatrixXf;
  using handinput::Gaussian;

  VectorXf mean(1);
  mean << 0;

  MatrixXf cov(1, 1);
  cov << 1;

  Gaussian gaussian(mean, cov);
  ASSERT_NEAR(0.3989, gaussian.Prob(VectorXf::Zero(1)), ABS_ERROR);
}

TEST(GaussianTest, ProbZeroTwo) {
  using Eigen::VectorXf;
  using Eigen::MatrixXf;
  using handinput::Gaussian;

  VectorXf mean(2);
  mean << 0, 0;

  MatrixXf cov(2, 2);
  cov << 1, 0, 0, 1;

  Gaussian gaussian(mean, cov);
  ASSERT_NEAR(0.1592, gaussian.Prob(VectorXf::Zero(2)), ABS_ERROR);
}

TEST(GaussianTest, ProbNonZero) {
  using Eigen::VectorXf;
  using Eigen::MatrixXf;
  using handinput::Gaussian;

  VectorXf mean(2);
  mean << 2, 2;

  MatrixXf cov(2, 2);
  cov << 2, 1, 1, 2;

  Gaussian gaussian(mean, cov);
  ASSERT_NEAR(0.0658, gaussian.Prob(VectorXf::Ones(2)), ABS_ERROR);
}