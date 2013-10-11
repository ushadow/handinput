#include "pcheader.h"
#include "infengine.h"

namespace handinput {
  InfEngine::InfEngine() : principal_comp_(NULL, 0, 0), pca_mean_(NULL, 0), std_mu_(NULL, 0), 
    std_sigma_(NULL, 0) {
    using Eigen::Map;
    using Eigen::MatrixXf;
    using Eigen::VectorXf;

    MATFile* file = matOpen("G:/salience/model.mat", "r");
    mxArray* model = matGetVariable(file, "model");
    mxArray* preprocess_model = mxGetField(model, 0, "preprocessModel");
    mxArray* pca_model = mxGetCell(preprocess_model, 0);
    pca_mean_mx_ = mxGetField(pca_model, 0, "mean");
    principal_comp_mx_ = mxGetField(pca_model, 0, "pc");
    descriptor_len_ = (int) mxGetN(principal_comp_mx_);
    n_principal_comp_ = (int) mxGetM(principal_comp_mx_);
    float* pc_data = (float*) mxGetData(principal_comp_mx_);
    float* pca_mean_data = (float*) mxGetData(pca_mean_mx_);
    new (&principal_comp_) Map<MatrixXf>(pc_data, n_principal_comp_, descriptor_len_); 
    new (&pca_mean_) Map<VectorXf>(pca_mean_data, descriptor_len_);

    mxArray* std_model = mxGetCell(preprocess_model, 1);
    std_mu_mx_ = mxGetField(std_model, 0, "mu");
    std_sigma_mx_ = mxGetField(std_model, 0, "sigma");
    feature_len_ = (int) mxGetM(std_mu_mx_);
    float* mu_data = (float*) mxGetData(std_mu_mx_);
    float* sigma_data = (float*) mxGetData(std_sigma_mx_);
    new (&std_mu_) Map<VectorXf>(mu_data, feature_len_);
    new (&std_sigma_) Map<VectorXf>(sigma_data, feature_len_);

    mxDestroyArray(std_model);
    mxDestroyArray(pca_model);
    mxDestroyArray(preprocess_model);
    mxDestroyArray(model);
    matClose(file);
  }

  InfEngine::~InfEngine() {
    mxDestroyArray(pca_mean_mx_);
    mxDestroyArray(principal_comp_mx_);
    mxDestroyArray(std_mu_mx_);
    mxDestroyArray(std_sigma_mx_);
  }

  void InfEngine::Infer(float* feature, float* descriptor) {
    using Eigen::Map;
    using Eigen::VectorXf;

    Map<VectorXf> des(descriptor, descriptor_len_);
    VectorXf res(n_principal_comp_);
    res.noalias() = principal_comp_ * (des - pca_mean_);

    Map<VectorXf> partial_feature(feature, feature_len_ - n_principal_comp_);
    VectorXf full_feature(feature_len_);
    full_feature << partial_feature, res;
    full_feature = (full_feature - std_mu_).cwiseProduct(std_sigma_);
  }

  void InfEngine::InitHMM(mxArray* mx_model) {
    using std::vector;

    mxArray* mx_hmm_model = mxGetField(mx_model, 0, "model");
    mxArray* mx_prior = mxGetCell(mxGetField(mx_hmm_model, 0, "prior"), 0);
    mxArray* mx_transmat = mxGetCell(mxGetField(mx_hmm_model, 0, "transmat"), 0);
    mxArray* mx_mu = mxGetCell(mxGetField(mx_hmm_model, 0, "mu"), 0);
    mxArray* mx_sigma = mxGetCell(mxGetField(mx_hmm_model, 0, "Sigma"), 0);
    mxArray* mx_mxmat = mxGetCell(mxGetField(mx_hmm_model, 0, "mixmat"), 0);
    float* priror_data = (float*) mxGetData(mx_prior);
    float* transmat_data = (float*) mxGetData(mx_transmat);
    vector<MixGaussian*> mixgaussians;
  }
}