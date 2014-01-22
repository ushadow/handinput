#include "Stdafx.h"
#include "MTemporalBuffer.h"

namespace handinput {
  void MTemporalBuffer::Update(System::IntPtr image, System::IntPtr dst) {
    hand_tracker_->Update(static_cast<IplImage*>(image.ToPointer()), 
                          static_cast<IplImage*>(dst.ToPointer()));
  }
}