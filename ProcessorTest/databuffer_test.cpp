#include "gtest\gtest.h"
#include "feature_processor.h"

TEST(DataBufferTest, Update) {
  using handinput::DataBuffer;
  using cv::Mat;
  using cv::Mat_;
  using std::vector;

  DataBuffer buffer(3);
  Mat v = (Mat_<float>(1, 3) << 1, 2, 3);
  buffer.Update(v);
  Mat v_star = buffer.GetFrame(1);
  for (int i = 0; i < v.cols; i++) 
    ASSERT_EQ(v.at<float>(0, i), v_star.at<float>(0, i));
  
  v = (Mat_<float>(1, 3) << 4, 5, 6);
  buffer.Update(v);
  v = (Mat_<float>(1, 3) << 7, 8, 9);
  buffer.Update(v);
  v = (Mat_<float>(1, 3) << 10, 11, 12);
  buffer.Update(v);
  v_star = buffer.GetFrame(4);
  for (int i = 0; i < v.cols; i++)
    ASSERT_EQ(v.at<float>(0, i), v_star.at<float>(0, i));

  vector<float> mask(3);
  mask[0] = 0.1f;
  mask[1] = 0.2f;
  mask[2] = 0.7f;

  Mat res(1, 3, DATATYPE);
  Mat expected = (Mat_<float>(1, 3) << 5.2, 6.2, 7.2);
  buffer.TemporalConvolve(&res, mask);
  for (int i = 0; i < res.cols; i++) {
    ASSERT_EQ(expected.at<float>(0, i), res.at<float>(0, i));
  }
}