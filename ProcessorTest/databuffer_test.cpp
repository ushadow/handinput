#include "gtest\gtest.h"
#include "feature_processor.h"

TEST(DataBufferTest, Update) {
  using handinput::DataBuffer;
  using cv::Mat;
  using cv::Mat_;

  DataBuffer buffer(3);
  Mat v = (Mat_<float>(1, 3) << 1, 2, 3);
  buffer.Update(v);
  Mat v1 = buffer.GetFrame(1);
  for (int i = 0; i < v.cols; i++) 
    ASSERT_EQ(v1.at<float>(0, i), v.at<float>(0, i));
}