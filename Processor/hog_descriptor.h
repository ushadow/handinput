#pragma once
#include "pcheader.h"

namespace handinput {
  
class HOGDescriptor {
public:
  HOGDescriptor(int w, int h, int sbin, int obin);
  ~HOGDescriptor(void) {};
  void Compute(float* I, float* HG);
  // Descriptor length.
  int Length();
private:
  static const int kTableSize = 25000;
  float a_[kTableSize];
  std::unique_ptr<float[]> M_, H_;
  std::unique_ptr<float[]> O_;
  int h_, w_, d_, hb1_, wb1_, sbin_, obin_;
  
  void hog(float *H, float *HG, int h, int w, int d, int sbin, int obin);
  void gradMag(float *I, float *M, float *O, int h, int w, int d );
  void InitAcosTable();
  void gradHist(float *M, float *O, float *H, int h, int w, int d, int sBin, int oBin, 
      bool sSoft, bool oSoft );
  double mind(float x, float y);
};

}

