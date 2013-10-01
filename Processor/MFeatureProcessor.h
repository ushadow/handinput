#pragma once
#include "pcheader.h"
#include "feature_processor.h"

namespace handinput {
public ref class MFeatureProcessor {
public:
  // w: width of the image.
  // h: height of the image.
  MFeatureProcessor(int w, int h);
  ~MFeatureProcessor() { this->!MFeatureProcessor(); }
  !MFeatureProcessor() { delete processor_; } 
  void Compute(System::IntPtr image);
  void Visualize(System::IntPtr image_ptr);
private:
  FeatureProcessor* processor_; 
};
}

