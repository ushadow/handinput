#include "pcheader.h"
#include "databuffer.h"

namespace handinput {
  DataBuffer::DataBuffer(int size) : buffer_(NULL), buffer_size_(size), width_(0), height_(0) {}

  DataBuffer::~DataBuffer() {
    if (buffer_)
      cvReleaseMat(&buffer_);
  }  void DataBuffer::Update(IplImage* newframe) {
    if(!buffer_) {
      frame_indices_.Init(buffer_size_);
      width_ = newframe->width;
      height_ = newframe->height;
      std::cout << width_ << std::endl;
      buffer_ = cvCreateMat(buffer_size_, width_ * height_, DATATYPE);
    } 
    int k = frame_indices_.Add();
    memcpy((void*)(buffer_->data.ptr + buffer_->step * k) ,
      newframe->imageData, buffer_->step);
  }

  void DataBuffer::TemporalConvolve(IplImage* dst, std::vector<double> mask) {
    int	tfsz = (int)mask.size();
    int i;

    if ((int)mask.size() < buffer_size_)
      for(i = (int)mask.size(); i < buffer_size_; i++)
        mask.push_back(0);

    std::vector<int> Sorted = frame_indices_.GetSortedIndices();

    CvMat *fil = cvCreateMat(1, buffer_size_, DATATYPE);
    IMG_ELEM_TYPE* filter=new IMG_ELEM_TYPE[buffer_size_];

    for (i = 0; i < buffer_size_; i++)
      filter[Sorted[i]] = (IMG_ELEM_TYPE)mask[i];

    for (i=0; i < buffer_size_;i++)
      cvmSet(fil, 0, i, filter[i]);

    CvMat *rdst, dsthdr;
    rdst = cvReshape(dst, &dsthdr, 0, 1);
    cvMatMul(fil, buffer_, rdst);
    delete[] filter;
    cvReleaseMat(&fil);
  }
}