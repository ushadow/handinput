#pragma once
#include "pcheader.h"
#include "stbuffer.h"

namespace handinput {
  class DataBuffer {
  public:
    DataBuffer(int size);
    ~DataBuffer();
    void Update(IplImage* image);
    void TemporalConvolve(IplImage* dst, std::vector<double> mask);
  private:
    CvMat* buffer_;
    int buffer_size_;
    int width_, height_;
    CircularIndex frame_indices_;

    DataBuffer(const DataBuffer&);
    DataBuffer& operator=(const DataBuffer&);
  };
}