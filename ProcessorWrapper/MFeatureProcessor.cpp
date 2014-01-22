#include "Stdafx.h"
#include "MFeatureProcessor.h"

namespace handinput {

  MFeatureProcessor::MFeatureProcessor(int w, int h, int bufferSize) {
    processor_ = new FeatureProcessor(w, h, bufferSize);
  }

  System::IntPtr MFeatureProcessor::Compute(float x, float y, float z, 
      System::IntPtr depth_image_ptr, bool visulize) {
    IplImage* depth_image = reinterpret_cast<IplImage*>(depth_image_ptr.ToPointer()); 
    cv::Mat mat(depth_image);
    cv::Mat skinMat;
    float* descriptor = processor_->Compute(x, y, z, mat, skinMat, visulize);
    return System::IntPtr(descriptor);
 }

  int MFeatureProcessor::HOGLength() { return processor_->HOGLength(); }
}