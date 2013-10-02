#include "pcheader.h"
#include "feature_processor.h"

namespace handinput {

#define M_PI 3.1415926f

  FeatureProcessor::FeatureProcessor(int w, int h) : w_(w), h_(h) {
    hog_.reset(new HOGDescriptor(w, h, kCellSize, kNBins));
    scaled_image_.reset(new cv::Mat(h, w, CV_8U)); 
    double_image_.reset(new cv::Mat(h, w, CV_32F));
    descriptor_.reset(new float[hog_->Length()]);
  }

  float* FeatureProcessor::Compute(cv::Mat& image) {
    cv::resize(image, *scaled_image_, cv::Size(w_, h_));
    scaled_image_->convertTo(*double_image_, CV_32F);
    hog_->Compute((float*) double_image_->data, descriptor_.get());
    return descriptor_.get();
  }

  cv::Mat FeatureProcessor::Visualize(cv::Mat& orig_image, int zoom_factor) {
    using cv::Mat;
    using cv::Size;

    Mat visu;
    cv::resize(orig_image, visu, Size(orig_image.cols * zoom_factor, 
                                      orig_image.rows * zoom_factor));

    float radRangeForOneBin = M_PI / (float) kNBins; 

    // prepare data structure: 9 orientation / gradient strenghts for each cell
    int cells_in_x_dir = w_ / kCellSize;
    int cells_in_y_dir = h_ / kCellSize;
    int totalnrofcells = cells_in_x_dir * cells_in_y_dir;
    float*** gradientStrengths = new float**[cells_in_y_dir];
    for (int y=0; y<cells_in_y_dir; y++) {
      gradientStrengths[y] = new float*[cells_in_x_dir];
      for (int x=0; x<cells_in_x_dir; x++) {
        gradientStrengths[y][x] = new float[kNBins];

        for (int bin = 0; bin < kNBins; bin++)
          gradientStrengths[y][x][bin] = 0.0;
      }
    }

    // compute gradient strengths per cell
    int descriptorDataIdx = 0;
    int wb = hog_->NxCells();
    int hb = hog_->NyCells();
    int fold = hog_->NFolds();

    for (int bin = 0; bin < kNBins; bin++) {
      for (int celly = 0; celly < hb; celly++) {
        for (int cellx = 0; cellx < wb; cellx++) {
          float gradientStrength = descriptor_[ descriptorDataIdx ];
          descriptorDataIdx++;
          gradientStrengths[celly + fold][cellx + fold][bin] += gradientStrength;
        } 
      } 
    }

    // draw cells
    for (int celly=0; celly<cells_in_y_dir; celly++) {
      for (int cellx=0; cellx<cells_in_x_dir; cellx++) {
        int drawX = cellx * kCellSize;
        int drawY = celly * kCellSize;

        int mx = drawX + kCellSize / 2;
        int my = drawY + kCellSize / 2;

        cv::rectangle(visu, cvPoint(drawX*zoom_factor,drawY*zoom_factor), 
          cvPoint((drawX + kCellSize) * zoom_factor, (drawY + kCellSize)*zoom_factor), 
          CV_RGB(100,100,100), 1);

        // draw in each cell all 9 gradient strengths
        for (int bin = 0; bin < kNBins; bin++) {
          float currentGradStrength = gradientStrengths[celly][cellx][bin];

          // no line to draw?
          if (currentGradStrength==0)
            continue;

          float currRad = bin * radRangeForOneBin + radRangeForOneBin/2;

          float dirVecX = cos( currRad );
          float dirVecY = sin( currRad );
          float maxVecLen = kCellSize / 2;
          float scale = 2.5; // just a visualization scale, to see the lines better

          // compute line coordinates
          float x1 = mx - dirVecX * currentGradStrength * maxVecLen * scale;
          float y1 = my - dirVecY * currentGradStrength * maxVecLen * scale;
          float x2 = mx + dirVecX * currentGradStrength * maxVecLen * scale;
          float y2 = my + dirVecY * currentGradStrength * maxVecLen * scale;

          // draw gradient visualization
          cv::line(visu, cvPoint((int) (x1 * zoom_factor), (int) (y1 * zoom_factor)), 
            cvPoint((int) (x2 * zoom_factor), (int) (y2 * zoom_factor)), 
            CV_RGB(0,255,0), 1);
        } // for (all bins)
      } // for (cellx)
    } // for (celly)

    // don't forget to free memory allocated by helper data structures!
    for (int y=0; y<cells_in_y_dir; y++) {
      for (int x=0; x<cells_in_x_dir; x++) {
        delete[] gradientStrengths[y][x];            
      }
      delete[] gradientStrengths[y];
    }
    delete[] gradientStrengths;

    return visu;
  }
}
