#pragma once
#include "pcheader.h"
#include "hog_descriptor.h"
#include "databuffer.h"

namespace handinput {
  class PROCESSOR_API FeatureProcessor {
  public:
    // w: feature image patch width.
    // h: feature image patch height.
    FeatureProcessor(int w, int h);
    ~FeatureProcessor(void);

    // Returns the float array of the feature vector. Can be null if there are not enough data to
    // compute f
    float* Compute(float x, float y, float z, cv::Mat& image, bool visualize = false);

    // Computes the descriptor feature from the image.
    //
    // orig_image: can be any size and value type.
    // visualize: if true, display the hog feature on top of the scaled original image patch.
    //
    // Returns the float array of HOG descriptors. This object still has the ownership of the float 
    // array.
    float* Compute(cv::Mat& image, bool visualize = false);
    cv::Mat VisualizeHOG(cv::Mat& orig_image, int zoom_factor = 3);
    int HOGLength() { return hog_->Length(); }
    int FeatureLength() { return hog_->Length() + kMotionFeatureLen; }

  private:
    static const int kCellSize = 4;
    static const int kNBins = 9;
    static const int kMotionFeatureLen = 9;
    static const int kSpan = 15;
    static const std::string kDebugWindowName;

    static void CopyMatToArray(const cv::Mat& v, float* a, int start_index);
    
    std::unique_ptr<HOGDescriptor> hog_; 
    int w_, h_;
    std::unique_ptr<cv::Mat> scaled_image_, float_image_;
    // HOG descriptor.
    std::unique_ptr<float[]> feature_;
    // Pointer to the start of descriptor in the feature array.
    float* descriptor_;
    cv::Mat prev_pos_, prev_v_;
    DataBuffer pos_buffer_;

    void DisplayImage(cv::Mat& image);
  };

}