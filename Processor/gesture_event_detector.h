#pragma once
#include "pcheader.h"

namespace handinput {
  class GestureEventDetector {
  public:
    GestureEventDetector() {};
    std::string Detect(std::string stage);
    void Reset();
  private:
    std::string prev_stage_;
  };
}