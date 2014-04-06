#include "pcheader.h"
#include "gesture_event_detector.h"

namespace handinput {
  GestureEventDetector::GestureEventDetector() : time_step_(0), start_gesture_time_(0) {}

  // Gesture stages: PreStroke, Gesture, PostStroke
  // Gesture events: StratPreStroke, StartGesture, StopGesture
  void GestureEventDetector::Detect(const std::string& gesture, const std::string& stage,
    json_spirit::mObject* result) {
    using std::string;

    time_step_++;

    string gesture_event;
    string nucleus = gesture;

    if (stage != prev_stage_) {
#ifdef _DEBUG
      std::cout << "prev event = " << prev_event_ << std::endl;
#endif
      if (stage == "PreStroke") {
        gesture_event = "StartPreStroke";
        start_gesture_time_ = time_step_;
      } else if (stage == "Gesture") {
        gesture_event = "StartGesture";
        start_gesture_time_ = time_step_;
      } else if (stage == "PostStroke" && prev_event_ == "StartGesture") {
        if (time_step_ - start_gesture_time_ > MIN_NUCLEUS_LEN) {
          gesture_event = "StartPostStroke";
          nucleus = prev_gesture_;
        }
      } else if (stage == "Rest" && prev_event_ == "StartGesture") {
        if (time_step_ - start_gesture_time_ > MIN_NUCLEUS_LEN) {
          gesture_event = "StartRest";
          nucleus = prev_gesture_;
        }
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
    time_step_ = start_gesture_time_ = 0;
  }
}