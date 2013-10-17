#include "Stdafx.h"
#include "MProcessor.h"

namespace handinput {

  MProcessor::MProcessor(int w, int h, System::String^ model_file) {
    using System::Runtime::InteropServices::Marshal;
    char* str = (char*) Marshal::StringToHGlobalAnsi(model_file).ToPointer();
    processor_ = new Processor(w, h, str);
  }

  void MProcessor::Update(System::IntPtr continuous_features, System::IntPtr image) {
    processor_->Update(reinterpret_cast<float*>(continuous_features.ToPointer()),
      reinterpret_cast<IplImage*>(image.ToPointer()));
  }
}

