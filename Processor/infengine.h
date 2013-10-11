#pragma once
#include "pcheader.h"
#include "hmm.h"

namespace handinput {
  class PROCESSOR_API InfEngine {
  public:
    InfEngine();
    ~InfEngine();

    // feature: continous features besides the image descriptor.
    void Infer(float* feature, float* descriptor);
    int descriptor_len() { return descriptor_len_; }
    int n_principal_comp() { return n_principal_comp_; }
  private:
    int descriptor_len_, n_principal_comp_, feature_len_;
    // Each row is a principal component.
    Eigen::Map<Eigen::MatrixXf> principal_comp_; 
    Eigen::Map<Eigen::VectorXf> pca_mean_;
    Eigen::Map<Eigen::VectorXf> std_mu_;
    Eigen::Map<Eigen::VectorXf> std_sigma_;
    mxArray *principal_comp_mx_, *pca_mean_mx_, *std_mu_mx_, *std_sigma_mx_;
    std::unique_ptr<HMM> hmm_;

    void InitHMM(mxArray* mx_model);
  };
}