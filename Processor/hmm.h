#pragma once
#include "pcheader.h"
#include "mixgaussian.h"

namespace handinput {
  class PROCESSOR_API HMM {
  public:
    // Factory method for creating HMM. The caller must take the ownership of the HMM object.
    static HMM* CreateFromMxArray(mxArray* mx_model);

    // prior: makes a copy of prioir.
    // transmat: creates a transpose of transmat.
    HMM(const Eigen::Ref<const Eigen::VectorXf> prior, 
      const Eigen::Ref<const Eigen::MatrixXf> transmat, 
      std::vector<std::unique_ptr<const MixGaussian>>& mixgaussians);

    int n_states() const { return n_states_; }
    int feature_len() const { return feature_len_; }
    const Eigen::VectorXf* prior() const { return &prior_; }

    // Transition matrix transposed.
    const Eigen::MatrixXf* transmat_t() const { return &transmat_t_; }

    const MixGaussian* MixGaussianAt(int index) const;

    float Fwdback(const Eigen::Ref<const Eigen::VectorXf> x);

  private:
    Eigen::VectorXf alpha_, prior_;
    std::vector<std::unique_ptr<const MixGaussian>> mixgaussians_;
    int n_states_, feature_len_;
    Eigen::VectorXf obslik_;
    Eigen::MatrixXf transmat_t_;
    float loglik_;

    HMM(const HMM&) {}
    HMM& operator=(const HMM&) { return *this; }
    void ComputeObslik(const Eigen::Ref<const Eigen::VectorXf> x);
    float Normalize();
    void CheckRI();
  };
}