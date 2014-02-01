#pragma once
#include "pcheader.h"
#include "mixgaussian.h"

namespace handinput {
  class PROCESSOR_API HMM {
  public:
    // Factory method for creating HMM. The caller must take the ownership of the HMM object.
    // mx_model: cannot be null.
    static HMM* CreateFromMxArray(mxArray* mx_model, int lag);

    // prior: makes a copy of prioir.
    // transmat: creates a transpose of transmat.
    HMM(const Eigen::Ref<const Eigen::VectorXd> prior, 
      Eigen::MatrixXd transmat, 
      std::vector<std::unique_ptr<const MixGaussian>>& mixgaussians,
      const std::vector<int> state_to_label_map,
      std::vector<std::string> state_to_stage_map, int smooth_win = 1); 

    int n_states() const { return n_states_; }
    int feature_len() const { return feature_len_; }
    const Eigen::VectorXd* prior() const { return &prior_; }
    const std::vector<int>& state_to_label_map() const { return state_to_label_map_; }
    const std::vector<std::string>& stage_map() const { return stage_map_; }

    // Transition matrix transposed.
    const Eigen::MatrixXd* transmat_t() const { return &transmat_t_; }

    const MixGaussian* MixGaussianAt(int index) const;

    // 1-based label index.
    int MostLikelyLabelIndex();
    std::string MostLikelyStage();

    void Fwdback(const Eigen::Ref<const Eigen::VectorXf> x);
    void Reset();

  private:
    static const double kMinGamma;
    Eigen::VectorXd prior_, gamma_;
    std::deque<Eigen::VectorXd> alpha_, obslik_;
    std::vector<std::unique_ptr<const MixGaussian>> mixgaussians_;
    int n_states_, feature_len_, most_likely_hidden_state_;
    Eigen::MatrixXd transmat_t_, transmat_;
    double loglik_;
    bool reset_;
    std::vector<int> state_to_label_map_;
    std::vector<std::string> stage_map_;
    int smooth_win_, rest_state_;

    HMM(const HMM&) {}
    HMM& operator=(const HMM&) { return *this; }
    void ComputeObslik(const Eigen::Ref<const Eigen::VectorXf> x);
    double Normalize(Eigen::VectorXd* x);
    void CheckRI();
    void Back();
    void ComputeMostLikelyState();
  };
}