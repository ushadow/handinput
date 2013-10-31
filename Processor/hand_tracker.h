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
    static const int kJ = 3;
    static const std::string kWaveletName;

    std::vector<double> l1_, h1_, l2_, h2_, output_, flag_;
    std::vector<float> temporal_mask_;
    std::vector<int> length_;
    DataBuffer databuffer_;
    IplImage* frame_;
    std::vector<std::vector<double>> vec1_, idwt_output_;
    int width_, height_;

    void Init(IplImage* image);
    void WaveletReconstruction(IplImage* image);
  };
}