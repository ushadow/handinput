#include <stdlib.h>
#include "gtest\gtest.h"
#include "hmm.h"
#include "mat.h"

#define ABS_ERROR 0.1

static const std::string kModelFile = "../../../data/model.mat";

TEST(HMMTest, Fwdback) {
  using handinput::HMM;
  using Eigen::VectorXf;

  const int kFeatureLen = 35;

  char full[_MAX_PATH];
  _fullpath(full, kModelFile.c_str(), _MAX_PATH);
  // Only takes absolute path.
  MATFile* file = matOpen(full, "r");
  mxArray* model = matGetVariable(file, "model");
  std::unique_ptr<HMM> hmm(HMM::CreateFromMxArray(mxGetField(model, 0, "infModel")));
  ASSERT_EQ(kFeatureLen, hmm->feature_len());

  double loglik = hmm->Fwdback(VectorXf::Zero(kFeatureLen));
  ASSERT_NEAR(-36.3618, loglik, ABS_ERROR);
  ASSERT_EQ(8, hmm->MostLikelyState());
  
  loglik = hmm->Fwdback(VectorXf::Ones(kFeatureLen));
  ASSERT_NEAR(-99.4905, loglik, ABS_ERROR);
  ASSERT_EQ(8, hmm->MostLikelyState());

  mxDestroyArray(model);
}