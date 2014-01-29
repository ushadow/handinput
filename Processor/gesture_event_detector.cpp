#include "pcheader.h"
#include "gesture_event_detector.h"

namespace handinput {
  std::string GestureEventDetector::Detect(std::string stage) {
    using std::string;

    string gesture_event;

    if (stage != prev_stage_) {
      if (stage == "PreStroke")
        gesture_event = "StartPreStroke";
      if (stage == "Gesture")
        gesture_event = "StartGesture";
      if (stage == "PostStroke")
        gesture_event = "StopGesture";
    }
    prev_stage_ = stage;
    return gesture_event;
  }

  void GestureEventDetector::Reset() {
    prev_stage_ = "";
  }
}