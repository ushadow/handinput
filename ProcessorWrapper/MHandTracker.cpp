#include "Stdafx.h"
#include "MHandTracker.h"

namespace handinput {
  void MHandTracker::Update(System::IntPtr image, System::IntPtr dst) {
    hand_tracker_->Update(static_cast<IplImage*>(image.ToPointer()), 
                          static_cast<IplImage*>(dst.ToPointer()));
  }
}