#include "pcheader.h"
#include "gesture_event_detector.h"

namespace handinput {
  GestureEventDetector::GestureEventDetector() : time_step_(0), start_gesture_time_(0) {}

  // Gesture phases: PreStroke, Gesture, PostStroke
  // Gesture events: StratPreStroke, StartGesture, StopGesture
  void GestureEventDetector::Detect(const std::string& gesture, const std::string& phase,
    json_spirit::mObject* result) {
    using std::string;

    time_step_++;

    string gesture_event; // Default to be an empty string.
    string nucleus = gesture;

    //When gesture phase changes.
    if (phase != prev_phase_) {
#ifdef _DEBUG
      std::cout << "prev event = " << prev_event_ << std::endl;
#endif
      if (phase == "PreStroke") {
        gesture_event = "StartPreStroke";
        start_gesture_time_ = time_step_;
      } else if (phase == "Nucleus") {
        gesture_event = "StartNucleus";
        start_gesture_time_ = time_step_;
      } else if (phase == "PostStroke" && prev_event_ == "StartNucleus") {
        if (time_step_ - start_gesture_time_ > MIN_NUCLEUS_LEN) {
          gesture_event = "StopNucleus";
          nucleus = prev_gesture_;
        }
      } else if (phase == "Rest" && prev_event_ == "StartNucleus") {
        if (time_step_ - start_gesture_time_ > MIN_NUCLEUS_LEN) {
          gesture_event = "StopNucleus";
          nucleus = prev_gesture_;
        }
      }
    }
    prev_phase_ = phase;
    prev_gesture_ = nucleus;
    if (gesture_event != "")
      prev_event_ = gesture_event;
    (*result)["gesture"] = nucleus;
    (*result)["phase"] = phase;
    (*result)["eventType"] = gesture_event;
  }

  void GestureEventDetector::Reset() {
    prev_phase_ = "";
    time_step_ = start_gesture_time_ = 0;
  }
}