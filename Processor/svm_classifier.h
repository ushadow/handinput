#pragma once
#include "pcheader.h"

namespace handinput {
  class PROCESSOR_API SVMClassifier {
  public:
    SVMClassifier(const char* model_file, bool predict_prob = true);
    ~SVMClassifier();
    Eigen::VectorXd Predict(const Eigen::Ref<const Eigen::VectorXf> attr);
  private:
    svm_model *model_;
    int svm_type_;
    bool predict_prob_;
    Eigen::VectorXd prob_estimates_;
  };
}