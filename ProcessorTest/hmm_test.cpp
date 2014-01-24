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

  const std::vector<int>& map = hmm->state_to_label_map();
  ASSERT_EQ(1, map[0]);
  ASSERT_EQ(4, map[map.size() - 1]);

  mxDestroyArray(model);
}