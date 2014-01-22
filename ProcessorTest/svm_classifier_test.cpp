#include <stdlib.h>
#include "gtest\gtest.h"
#include "svm_classifier.h"

TEST(SVMClassifierTest, PredictTest) {
  using Eigen::VectorXf;

  static const std::string model_file = "../../../data/svm_train_hand.txt.model";
  handinput::SVMClassifier clf(model_file.c_str());

  VectorXf attr = VectorXf::Zero(35);
  clf.Predict(attr);
  clf.Predict(attr);
}