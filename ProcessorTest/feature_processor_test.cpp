#include "gtest\gtest.h"
#include "feature_processor.h"

TEST(FeatureProcessorTest, ComputeHOGDescriptor) {
  using cv::Mat;
  using std::string;

  handinput::FeatureProcessor processor(64, 64);
  Mat mat = cv::imread("../../data/test.png", CV_LOAD_IMAGE_COLOR);
  Mat resized;
  resize(mat, resized, cv::Size(64, 64));
  Mat gray_image;
  cv::cvtColor(resized, gray_image, CV_BGR2GRAY); 
  processor.Compute(gray_image);
  Mat vis = processor.Visualize(gray_image, 3);
  string window_name = "Test Window";
  cv::namedWindow(window_name);
  cv::imshow(window_name, vis);
  cv::waitKey(0);
  cv::destroyWindow(window_name);
}