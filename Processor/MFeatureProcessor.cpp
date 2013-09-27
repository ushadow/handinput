#include "pcheader.h"
#include "MFeatureProcessor.h"

namespace handinput {

  MFeatureProcessor::MFeatureProcessor(int w, int h) {
    processor_ = new FeatureProcessor(w, h);
  }

  float* MFeatureProcessor::Compute(System::IntPtr image_ptr) {
    IplImage* image = reinterpret_cast<IplImage*>(image_ptr.ToPointer()); 
    cv::Mat mat(image);
    return processor_->Compute(mat);
 }

  void MFeatureProcessor::Visualize(System::IntPtr image_ptr, float* descriptorValues) {   
    using cv::Mat;
    IplImage* image = reinterpret_cast<IplImage*>(image_ptr.ToPointer()); 
    Mat mat(image);
    Mat visu = processor_->Visualize(mat, descriptorValues);
  } 
}