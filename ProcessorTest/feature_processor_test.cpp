#include "gtest\gtest.h"
#include "feature_processor.h"

void DisplayImage(cv::Mat& image) {
  std::string window_name = "Test Window";
  cv::namedWindow(window_name);
  cv::imshow(window_name, image);
  cv::waitKey(0);
  cv::destroyWindow(window_name);
}

TEST(FeatureProcessorTest, ComputeHOGDescriptorAllZero) {
  using cv::Mat;

  int imageSize = 64;
  handinput::FeatureProcessor processor(imageSize, imageSize);
  Mat image(imageSize, imageSize, CV_8U, cv::Scalar(255));
  float* descriptor = processor.Compute(image);
  for (int i = 0; i < processor.HOGLength(); i++) {
    ASSERT_EQ(0, descriptor[i]);
  }
}

TEST(FeatureProcessorTest, ComputeHOGDescriptorTwoImages) {
  using cv::Mat;
  int imageSize = 64;
  handinput::FeatureProcessor processor(imageSize, imageSize);
  Mat image(imageSize, imageSize, CV_8U, cv::Scalar(255));
  float* descriptor = processor.Compute(image);
  for (int i = 0; i < imageSize; i++) {
    image.at<byte>(32, i) = 0;
    image.at<byte>(i, 32) = 0;
  }
  descriptor = processor.Compute(image);
  Mat vis = processor.VisualizeHOG(image, 6);
  DisplayImage(vis);
}

TEST(FeatureProcessorTest, ComputeHOGDescriptor) {
  using cv::Mat;
  using std::string;

  handinput::FeatureProcessor processor(64, 64);
  Mat mat = cv::imread("../../../data/test.png", CV_LOAD_IMAGE_COLOR);
  Mat resized;
  resize(mat, resized, cv::Size(64, 64));
  Mat gray_image;
  cv::cvtColor(resized, gray_image, CV_BGR2GRAY); 
  processor.Compute(gray_image);
  Mat vis = processor.VisualizeHOG(gray_image, 6);
  DisplayImage(vis);
}