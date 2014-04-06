#include "gtest\gtest.h"
#include "feature_processor.h"

void DisplayImage(cv::Mat& image) {
  std::string window_name = "Test Window";
  cv::namedWindow(window_name);
  cv::imshow(window_name, image);
  cv::waitKey(0);
  cv::destroyWindow(window_name);
}

TEST(FeatureProcessorTest, ComputeHOGDescriptorAllWhite) {
  using cv::Mat;

  int image_size = 64;
  handinput::FeatureProcessor processor(image_size, image_size);
  Mat image(image_size, image_size, CV_8U, cv::Scalar(255));
  float* descriptor = processor.Compute(image);
  for (int i = 0; i < processor.HOGLength(); i++) {
    ASSERT_EQ(0, descriptor[i]);
  }
}

TEST(FeatureProcessorTest, ComputeHOGDescriptorTwoImages) {
  using cv::Mat;
  int image_size = 64;
  handinput::FeatureProcessor processor(image_size, image_size);
  Mat image(image_size, image_size, CV_8U, cv::Scalar(255));
  float* descriptor = processor.Compute(image);
  for (int i = 0; i < image_size; i++) {
    image.at<uchar>(32, i) = 0;
    image.at<uchar>(i, 32) = 0;
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

TEST(FeatureProcessorTest, ComputeMotionFeature) {
  using cv::Mat;

  int image_size = 64;
  handinput::FeatureProcessor processor(image_size, image_size);
  Mat image(image_size, image_size, CV_8U, cv::Scalar(255));
  Mat skin;
  float* feature = processor.Compute(0, 0, 0, image, skin);
  ASSERT_EQ(NULL, feature);
  feature = processor.Compute(0, 0, 0, image, skin);
  ASSERT_EQ(NULL, feature);
  feature = processor.Compute(0, 0, 0, image, skin);
  for (int i = 0; i < processor.FeatureLength(); i++)
    ASSERT_EQ(0, feature[i]);
  feature = processor.Compute(1, 2, 3, image, skin);
  ASSERT_EQ(1, feature[0]);
  ASSERT_EQ(2, feature[1]);
  ASSERT_EQ(3, feature[2]);
  ASSERT_EQ(1, feature[3]);
  ASSERT_EQ(2, feature[4]);
  ASSERT_EQ(3, feature[5]);
  ASSERT_EQ(1, feature[6]);
  ASSERT_EQ(2, feature[7]);
  ASSERT_EQ(3, feature[8]);
  feature = processor.Compute(0, 0, 0, image, skin);
  ASSERT_EQ(0, feature[0]);
  ASSERT_EQ(0, feature[1]);
  ASSERT_EQ(0, feature[2]);
  ASSERT_EQ(-1, feature[3]);
  ASSERT_EQ(-2, feature[4]);
  ASSERT_EQ(-3, feature[5]);
  ASSERT_EQ(-2, feature[6]);
  ASSERT_EQ(-4, feature[7]);
  ASSERT_EQ(-6, feature[8]);
}