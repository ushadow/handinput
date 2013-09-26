#pragma once
#include "pcheader.h"
#include "hog_descriptor.h"

namespace handinput {
public ref class MFeatureProcessor {
public:
  MFeatureProcessor(int w, int h);
  void Compute(System::IntPtr image);
  ~MFeatureProcessor() { this->!MFeatureProcessor(); }
  !MFeatureProcessor() { delete hog_; }
private:
  static const int kCellSize = 4;
  static const int kNBins = 9;
  HOGDescriptor* hog_; 
};
}

