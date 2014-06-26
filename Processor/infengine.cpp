#include "pcheader.h"
#include "infengine.h"

namespace handinput {
  InfEngine::InfEngine(const std::string& model_file) {
    using Eigen::Map;
    using Eigen::MatrixXf;
    using Eigen::VectorXf;

    MATFile* file = matOpen(model_file.c_str(), "r");
    // Copies an mxArray out of a MAT-file. Use mxDestroyArray to destroy the mxArray created
    // by this routine.
    mxArray* model = matGetVariable(file, "model");
    mxArray* preprocess_model = mxGetField(model, 0, "preprocessModel");
    size_t num_preprocess_models = mxGetNumberOfElements(preprocess_model);
    int i = 0;
    mxArray* pca_model = mxGetCell(preprocess_model, i);
    i++;
    if (num_preprocess_models > 2) {
      mxArray* svm_model = mxGetCell(preprocess_model, i);
      InitSVM(svm_model);
      i++;
    }
    mxArray* std_model = mxGetCell(preprocess_model, i);

    mxArray* pca_mean_mx = mxGetField(pca_model, 0, "mean");
    mxArray* principal_comp_mx = mxGetField(pca_model, 0, "pc");
    mxArray* std_mu_mx = mxGetField(std_model, 0, "mu");
    mxArray* std_sigma_mx = mxGetField(std_model, 0, "sigma");

    descriptor_len_ = (int) mxGetN(principal_comp_mx);
    n_principal_comps_ = (int) mxGetM(principal_comp_mx);
    feature_len_ = (int) mxGetM(std_mu_mx);

    float* pc_data = (float*) mxGetData(principal_comp_mx);
    float* pca_mean_data = (float*) mxGetData(pca_mean_mx);
    principal_comp_ = Map<MatrixXf>(pc_data, n_principal_comps_, descriptor_len_); 
    pca_mean_ = Map<VectorXf>(pca_mean_data, descriptor_len_);

    float* mu_data = (float*) mxGetData(std_mu_mx);
    float* sigma_data = (float*) mxGetData(std_sigma_mx);
    std_mu_ = Map<VectorXf>(mu_data, feature_len_);
    std_sigma_ = Map<VectorXf>(sigma_data, feature_len_);

    mxArray* param = mxGetField(model, 0, "param");
    mxArray* vocabulary_size_mx = mxGetField(param, 0, "vocabularySize");
    mxArray* gesture_labels = mxGetField(param, 0, "gestureLabel");
    mxArray* kinect_sample_rate_mx = mxGetField(param, 0, "kinectSampleRate");
    mxArray* lag_max = mxGetField(param, 0, "L");
    
    kinect_sample_rate_ = (int)mxGetScalar(kinect_sample_rate_mx);
    n_vocabularies_ = (int)mxGetScalar(vocabulary_size_mx);
    InitGestureLabels(gesture_labels);
    hand_pose_labels_.push_back("Unknown");
    int lag = (int)mxGetScalar(lag_max);

    // Initialize HMM model.
    mxArray* inf_model = mxGetField(model, 0, "infModel");
    if (inf_model != NULL)
      hmm_.reset(HMM::CreateFromMxArray(inf_model, lag));
    mxDestroyArray(model);
    matClose(file);
  }

  // Updates the inference result.
  // Hand position is in mm.
  std::string InfEngine::Update(float* raw_feature) {
    using Eigen::Map;
    using Eigen::VectorXf;
    using Eigen::VectorXd;
    using std::string;

    int gesture_index = 0;
    int handpose_index = 0;
    string stage = "Unknown";

    json_spirit::mObject result;

    if (raw_feature != NULL) {
      int motion_feature_len = feature_len_ - n_principal_comps_;

      Map<VectorXf> des(raw_feature + motion_feature_len, descriptor_len_);
      VectorXf res(n_principal_comps_);
      res.noalias() = principal_comp_ * (des - pca_mean_);

      Map<VectorXf> motion_feature(raw_feature, motion_feature_len);
      VectorXf full_feature(feature_len_);
      full_feature << motion_feature, res;
      // Normalize feature.
      full_feature = (full_feature - std_mu_).cwiseQuotient(std_sigma_);

      if (svm_classifier_) {
        VectorXd prob = svm_classifier_->Predict(full_feature);
        VectorXd::Index max_index;
        prob.maxCoeff(&max_index);
        handpose_index = (int) max_index;
      }

      if (hmm_) {
        hmm_->Fwdback(full_feature);
        gesture_index = hmm_->MostLikelyLabelIndex();
        stage = hmm_->MostLikelyStage();
      }

      result["rightX"] = (int) (motion_feature[0] * 1000);
      result["rightY"] = (int) (motion_feature[1] * 1000);
    }

    const string& gesture_label = gesture_labels_[gesture_index];
    gesture_event_detector_.Detect(gesture_label, stage, &result);
    std::string s = write(result, json_spirit::pretty_print | json_spirit::raw_utf8);
    return s;
  }

  void InfEngine::InitGestureLabels(mxArray* mx_gesture_labels) {
    // Gesture label index from Matlab is 1-based. Put a dummy at the beginning.
    gesture_labels_.push_back("Unknown");
    if (mx_gesture_labels == NULL)
      return;
    for (int i = 0; i < mxGetNumberOfElements(mx_gesture_labels); i++) {
      std::string s = std::string(mxArrayToString(mxGetCell(mx_gesture_labels, i)));
      gesture_labels_.push_back(s);
    }
  }

  void InfEngine::InitSVM(mxArray* svm_model) {
    mxArray* svm_file_mx = mxGetField(svm_model, 0, "file");
    mxArray* svm_labels_mx = mxGetField(svm_model, 0, "labels");
    char* svm_model_file = mxArrayToString(svm_file_mx);
    if (svm_model_file != NULL) {
      svm_classifier_.reset(new SVMClassifier(svm_model_file));
      std::cout << "SVM initialized" << std::endl;
    }
    for (int i = 0; i < mxGetNumberOfElements(svm_labels_mx); i++) {
      std::string s = std::string(mxArrayToString(mxGetCell(svm_labels_mx, i)));
      hand_pose_labels_.push_back(s);
    }
  }

  void InfEngine::Reset() { 
    if (hmm_) hmm_->Reset(); 
    gesture_event_detector_.Reset();
  }
}