#pragma once
#include "pcheader.h"
#include "mixgaussian.h"

namespace handinput {
  class PROCESSOR_API HMM {
  public:
    // Factory method for creating HMM. The caller must take the ownership of the HMM object.
    // mx_model: cannot be null.
    static HMM* CreateFromMxArray(mxArray* mx_model);

    // prior: makes a copy of prioir.
    // transmat: creates a transpose of transmat.
    HMM(const Eigen::Ref<const Eigen::VectorXd> prior, 
      const Eigen::Ref<const Eigen::MatrixXd> transmat, 
      const std::vector<int> state_to_label_map,
      std::vector<std::unique_ptr<const MixGaussian>>& mixgaussians);

    int n_states() const { return n_states_; }
    int feature_len() const { return feature_len_; }
    const Eigen::VectorXd* prior() const { return &prior_; }

    // Transition matrix transposed.
    const Eigen::MatrixXd* transmat_t() const { return &transmat_t_; }

    const MixGaussian* MixGaussianAt(int index) const;

    int MostLikelyState();

    double Fwdback(const Eigen::Ref<const Eigen::VectorXf> x);
    void Reset();

  private:
    Eigen::VectorXd alpha_, prior_;
    std::vector<std::unique_ptr<const MixGaussian>> mixgaussians_;
    int n_states_, feature_len_;
    Eigen::VectorXd obslik_;
    Eigen::MatrixXd transmat_t_;
    double loglik_;
    bool reset_;
    std::vector<int> state_to_label_map_;

    HMM(const HMM&) {}
    HMM& operator=(const HMM&) { return *this; }
    void ComputeObslik(const Eigen::Ref<const Eigen::VectorXf> x);
    double Normalize();
    void CheckRI();
  };
}