#include <stdlib.h>
#include "gtest\gtest.h"
#include "hmm.h"
#include "mat.h"

namespace handinput {
#define ABS_ERROR 0.1

  class HMMTest : public ::testing::Test {

  protected:
    static const int kFeatureLen = 35;
    static const std::string kModelFile; 
    mxArray* model_;

    virtual void SetUp() {
      char full[_MAX_PATH];
      _fullpath(full, kModelFile.c_str(), _MAX_PATH);
      // Only takes absolute path.
      MATFile* file = matOpen(full, "r");
      model_ = matGetVariable(file, "model");
    }

    virtual void TearDown() {
      mxDestroyArray(model_);
    }
  };

  const std::string HMMTest::kModelFile = "../../../data/model.mat";

  TEST_F(HMMTest, Fwdback) {
    using Eigen::VectorXf;
    std::unique_ptr<HMM> hmm(HMM::CreateFromMxArray(mxGetField(model_, 0, "infModel"), 0));
    ASSERT_EQ(kFeatureLen, hmm->feature_len());

    hmm->Fwdback(VectorXf::Zero(kFeatureLen));
    ASSERT_EQ(8, hmm->MostLikelyState());

    hmm->Fwdback(VectorXf::Ones(kFeatureLen));
    ASSERT_EQ(8, hmm->MostLikelyState());

    const std::vector<int>& map = hmm->state_to_label_map();
    ASSERT_EQ(1, map[0]);
    ASSERT_EQ(4, map[map.size() - 1]);

    const std::vector<std::string> stage_map = hmm->stage_map();
    EXPECT_EQ("PreStroke", stage_map[0]);
    EXPECT_EQ("Gesture", stage_map[stage_map.size() - 1]);
  }

  TEST_F(HMMTest, FwdbackWithLag) {
    using Eigen::VectorXf;
    std::unique_ptr<HMM> hmm(HMM::CreateFromMxArray(mxGetField(model_, 0, "infModel"), 3));
    hmm->Fwdback(VectorXf::Zero(kFeatureLen));
    ASSERT_EQ(8, hmm->MostLikelyState());

    hmm->Fwdback(VectorXf::Ones(kFeatureLen) * 0.1f);
    ASSERT_EQ(8, hmm->MostLikelyState());

    hmm->Fwdback(VectorXf::Ones(kFeatureLen) * 0.3f);
    ASSERT_EQ(8, hmm->MostLikelyState());
    
    hmm->Fwdback(VectorXf::Ones(kFeatureLen) * -0.1f);
    ASSERT_EQ(8, hmm->MostLikelyState());
  }

}