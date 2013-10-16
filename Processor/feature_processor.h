#pragma once
#include "pcheader.h"
#include "hog_descriptor.h"

namespace handinput {
  class PROCESSOR_API FeatureProcessor {
  public:
    // w: feature image patch width.
    // h: feature image patch height.
    FeatureProcessor(int w, int h);
    ~FeatureProcessor(void);

    // Resizes the image and converts the image to float point values. 
    // Returns the float array of HOG descriptors. This object still has the ownership of the float 
    // array.
    float* Compute(cv::Mat& image, bool visualize = false);
    cv::Mat VisualizeHOG(cv::Mat& orig_image, int zoom_factor = 3);
    int HOGLength() { return hog_->Length(); }

  private:
    static const int kCellSize = 4;
    static const int kNBins = 9;
    static const std::string kDebugWindowName;

    std::unique_ptr<HOGDescriptor> hog_; 
    int w_, h_;
    std::unique_ptr<cv::Mat> scaled_image_, float_image_;
    // HOG descriptor.
    std::unique_ptr<float[]> descriptor_;

    void DisplayImage(cv::Mat& image);
  };

}