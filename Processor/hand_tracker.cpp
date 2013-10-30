#include "pcheader.h"
#include "hand_tracker.h"

namespace handinput {
  const std::string HandTracker::kWaveletName = "db3";

  HandTracker::HandTracker(int width, int height) : databuffer_(kBufferSize), width_(width), 
    height_(height), vec1_(height, std::vector<double>(width)),
    idwt_output_(height, std::vector<double>(width)) {

      temporal_mask_.push_back(0.8);
      temporal_mask_.push_back(0.15);
      temporal_mask_.push_back(0.05);

      filtcoef(kWaveletName, l1_, h1_, l2_, h2_);

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

  void HandTracker::WaveletReconstruction(IplImage* image) {
    for (int i = 0; i < height_; i++) 
      for (int j = 0; j < width_; j++) {
        unsigned char temp = ((uchar*)(image->imageData + i * image->widthStep))[j];
        vec1_[i][j] = (double) temp;
      }

    dwt_2d_sym(vec1_, kJ, kWaveletName, output_, flag_, length_);
    idwt_2d_sym(output_, flag_, kWaveletName, idwt_output_, length_);

    for (int i = 0; i < height_; i++)
      for (int j = 0; j < width_; j++) {
        ((uchar*) (image->imageData + i * image->widthStep))[j] = (uchar) (idwt_output_[i][j]);
      }
  }
}