#include "pcheader.h"
#include "databuffer.h"

namespace handinput {
  DataBuffer::DataBuffer(int size) : buffer_size_(size), width_(0), height_(0) {}

  void DataBuffer::Update(const cv::Mat& newframe) {
    if(buffer_.empty()) {
      frame_indices_.Init(buffer_size_);
      width_ = newframe.cols;
      height_ = newframe.rows;
      buffer_.create(buffer_size_, width_ * height_, DATATYPE);
    } 
    int k = frame_indices_.Add();
    memcpy((void*)(buffer_.data + buffer_.step[0] * k) ,
      newframe.data, buffer_.step[0]);
  }

  void DataBuffer::TemporalConvolve(cv::Mat* dst, std::vector<double> mask) {
    using cv::Mat;
    int	tfsz = (int)mask.size();
    int i;

    if ((int)mask.size() < buffer_size_)
      for(i = (int)mask.size(); i < buffer_size_; i++)
        mask.push_back(0);

    std::vector<int> Sorted = frame_indices_.GetSortedIndices();

    IMG_ELEM_TYPE* filter = new IMG_ELEM_TYPE[buffer_size_];
    for (i = 0; i < buffer_size_; i++)
      filter[Sorted[i]] = (IMG_ELEM_TYPE)mask[i];
    Mat fil(1, buffer_size_, DATATYPE, filter);

    Mat rdst = dst->reshape(1, 1);
    rdst = fil * buffer_; 
    delete[] filter;
  }
}