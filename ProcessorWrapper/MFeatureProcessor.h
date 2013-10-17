#pragma once
#include "feature_processor.h"

namespace handinput {

public ref class MFeatureProcessor {
public:
  // w: width of the image.
  // h: height of the image.
  MFeatureProcessor(int w, int h);
  ~MFeatureProcessor() { this->!MFeatureProcessor(); }
  !MFeatureProcessor() { delete processor_; } 
  System::IntPtr Compute(float x, float y, float z, System::IntPtr image, bool visualize);
  int HOGLength();
private:
  FeatureProcessor* processor_; 
};
}

