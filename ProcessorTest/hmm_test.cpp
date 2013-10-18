#include "gtest\gtest.h"
#include "hmm.h"
#include "mat.h"

#define ABS_ERROR 0.0001

static const std::string kModelFile = "G:\\salience\\model.mat";

TEST(HMMTest, Fwdback) {
  using handinput::HMM;
  using Eigen::VectorXf;

  const int kFeatureLen = 32;

  MATFile* file = matOpen(kModelFile.c_str(), "r");
  mxArray* model = matGetVariable(file, "model");
  std::unique_ptr<HMM> hmm(HMM::CreateFromMxArray(mxGetField(model, 0, "infModel")));
  ASSERT_EQ(kFeatureLen, hmm->feature_len());

  float loglik = hmm->Fwdback(VectorXf::Zero(kFeatureLen));
  ASSERT_NEAR(-28.0732, loglik, ABS_ERROR);
  
  loglik = hmm->Fwdback(VectorXf::Ones(kFeatureLen));
  ASSERT_NEAR(-73.5158, loglik, ABS_ERROR);
  mxDestroyArray(model);
  matClose(file);
}