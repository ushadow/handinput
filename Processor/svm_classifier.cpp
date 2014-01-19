#include "pcheader.h"
#include "svm_classifier.h"

namespace handinput {
  static int (*info)(const char *fmt,...) = &printf;

  double* SVMClassifier::predict(std::vector<double> attr) {

    double predict_label;
    if (x_ == NULL)
      x_ = (struct svm_node *) realloc(x_, attr.size() + 1 * sizeof(struct svm_node));

    for (int i = 0; i < attr.size(); i++) {
      x_[i].index = i + 1;
      x_[i].value = attr[i];
    }
    x_[attr.size()].index = -1;

    if (predict_prob_ && (svm_type_ == C_SVC || svm_type_ == NU_SVC)) {
      predict_label = svm_predict_probability(model_, x_, prob_estimates_);
    } else {
      predict_label = svm_predict(model_, x_);
    }
    return prob_estimates_;
  }

  SVMClassifier::SVMClassifier(std::string model_file, bool pred_prob) : predict_prob_(pred_prob) {
    model_ = svm_load_model(model_file.c_str()); 
    if (model_ == NULL) {
      fprintf(stderr,"can't open model file %s\n", model_file);
      exit(1);
    }

    if(predict_prob_) {
      if(svm_check_probability_model(model_) == 0) {
        fprintf(stderr,"Model does not support probabiliy estimates\n");
        exit(1);
      }
    } else {
      if(svm_check_probability_model(model_)!=0)
        info("Model supports probability estimates, but disabled in prediction.\n");
    }

    svm_type_ = svm_get_svm_type(model_);
    int nr_class_ = svm_get_nr_class(model_);

    if(predict_prob_) {
      if (svm_type_ == NU_SVR || svm_type_ == EPSILON_SVR)
        info("Prob. model for test data: target value = predicted value + z,\nz: Laplace distribution e^(-|z|/sigma)/(2sigma),sigma=%g\n",svm_get_svr_probability(model_));
      else {
        prob_estimates_ = (double *) malloc(nr_class_ * sizeof(double));
      }
    }
  }

  SVMClassifier::~SVMClassifier() {
    svm_free_and_destroy_model(&model_);
    free(x_);
    if (predict_prob_)
      free(prob_estimates_);
  }
}