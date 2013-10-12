#include "pcheader.h"
#include "infengine.h"

namespace handinput {
  InfEngine::InfEngine() {
    using Eigen::Map;
    using Eigen::MatrixXf;
    using Eigen::VectorXf;

    MATFile* file = matOpen("G:/salience/model.mat", "r");
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

    InitHMM(mxGetField(model, 0, "infModel"));

    mxDestroyArray(model);
    matClose(file);
  }

  void InfEngine::Infer(float* feature, float* descriptor) {
    using Eigen::Map;
    using Eigen::VectorXf;

    Map<VectorXf> des(descriptor, descriptor_len_);
    VectorXf res(n_principal_comps_);
    res.noalias() = principal_comp_ * (des - pca_mean_);

    Map<VectorXf> partial_feature(feature, feature_len_ - n_principal_comps_);
    VectorXf full_feature(feature_len_);
    full_feature << partial_feature, res;
    full_feature = (full_feature - std_mu_).cwiseProduct(std_sigma_);
  }

  void InfEngine::InitHMM(mxArray* mx_model) {
    using std::vector;
    using std::unique_ptr;
    using Eigen::Map;
    using Eigen::VectorXf;
    using Eigen::MatrixXf;

    mxArray* mx_hmm_model = mxGetField(mx_model, 0, "model");
    mxArray* mx_prior = mxGetCell(mxGetField(mx_hmm_model, 0, "prior"), 0);
    mxArray* mx_transmat = mxGetCell(mxGetField(mx_hmm_model, 0, "transmat"), 0);
    mxArray* mx_mu = mxGetCell(mxGetField(mx_hmm_model, 0, "mu"), 0);
    mxArray* mx_sigma = mxGetCell(mxGetField(mx_hmm_model, 0, "Sigma"), 0);
    mxArray* mx_mixmat = mxGetCell(mxGetField(mx_hmm_model, 0, "mixmat"), 0);

    const float* prior_data = (const float*) mxGetData(mx_prior);
    const float* transmat_data = (const float*) mxGetData(mx_transmat);
    float* mixmat_data = (float*) mxGetData(mx_mixmat);
    float* mu_data = (float*) mxGetData(mx_mu);
    float* sigma_data = (float*) mxGetData(mx_sigma);

    int n_states = (int) mxGetNumberOfElements(mx_prior);
    int n_mixtures = (int) mxGetM(mx_mixmat);
    int sigma_len = feature_len_ * feature_len_;

    vector<unique_ptr<const MixGaussian>> mixgaussians;
    for (int i = 0; i < n_states; i++) {
      // mixmat_data is m x n matrix where each colum is the mixture probability for a state.
      Map<VectorXf> mix(mixmat_data + i * n_mixtures, n_mixtures);  
      vector<unique_ptr<const Gaussian>> gaussians;
      for (int j = 0; j < n_mixtures; j++) {
        Map<VectorXf> mu(mu_data + feature_len_ * (i * n_mixtures + j), feature_len_);
        Map<MatrixXf> sigma(sigma_data + sigma_len * (i * n_mixtures + j), feature_len_, 
          feature_len_);
        unique_ptr<const Gaussian> p(new Gaussian(mu, sigma));
        gaussians.push_back(std::move(p));
      }
      unique_ptr<const MixGaussian> mixgauss(new MixGaussian(mix, gaussians));
      mixgaussians.push_back(std::move(mixgauss));
    }
    hmm_.reset(new HMM(VectorXf::Map(prior_data, feature_len_),
      MatrixXf::Map(transmat_data, feature_len_, feature_len_), mixgaussians));
  }
}