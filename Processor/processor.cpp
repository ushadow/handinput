#include "processor.h"

namespace handinput {
  Processor::Processor(int w, int h, const std::string& model_file) {
    feature_proc_.reset(new FeatureProcessor(w, h));
    inf_engine_.reset(new InfEngine(model_file));
  }

  void Processor::Update(float* continuous_features, IplImage* image) {
    cv::Mat mat(image); // Default is not copying data.
    float* descriptor = feature_proc_->Compute(mat);
    inf_engine_->Update(continuous_features, descriptor);
  }
} 