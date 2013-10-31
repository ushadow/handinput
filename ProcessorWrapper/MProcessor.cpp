#include "Stdafx.h"
#include "MProcessor.h"

namespace handinput {

  MProcessor::MProcessor(int w, int h, System::String^ model_file) {
    using System::Runtime::InteropServices::Marshal;
    char* str = (char*) Marshal::StringToHGlobalAnsi(model_file).ToPointer();
    processor_ = new Processor(w, h, str);
  }

  int MProcessor::Update(float x, float y, float z, System::IntPtr image, bool visualize) {
    return processor_->Update(x, y, z, reinterpret_cast<IplImage*>(image.ToPointer()), visualize);
  }
}

