#pragma once
#include "Stdafx.h"
#include "processor.h"

namespace handinput {
  public ref class MProcessor {
  public:
    MProcessor(int w, int h, System::String^ model_file);
    ~MProcessor() { this->!MProcessor(); }
    !MProcessor() { delete processor_; }
    void Update(float x, float y, float z, System::IntPtr image, bool visualize);
    void Reset() { processor_->Reset(); } 
  private:
    Processor* processor_;
  };
}