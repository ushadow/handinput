#include "pcheader.h"
#include "processor.h"

namespace handinput {
  Processor::Processor(int w, int h, const std::string& model_file) {
    feature_proc_.reset(new FeatureProcessor(w, h));
    inf_engine_.reset(new InfEngine(model_file));
  }

  int Processor::Update(float x, float y, float z, IplImage* image, IplImage* skin, bool visualize) 
  {
    cv::Mat mat(image); // Default is not copying data.
    cv::Mat skinMat(skin);
    float* feature = feature_proc_->Compute(x, y, z, mat, skinMat, visualize);
    if (feature != NULL)
      return inf_engine_->Update(feature);
    else 
      return 0;
  }
} 