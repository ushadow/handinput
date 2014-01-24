#include <stdlib.h>
#include "gtest\gtest.h"
#include "infengine.h"

#define ABS_ERROR 0.0001

static const std::string kModelFile = "../../../data/model.mat";

TEST(InfEngineTest, InitializationSVM) {
  using std::vector;
  using std::string;

  static const std::string model = "../../../data/model_svm.mat";
  static const int kDescriptionLen = 441;
  static const int kNumGestures = 5;
  static const string kGestureLabels[kNumGestures] = {"Unknown", "SwipeRight", "SwipeLeft", 
                                                      "Point", "Rest"};
  
  char full[_MAX_PATH];
  _fullpath(full, model.c_str(), _MAX_PATH);
  handinput::InfEngine engine(full);

  ASSERT_EQ(kDescriptionLen, engine.descriptor_len());
  vector<string> gesture_labels = engine.gesture_labels();
  for (int i = 0; i < kNumGestures; i++) {
    ASSERT_EQ(kGestureLabels[i], gesture_labels[i]);
  }
}

TEST(InfEngineTest, Initialization) {
  using Eigen::VectorXf;
  using Eigen::VectorXd;
  using Eigen::MatrixXd;
  using Eigen::MatrixXf;
  using handinput::MixGaussian;
  using handinput::Gaussian;

  static const int kDescriptorLen = 7 * 7 * 9;
  static const int N_PRINCIPAL_COMPS = 26;
  static const int FEATURE_LEN = N_PRINCIPAL_COMPS + 9;
  static const int kNStates = 10;
  static const int N_MIXTURES = 2;

  char full[_MAX_PATH];
  _fullpath(full, kModelFile.c_str(), _MAX_PATH);

  handinput::InfEngine engine(full);
  ASSERT_EQ(kDescriptorLen, engine.descriptor_len());
  ASSERT_EQ(N_PRINCIPAL_COMPS, engine.n_principal_comps());
  ASSERT_EQ(FEATURE_LEN, engine.feature_len());
  const VectorXf* pca_mean = engine.pca_mean();
  ASSERT_EQ(kDescriptorLen, pca_mean->size());
  ASSERT_NEAR(0.12, pca_mean->coeff(0), ABS_ERROR);
  ASSERT_NEAR(0.012, pca_mean->coeff(kDescriptorLen - 1), ABS_ERROR);
  ASSERT_EQ(4, engine.n_vocabularies());
  ASSERT_EQ(4, engine.n_states_per_gesture());

  const MatrixXf* pc = engine.principal_comp();
  ASSERT_EQ(N_PRINCIPAL_COMPS, pc->rows());
  ASSERT_EQ(kDescriptorLen, pc->cols());
  ASSERT_NEAR(0.0166, pc->coeff(0, 0), ABS_ERROR);
  ASSERT_NEAR(-0.0147, pc->coeff(0, kDescriptorLen - 1), ABS_ERROR);
ASSERT_NEAR(0.0145, pc->coeff(N_PRINCIPAL_COMPS - 1, kDescriptorLen - 1), ABS_ERROR);

  const VectorXf* std_mu = engine.std_mu();
  ASSERT_EQ(FEATURE_LEN, std_mu->size());
  ASSERT_NEAR(0.1169, std_mu->coeff(0), ABS_ERROR);
  ASSERT_NEAR(0, std_mu->coeff(FEATURE_LEN - 1), ABS_ERROR);

  const VectorXf* std_sigma = engine.std_sigma();
  ASSERT_EQ(FEATURE_LEN, std_sigma->size());
  ASSERT_NEAR(0.1587, std_sigma->coeff(0), ABS_ERROR);
  ASSERT_NEAR(0.1227, std_sigma->coeff(FEATURE_LEN - 1), ABS_ERROR);

  const handinput::HMM* hmm = engine.hmm();
  ASSERT_EQ(kNStates, hmm->n_states());
  const VectorXd* prior = hmm->prior();
  ASSERT_EQ(kNStates, prior->size());
  ASSERT_NEAR(0, prior->coeff(0), ABS_ERROR);
  ASSERT_NEAR(0.25, prior->coeff(kNStates - 1), ABS_ERROR);

  // Transposed transmat.
  const MatrixXd* transmat = hmm->transmat_t();
  ASSERT_EQ(kNStates, transmat->rows());
  ASSERT_EQ(kNStates, transmat->cols());
  ASSERT_NEAR(0.7157, transmat->coeff(0, 0), ABS_ERROR);
  ASSERT_NEAR(0, transmat->coeff(0, kNStates - 1), ABS_ERROR);
  ASSERT_NEAR(0.0098, transmat->coeff(kNStates - 1, 0), ABS_ERROR);
  ASSERT_NEAR(0.9750, transmat->coeff(kNStates - 1, kNStates - 1), ABS_ERROR);
 
  const MixGaussian* mixgaussian = hmm->MixGaussianAt(0);
  ASSERT_EQ(N_MIXTURES, mixgaussian->n_mixtures());
  const VectorXf* mix = mixgaussian->mix();
  ASSERT_NEAR(0.6485, mix->coeff(0), ABS_ERROR);
  const Gaussian* gaussian = mixgaussian->GaussianAt(0);
  const VectorXf* mean = gaussian->mean();
  ASSERT_NEAR(1.0071, mean->coeff(0), ABS_ERROR);
  ASSERT_NEAR(-0.3895, mean->coeff(FEATURE_LEN - 1), ABS_ERROR);
  const MatrixXf* inv_cov = gaussian->inv_cov();
  ASSERT_NEAR(19.3991, inv_cov->coeff(0, 0), ABS_ERROR);
  ASSERT_NEAR(0.8824, inv_cov->coeff(FEATURE_LEN - 1, FEATURE_LEN - 1), ABS_ERROR);

  gaussian = mixgaussian->GaussianAt(1);
  mean = gaussian->mean();
  ASSERT_NEAR(-0.1283, mean->coeff(0), ABS_ERROR);
  inv_cov = gaussian->inv_cov();
  ASSERT_NEAR(3.5790, inv_cov->coeff(0, 0), ABS_ERROR);

  mixgaussian = hmm->MixGaussianAt(kNStates - 1);
  gaussian = mixgaussian->GaussianAt(1);
  mean = gaussian->mean();
  inv_cov = gaussian->inv_cov();
  ASSERT_NEAR(0, mean->coeff(0), ABS_ERROR);
  ASSERT_NEAR(0, mean->coeff(FEATURE_LEN - 1), ABS_ERROR);
  ASSERT_NEAR(1, inv_cov->coeff(0, 0), ABS_ERROR);
  ASSERT_NEAR(1, inv_cov->coeff(FEATURE_LEN - 1, FEATURE_LEN - 1), ABS_ERROR);
}
