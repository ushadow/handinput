#pragma once
#include "harrisbuffer.h"

public ref class MInterestPoint {
public:
  int X, Y;
  double Sx2;
  MInterestPoint(int x, int y, double sx2) : X(x), Y(y), Sx2(sx2) {}
};

public ref class MHarrisBuffer {
public:
  MHarrisBuffer() { harrisbuffer_ = new HarrisBuffer(); }
  ~MHarrisBuffer() { this->!MHarrisBuffer(); }
  !MHarrisBuffer() { delete harrisbuffer_; } // Finalizer
  bool Init(System::IntPtr image);
  void ProcessFrame(System::IntPtr image);
  void DrawInteresPoints(System::IntPtr image);
  System::Collections::ArrayList^ GetInterestPoints();
private:
  HarrisBuffer* harrisbuffer_;
};

