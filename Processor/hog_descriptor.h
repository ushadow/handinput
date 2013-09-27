#pragma once
#include "pcheader.h"

namespace handinput {
  
// One-fold normalization HOG descriptor.
class HOGDescriptor {
public:
  // w: width of the input image.
  // h: heigth of the input image.
  HOGDescriptor(int w, int h, int sbin, int obin, int fold = 1);
  ~HOGDescriptor(void) {};
  void Compute(float* I, float* HG);
  // Descriptor length.
  int Length();

  int NxCells() { return wb1_; }
  int NyCells() { return hb1_; }

private:
  static const int kTableSize = 25000;
  float a_[kTableSize];
  std::unique_ptr<float[]> M_, H_;
  std::unique_ptr<float[]> O_, N_;
  int h_, w_, d_, hb1_, wb1_, sbin_, obin_, fold_per_dim_, fold_;
  float acmult_;
  
  void Hog(float *H, float *HG, int h, int w, int d, int sbin, int obin);
  void GradMag(float *I, float *M, float *O, int h, int w, int d );
  void InitAcosTable();
  void GradHist(float *M, float *O, float *H, int h, int w, int d, int sBin, int oBin, 
      bool sSoft, bool oSoft );
  float Mind(float x, float y);
};

}

