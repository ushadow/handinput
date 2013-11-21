#pragma once
#include "pcheader.h"
#include "databuffer.h"

namespace handinput {
  class PROCESSOR_API HandTracker {
  public:
    // width: width of the image.
    // height: height of the image.
    HandTracker(int width, int height);
    ~HandTracker();
    void Update(IplImage* image, IplImage* dst);
  private:
    static const int kBufferSize = 3;

    std::vector<float> temporal_mask_;
    std::vector<int> length_;
    DataBuffer databuffer_;
    IplImage* frame_;
    int width_, height_;
  };
}