#pragma once
#include "Stdafx.h"
#include "processor.h"

namespace handinput {
  public ref class MProcessor {
  public:
    MProcessor(int w, int h, System::String^ model_file);
    ~MProcessor() { this->!MProcessor(); }
    !MProcessor() { delete processor_; }
    void Update(System::IntPtr continuous_features, System::IntPtr image);
  private:
    Processor* processor_;
  };
}