#pragma once
#include "pcheader.h"
#include "feature_processor.h"
#include "infengine.h"

namespace handinput {
  // The main interface to the gesture recognition engine.
  class PROCESSOR_API Processor {
  public:
    Processor(int w, int h, const std::string& model_file);
    void Update(float x, float y, float z, IplImage* image);
  private:
    std::unique_ptr<FeatureProcessor> feature_proc_;
    std::unique_ptr<InfEngine> inf_engine_;

  };
}