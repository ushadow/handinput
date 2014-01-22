#include "Stdafx.h"
#include "MProcessor.h"

namespace handinput {

  MProcessor::MProcessor(int w, int h, System::String^ model_file) {
    using System::Runtime::InteropServices::Marshal;
    char* str = (char*) Marshal::StringToHGlobalAnsi(model_file).ToPointer();
    processor_ = new Processor(w, h, str);
  }

  System::String^ MProcessor::Update(float x, float y, float z, System::IntPtr image, 
      System::IntPtr skin, bool visualize) {

    std::string gesture_label = processor_->Update(x, y, z, 
        reinterpret_cast<IplImage*>(image.ToPointer()), 
        reinterpret_cast<IplImage*>(skin.ToPointer()), visualize);
    System::String^ ms = msclr::interop::marshal_as<System::String^>(gesture_label);
    return ms;
  }
}

