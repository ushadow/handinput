#include "Stdafx.h"
#include "MFeatureProcessor.h"

namespace handinput {

  MFeatureProcessor::MFeatureProcessor(int w, int h) {
    processor_ = new FeatureProcessor(w, h);
  }

  System::IntPtr MFeatureProcessor::Compute(System::IntPtr image_ptr, bool visulize) {
    IplImage* image = reinterpret_cast<IplImage*>(image_ptr.ToPointer()); 
    cv::Mat mat(image);
    float* descriptor = processor_->Compute(mat, visulize);
    return System::IntPtr(descriptor);
 }

  int MFeatureProcessor::HOGLength() { return processor_->HOGLength(); }
}