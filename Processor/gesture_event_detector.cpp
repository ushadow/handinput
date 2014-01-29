#include "pcheader.h"
#include "gesture_event_detector.h"

namespace handinput {
  std::string GestureEventDetector::Detect(const std::string& gesture, const std::string& stage) {
    using std::string;

    string gesture_event;

    if (stage != prev_stage_) {
      if (stage == "PreStroke") {
        gesture_event = "StartPreStroke";
      } else if (stage == "Gesture") {
        gesture_event = "StartGesture";
      } else if (stage == "PostStroke" && prev_event_ == "StartGesture" && 
                 prev_gesture_ == gesture) {
        gesture_event = "StopGesture";
      }
      prev_event_ = gesture_event;
    }
    prev_stage_ = stage;
    prev_gesture_ = gesture;
    return gesture_event;
  }

  void GestureEventDetector::Reset() {
    prev_stage_ = "";
  }
}