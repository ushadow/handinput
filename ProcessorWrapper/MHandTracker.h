#pragma once
#include "hand_tracker.h"

namespace handinput {
  public ref class MHandTracker {
  public:
    MHandTracker(int width, int height) : hand_tracker_(new HandTracker(width, height)) {}
    !MHandTracker() { delete hand_tracker_; }
    ~MHandTracker() { this->!MHandTracker(); }
    void Update(System::IntPtr image, System::IntPtr dst);
  private:
    HandTracker* hand_tracker_;
  };
}