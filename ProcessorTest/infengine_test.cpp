#include <stdlib.h>
#include "gtest\gtest.h"
#include "infengine.h"

#define ABS_ERROR 0.0001

static const std::string kModelFile = "../../../data/model.mat";

TEST(InfEngineTest, Initialization) {
  using Eigen::VectorXf;
  using Eigen::MatrixXf;
  using handinput::MixGaussian;
  using handinput::Gaussian;

  static const int kDescriptorLen = 2025;
  static const int N_PRINCIPAL_COMPS = 23;
  static const int FEATURE_LEN = N_PRINCIPAL_COMPS + 9;
  static const int N_STATES = 12;
  static const int N_MIXTURES = 1;

  char full[_MAX_PATH];
  _fullpath(full, kModelFile.c_str(), _MAX_PATH);

  handinput::InfEngine engine(full);
  ASSERT_EQ(kDescriptorLen, engine.descriptor_len());
  ASSERT_EQ(N_PRINCIPAL_COMPS, engine.n_principal_comps());
  ASSERT_EQ(FEATURE_LEN, engine.feature_len());
  const VectorXf* pca_mean = engine.pca_mean();
  ASSERT_EQ(2025, pca_mean->size());
  ASSERT_NEAR(0.0218, pca_mean->coeff(0), ABS_ERROR);
  ASSERT_NEAR(0.0045, pca_mean->coeff(kDescriptorLen - 1), ABS_ERROR);

  const MatrixXf* pc = engine.principal_comp();
  ASSERT_EQ(N_PRINCIPAL_COMPS, pc->rows());
  ASSERT_EQ(kDescriptorLen, pc->cols());
  ASSERT_NEAR(-0.0175, pc->coeff(0, 0), ABS_ERROR);
  ASSERT_NEAR(0.0014, pc->coeff(0, kDescriptorLen - 1), ABS_ERROR);
  ASSERT_NEAR(0.0062, pc->coeff(N_PRINCIPAL_COMPS - 1, kDescriptorLen - 1), ABS_ERROR);

  const VectorXf* std_mu = engine.std_mu();
  ASSERT_EQ(FEATURE_LEN, std_mu->size());
  ASSERT_NEAR(0.0549, std_mu->coeff(0), ABS_ERROR);
  ASSERT_NEAR(0, std_mu->coeff(FEATURE_LEN - 1), ABS_ERROR);

  const VectorXf* std_sigma = engine.std_sigma();
  ASSERT_EQ(FEATURE_LEN, std_sigma->size());
  ASSERT_NEAR(0.3170, std_sigma->coeff(0), ABS_ERROR);
  ASSERT_NEAR(0.3882, std_sigma->coeff(FEATURE_LEN - 1), ABS_ERROR);

  const handinput::HMM* hmm = engine.hmm();
  ASSERT_EQ(N_STATES, hmm->n_states());
  const VectorXf* prior = hmm->prior();
  ASSERT_EQ(N_STATES, prior->size());
  ASSERT_NEAR(0, prior->coeff(0), ABS_ERROR);
  ASSERT_NEAR(0.1667, prior->coeff(7), ABS_ERROR);

  const MatrixXf* transmat = hmm->transmat_t();
  ASSERT_EQ(N_STATES, transmat->rows());
  ASSERT_EQ(N_STATES, transmat->cols());
  ASSERT_NEAR(0, transmat->coeff(0, 0), ABS_ERROR);
  ASSERT_NEAR(0, transmat->coeff(0, N_STATES - 1), ABS_ERROR);
  ASSERT_NEAR(0, transmat->coeff(N_STATES - 1, 0), ABS_ERROR);
  ASSERT_NEAR(0.5714, transmat->coeff(N_STATES - 1, N_STATES - 1), ABS_ERROR);
  const MixGaussian* mixgaussian = hmm->MixGaussianAt(0);
  ASSERT_EQ(N_MIXTURES, mixgaussian->n_mixtures());
  const VectorXf* mix = mixgaussian->mix();
  ASSERT_NEAR(1, mix->coeff(0), ABS_ERROR);
  const Gaussian* gaussian = mixgaussian->GaussianAt(0);
  const VectorXf* mean = gaussian->mean();
  ASSERT_NEAR(0, mean->coeff(0), ABS_ERROR);
  ASSERT_NEAR(0, mean->coeff(FEATURE_LEN - 1), ABS_ERROR);
  const MatrixXf* inv_cov = gaussian->inv_cov();
  ASSERT_NEAR(100, inv_cov->coeff(0, 0), ABS_ERROR);
  ASSERT_NEAR(100, inv_cov->coeff(FEATURE_LEN - 1, FEATURE_LEN - 1), ABS_ERROR);

  mixgaussian = hmm->MixGaussianAt(N_STATES - 1);
  gaussian = mixgaussian->GaussianAt(0);
  mean = gaussian->mean();
  inv_cov = gaussian->inv_cov();
  ASSERT_NEAR(-0.6144, mean->coeff(0), ABS_ERROR);
  ASSERT_NEAR(-0.0747, mean->coeff(FEATURE_LEN - 1), ABS_ERROR);
  ASSERT_NEAR(6.5641, inv_cov->coeff(0, 0), ABS_ERROR);
  ASSERT_NEAR(1.0835, inv_cov->coeff(FEATURE_LEN - 1, FEATURE_LEN - 1), ABS_ERROR);
}
