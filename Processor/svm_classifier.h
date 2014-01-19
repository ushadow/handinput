#pragma once
#include "pcheader.h"

namespace handinput {
  class SVMClassifier {
  public:
    SVMClassifier(std::string model_file, bool predict_prob = false);
    ~SVMClassifier();
    double* predict(std::vector<double> attr);
  private:
    svm_node *x_;
    svm_model *model_;
    int svm_type_;
    bool predict_prob_;
    double *prob_estimates_;
  };
}