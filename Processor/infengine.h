#pragma once
#include "pcheader.h"
#include "hmm.h"

namespace handinput {
  class PROCESSOR_API InfEngine {
  public:
    InfEngine(const std::string& model_file);
    ~InfEngine() {}

    // Accessors
    int feature_len() const { return feature_len_; }
    int descriptor_len() const { return descriptor_len_; }
    int n_principal_comps() const { return n_principal_comps_; }
    const Eigen::VectorXf* pca_mean() const { return &pca_mean_; }
    const Eigen::MatrixXf* principal_comp() const { return &principal_comp_; }
    const Eigen::VectorXf* std_mu() const { return &std_mu_; }
    const Eigen::VectorXf* std_sigma() const { return &std_sigma_; }
    const HMM* hmm() const { return hmm_.get(); }

    // feature: continous features besides the image descriptor.
    void Update(float* feature, float* descriptor);
  private:
    int descriptor_len_, n_principal_comps_, feature_len_;
    // Each row is a principal component.
    Eigen::MatrixXf principal_comp_; 
    Eigen::VectorXf pca_mean_;
    Eigen::VectorXf std_mu_;
    Eigen::VectorXf std_sigma_;
    std::unique_ptr<HMM> hmm_;

    InfEngine(const InfEngine&) {}
    InfEngine& operator=(const InfEngine&) { return *this; }
    void InitHMM(mxArray* mx_model);
  };
}