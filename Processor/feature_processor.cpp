#include "pcheader.h"
#include "feature_processor.h"

namespace handinput {

  const std::string FeatureProcessor::kDepthWindowName = "Depth";
  const std::string FeatureProcessor::kColorWindowName = "Color";

  FeatureProcessor::FeatureProcessor(int w, int h, int buffer_size) : w_(w), h_(h), 
      pos_buffer_(buffer_size), temporal_mask_(buffer_size, 1.0f / buffer_size) {
    hog_.reset(new HOGDescriptor(w, h, kCellSize, kNBins));
    resized_image_.reset(new cv::Mat(h, w, CV_8U)); 
    float_image_.reset(new cv::Mat(h, w, CV_32F));
    feature_.reset(new float[hog_->Length() + kMotionFeatureLen]);
    descriptor_ = feature_.get() + kMotionFeatureLen;
  }

  FeatureProcessor::~FeatureProcessor() {
    cv::destroyAllWindows();
  }

  float* FeatureProcessor::Compute(float x, float y, float z, cv::Mat& image, cv::Mat& skin,
                                   bool visualize) {
    using Eigen::Vector3f;
    using cv::Mat;
    using cv::Mat_;
    Mat pos = (Mat_<float>(1, 3) << x, y, z);
    pos_buffer_.Update(pos);
    
    if (pos_buffer_.IsFull())
      pos_buffer_.TemporalConvolve(&pos, temporal_mask_);
    bool updated = false;
    if (!prev_pos_.empty()) {
      Mat v = pos - prev_pos_;
      if (!prev_v_.empty()) {
        Mat a = v - prev_v_;
        CopyMatToArray(pos, feature_.get(), 0);
        CopyMatToArray(v, feature_.get(), 3);
        CopyMatToArray(a, feature_.get(), 6);
        Compute(image, kDepthWindowName, visualize);
        if (skin.cols > 0)
          Compute(skin, kColorWindowName, visualize);
        updated = true;
      }
      prev_v_ = v;
    }
    prev_pos_ = pos;

    if (updated)
      return feature_.get();
    else return NULL;
  }

  void FeatureProcessor::CopyMatToArray(const cv::Mat& v, float* a, int start) {
    std::copy(v.begin<float>(), v.end<float>(), a + start);
  }

  // Resizes the image and converts the image to float point values. 
  float* FeatureProcessor::Compute(cv::Mat& image, std::string window_name, bool visualize) {
    // Uses the default linear interpolation.
    cv::resize(image, *resized_image_, cv::Size(w_, h_));
    resized_image_->convertTo(*float_image_, CV_32F);

    hog_->Compute((float*) float_image_->data, descriptor_);
    if (visualize) {
      cv::Mat vis = VisualizeHOG(*resized_image_);
      DisplayImage(vis, window_name);
    }
    return descriptor_;
  }

  cv::Mat FeatureProcessor::VisualizeHOG(cv::Mat& orig_image, int zoom_factor) {
    using cv::Mat;
    using cv::Size;

    Mat visu;
    cv::resize(orig_image, visu, Size(orig_image.cols * zoom_factor, 
                                      orig_image.rows * zoom_factor));
    float radRangeForOneBin = (float) M_PI / kNBins; 

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

        cv::rectangle(visu, cv::Point(drawX * zoom_factor, drawY * zoom_factor), 
          cv::Point((drawX + kCellSize) * zoom_factor, (drawY + kCellSize)*zoom_factor), 
          cv::Scalar(100,100,100), 1);

        // draw in each cell all 9 gradient strengths
        for (int bin = 0; bin < kNBins; bin++) {
          float currentGradStrength = gradientStrengths[celly][cellx][bin];

          // no line to draw?
          if (currentGradStrength == 0)
            continue;

          float currRad = bin * radRangeForOneBin + radRangeForOneBin / 2;

          float dirVecX = cos(currRad);
          float dirVecY = sin(currRad);
          float maxVecLen = kCellSize / 2;
          float scale = 2.5; // just a visualization scale, to see the lines better

          // compute line coordinates
          float x1 = mx - dirVecX * currentGradStrength * maxVecLen * scale;
          float y1 = my - dirVecY * currentGradStrength * maxVecLen * scale;
          float x2 = mx + dirVecX * currentGradStrength * maxVecLen * scale;
          float y2 = my + dirVecY * currentGradStrength * maxVecLen * scale;

          // draw gradient visualization
          cv::line(visu, cv::Point((int) (x1 * zoom_factor), (int) (y1 * zoom_factor)), 
            cv::Point((int) (x2 * zoom_factor), (int) (y2 * zoom_factor)), 
            cv::Scalar(0,255,0), 1);
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

  void FeatureProcessor::DisplayImage(cv::Mat& image, std::string window_name) {
    // If the window with the same name already exists, the function does nothing.
    cv::namedWindow(window_name);
    cv::imshow(window_name, image);
  }
}
