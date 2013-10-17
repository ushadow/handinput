#include "Stdafx.h"
#include "MFeatureProcessor.h"

namespace handinput {

  MFeatureProcessor::MFeatureProcessor(int w, int h) {
    processor_ = new FeatureProcessor(w, h);
  }

  System::IntPtr MFeatureProcessor::Compute(float x, float y, float z, System::IntPtr image_ptr, 
      bool visulize) {
    IplImage* image = reinterpret_cast<IplImage*>(image_ptr.ToPointer()); 
    cv::Mat mat(image);
    float* descriptor = processor_->Compute(x, y, z, mat, visulize);
    return System::IntPtr(descriptor);
 }

  int MFeatureProcessor::HOGLength() { return processor_->HOGLength(); }
}