#include <stdlib.h>
#include "gtest\gtest.h"
#include "hmm.h"
#include "mat.h"

#define ABS_ERROR 0.0001

static const std::string kModelFile = "../../../data/model.mat";

TEST(HMMTest, Fwdback) {
  using handinput::HMM;
  using Eigen::VectorXf;

  const int kFeatureLen = 32;

  char full[_MAX_PATH];
  _fullpath(full, kModelFile.c_str(), _MAX_PATH);
  // Only takes absolute path.
  MATFile* file = matOpen(full, "r");
  mxArray* model = matGetVariable(file, "model");
  std::unique_ptr<HMM> hmm(HMM::CreateFromMxArray(mxGetField(model, 0, "infModel")));
  ASSERT_EQ(kFeatureLen, hmm->feature_len());

  float loglik = hmm->Fwdback(VectorXf::Zero(kFeatureLen));
  ASSERT_NEAR(-28.0732, loglik, ABS_ERROR);
  ASSERT_EQ(1, hmm->MostLikelyState());
  
  loglik = hmm->Fwdback(VectorXf::Ones(kFeatureLen));
  ASSERT_NEAR(-73.5158, loglik, ABS_ERROR);
  ASSERT_EQ(2, hmm->MostLikelyState());

  mxDestroyArray(model);
}