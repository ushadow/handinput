#include "pcheader.h"
#include "gesture_event_detector.h"

namespace handinput {
  // Gesture stages: PreStroke, Gesture, PostStroke
  // Gesture events: StratPreStroke, StartGesture, StopGesture
  void GestureEventDetector::Detect(const std::string& gesture, const std::string& stage,
    json_spirit::mObject* result) {
    using std::string;

    string gesture_event;
    string nucleus = gesture;

    if (stage != prev_stage_) {
#ifdef _DEBUG
      std::cout << "prev event = " << prev_event_ << std::endl;
#endif
      if (stage == "PreStroke") {
        gesture_event = "StartPreStroke";
      } else if (stage == "Gesture") {
        gesture_event = "StartGesture";
      } else if (stage == "PostStroke" && prev_event_ == "StartGesture") {
        gesture_event = "StopGesture";
        nucleus = prev_gesture_;
      }
    }
    prev_stage_ = stage;
    prev_gesture_ = nucleus;
    if (gesture_event != "")
      prev_event_ = gesture_event;
    (*result)["gesture"] = nucleus;
    (*result)["stage"] = stage;
    (*result)["eventType"] = gesture_event;
  }

  void GestureEventDetector::Reset() {
    prev_stage_ = "";
  }
}