#include "pcheader.h"
#include "MFeatureProcessor.h"

namespace handinput {

  MFeatureProcessor::MFeatureProcessor(int w, int h) {
    processor_ = new FeatureProcessor(w, h);
  }

  void MFeatureProcessor::Compute(System::IntPtr image_ptr) {
    IplImage* image = reinterpret_cast<IplImage*>(image_ptr.ToPointer()); 
    cv::Mat mat(image);
    processor_->Compute(mat);
 }

  void MFeatureProcessor::Visualize(System::IntPtr image_ptr) {   
    using cv::Mat;
    IplImage* image = reinterpret_cast<IplImage*>(image_ptr.ToPointer()); 
    Mat mat(image);
    Mat visu = processor_->Visualize(mat);
    std::string windowName = "Test Window";
    cvNamedWindow(windowName.c_str());
    cv::imshow(windowName, visu);
    cvWaitKey(0);
    cvDestroyWindow(windowName.c_str());
  } 
}