#include "pcheader.h"
#include "MFeatureProcessor.h"

namespace handinput {
  MFeatureProcessor::MFeatureProcessor(int w, int h) {
    hog_ = new HOGDescriptor(w, h, kCellSize, kNBins);
  }

  void MFeatureProcessor::Compute(System::IntPtr imagePtr) {
    IplImage* image = reinterpret_cast<IplImage*>(imagePtr.ToPointer()); 
    IplImage* double_image = cvCreateImage(cvSize(64, 64), IPL_DEPTH_32F, 1);
    cvConvert(image, double_image);
    std::unique_ptr<float[]> descriptor(new float[hog_->Length()]);
    hog_->Compute((float*) double_image->imageData, descriptor.get());
  }
}