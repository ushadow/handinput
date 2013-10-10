#include "gtest\gtest.h"
#include "infengine.h"

TEST(InfEngineTest, Initialization) {
  handinput::InfEngine engine;
  ASSERT_EQ(engine.descriptor_len(), 2025);
  ASSERT_EQ(engine.n_principal_comp(), 23);
}
