#pragma once
#include "pcheader.h"

namespace handinput {
  class PROCESSOR_API InfEngine {
  public:
    InfEngine();
    void Infer(float* descriptor);
    int descriptor_len() { return descriptor_len_; }
    int n_principal_comp() { return n_principal_comp_; }
  private:
    int descriptor_len_, n_principal_comp_;
    // Each row is a principal component.
    std::unique_ptr<Eigen::Map<Eigen::MatrixXf>> principal_comp_; 
    std::unique_ptr<Eigen::Map<Eigen::VectorXf>> pca_mean_;
  };
}