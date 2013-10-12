#pragma once
#include "pcheader.h"
#include "mixgaussian.h"

namespace handinput {
  class HMM {
  public:
    // prior: makes a copy of prioir.
    // transmat: creates a transpose of transmat.
    HMM(const Eigen::Ref<const Eigen::VectorXf> prior, 
      const Eigen::Ref<const Eigen::MatrixXf> transmat, 
      std::vector<std::unique_ptr<const MixGaussian>>& mixgaussians);

    int n_states() const { return n_states_; }
    const Eigen::VectorXf* prior() const { return &prior_; }

    // Transition matrix transposed.
    const Eigen::MatrixXf* transmat_t() const { return &transmat_t_; }

    const MixGaussian* GetMixGaussian(int index) const;

    void Fwdback(const Eigen::Ref<const Eigen::VectorXf> x);

  private:
    Eigen::VectorXf alpha_, prior_;
    std::vector<std::unique_ptr<const MixGaussian>> mixgaussians_;
    int n_states_;
    Eigen::VectorXf obslik_;
    Eigen::MatrixXf transmat_t_;
    float loglik_;

    void ComputeObslik(const Eigen::Ref<const Eigen::VectorXf> x);
    float Normalize();
  };
}