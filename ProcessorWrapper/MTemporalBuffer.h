#pragma once
#include "hand_tracker.h"

namespace handinput {
  public ref class MTemporalBuffer {
  public:
    MTemporalBuffer(int width, int height) : hand_tracker_(new HandTracker(width, height)) {}
    !MTemporalBuffer() { delete hand_tracker_; }
    ~MTemporalBuffer() { this->!MTemporalBuffer(); }
    void Update(System::IntPtr image, System::IntPtr dst);
  private:
    HandTracker* hand_tracker_;
  };
}