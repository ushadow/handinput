#pragma once
#include "processor.h"

namespace handinput {
  public ref class MProcessor {
  public:
    MProcessor(int w, int h, std::string model_file);
    ~MProcessor() { this->!MProcessor(); }
    !MProcessor() { delete processor_; }
    void Update(System::IntPtr continuous_features, System::IntPtr image);
  private:
    Processor* processor_;
  };

  inline MProcessor::MProcessor(int w, int h, std::string model_file) {
    processor_ = new Processor(w, h, model_file);
  }

  inline void MProcessor::Update(System::IntPtr continuous_features, System::IntPtr image) {
    processor_->Update(reinterpret_cast<float*>(continuous_features.ToPointer()),
      reinterpret_cast<IplImage*>(image.ToPointer()));
  }
}

