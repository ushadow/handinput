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
    mxArray* pca_model = mxGetCell(preprocess_model, 0);
    mxArray* std_model = mxGetCell(preprocess_model, 1);

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

    hmm_.reset(HMM::CreateFromMxArray(mxGetField(model, 0, "infModel")));

    mxDestroyArray(model);
    matClose(file);
  }

  float InfEngine::Update(float* raw_feature) {
    using Eigen::Map;
    using Eigen::VectorXf;

    int motion_feature_len = feature_len_ - n_principal_comps_;

    Map<VectorXf> des(raw_feature + motion_feature_len, descriptor_len_);
    VectorXf res(n_principal_comps_);
    res.noalias() = principal_comp_ * (des - pca_mean_);

    Map<VectorXf> motion_feature(raw_feature, motion_feature_len);
    VectorXf full_feature(feature_len_);
    full_feature << motion_feature, res;
    // Normalize feature.
    full_feature = (full_feature - std_mu_).cwiseQuotient(std_sigma_);

    float loglik = hmm_->Fwdback(full_feature);
    int state = hmm_->MostLikelyState();
    int gesture  = state / 6 + 1;
    std::cout << "most likely state = " << hmm_->MostLikelyState() << std::endl;
    std::cout << "most likely gesture = " << gesture << std::endl;
    return loglik;
  }
}