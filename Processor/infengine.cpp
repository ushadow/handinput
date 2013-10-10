#include "pcheader.h"
#include "infengine.h"

namespace handinput {
  InfEngine::InfEngine() {
    using Eigen::Map;
    using Eigen::MatrixXf;
    using Eigen::VectorXf;

    MATFile* file = matOpen("G:/salience/model.mat", "r");
    mxArray* model = matGetVariable(file, "model");
    mxArray* preprocess_model = mxGetField(model, 0, "preprocessModel");
    mxArray* pca_model = mxGetCell(preprocess_model, 0);
    mxArray* pca_mean = mxGetField(pca_model, 0, "mean");
    mxArray* principal_comp = mxGetField(pca_model, 0, "pc");
    descriptor_len_ = (int) mxGetN(principal_comp);
    n_principal_comp_ = (int) mxGetM(principal_comp);
    float* pc_data = (float*) mxGetData(principal_comp);
    float* pca_mean_data = (float*) mxGetData(pca_mean);
    principal_comp_.reset(new Map<MatrixXf>(pc_data, n_principal_comp_, descriptor_len_));
    pca_mean_.reset(new Map<VectorXf>(pca_mean_data, descriptor_len_));

    mxDestroyArray(pca_model);
    mxDestroyArray(preprocess_model);
    mxDestroyArray(model);
    matClose(file);
  }

  void InfEngine::Infer(float* descriptor) {
    using Eigen::Map;
    using Eigen::VectorXf;

    Map<VectorXf> des(descriptor, descriptor_len_);
    VectorXf res = *principal_comp_ * (des - *pca_mean_);
  }
}