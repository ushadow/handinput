#include "processor.h"

namespace handinput {
  Processor::Processor(int w, int h, const std::string& model_file) {
    feature_proc_.reset(new FeatureProcessor(w, h));
    inf_engine_.reset(new InfEngine(model_file));
  }

  void Processor::Update(float x, float y, float z, IplImage* image) {
    cv::Mat mat(image); // Default is not copying data.
    float* feature = feature_proc_->Compute(x, y, z, mat);
    if (feature != NULL)
      inf_engine_->Update(feature);
  }
} 