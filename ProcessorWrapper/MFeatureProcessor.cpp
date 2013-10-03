#include "Stdafx.h"
#include "MFeatureProcessor.h"

namespace handinput {

  MFeatureProcessor::MFeatureProcessor(int w, int h) {
    processor_ = new FeatureProcessor(w, h);
  }

  void MFeatureProcessor::Compute(System::IntPtr image_ptr) {
    IplImage* image = reinterpret_cast<IplImage*>(image_ptr.ToPointer()); 
    cv::Mat mat(image);
    processor_->Compute(mat, true);
 }

  void MFeatureProcessor::Visualize(System::IntPtr image_ptr) {   
    using cv::Mat;
    IplImage* image = reinterpret_cast<IplImage*>(image_ptr.ToPointer()); 
    Mat mat(image);
    Mat visu = processor_->VisualizeHOG(mat);
    std::string windowName = "Test Window";
    cv::namedWindow(windowName);
    cv::imshow(windowName, visu);
    cv::waitKey(0);
    cv::destroyWindow(windowName.c_str());
  } 
}