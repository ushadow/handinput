#include "pcheader.h"
#include "hand_tracker.h"

namespace handinput {

  HandTracker::HandTracker(int width, int height) : databuffer_(kBufferSize), width_(width), 
    height_(height) {

      temporal_mask_.push_back(0.8f);
      temporal_mask_.push_back(0.15f);
      temporal_mask_.push_back(0.05f);

      frame_ = cvCreateImage(cvSize(width, height), IMGTYPE, 1);
  }

  HandTracker::~HandTracker() {
    if (frame_) cvReleaseImage(&frame_);
  }

  // image: type IPL_DEPTH_8U
  void HandTracker::Update(IplImage* image, IplImage* dst) {
    cvScale(image, frame_, 1.0 / 255.0, 0);
    databuffer_.Update(frame_);
    databuffer_.TemporalConvolve(&cv::Mat(dst), temporal_mask_);
  }
}