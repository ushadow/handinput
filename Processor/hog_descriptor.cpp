#include "pcheader.h"
#include "hog_descriptor.h"

#include <math.h>

namespace handinput {

#define PI 3.1415926535897931f

  HOGDescriptor::HOGDescriptor(int w, int h, int sbin, int obin, int fold) : w_(w), h_(h), d_(1), 
    sbin_(sbin), obin_(obin), fold_(fold) {
      int hb = h_ / sbin_; 
      int wb = w_ / sbin_; 
      nb_ = wb * hb;

      fold_per_dim_ = (int) sqrt(fold);
      hb1_ = hb > fold_per_dim_ ? hb - fold_per_dim_ : 0;
      wb1_ = wb > fold_per_dim_ ? wb - fold_per_dim_ : 0;
      hist_length_ = nb_ * obin;

      M_.reset(new float[h * w]);
      O_.reset(new float[h * w]);
      H_.reset(new float[hist_length_]);
      N_.reset(new float[nb_]);
      InitAcosTable();
  }

  int HOGDescriptor::Length() { return hb1_ * wb1_ * obin_ * fold_; }

  float HOGDescriptor::Mind(float x, float y) { return (x <= y ? x : y); }

  /* build lookup table a[] s.t. a[(dx+1.1)/2.2*(n-1)]~=acos(dx) */
  void HOGDescriptor::InitAcosTable() {
    int i; float t, ni;
    ni = 2.2f/(float) kTableSize;
    for( i=0; i < kTableSize; i++ ) {
      t = (i+1)*ni - 1.1f;
      t = t<-1 ? -1 : (t>1 ? 1 : t);
      a_[i] = (float) acos( t );
    }
    acmult_ = (kTableSize - 1) / 2.2f;
  }

  /* compute gradient magnitude and orientation at each location */
  void HOGDescriptor::GradMag(float *I, float *M, float *O, int h, int w, int d) {
    int x, y, c, a = w * h; float m, m1, dx, dx1, dy, dy1, rx, ry;
    float *Iy, *Ix0, *Ix1, *Iy0, *Iy1, *M0; float o, *O0;
    float *acost = a_; 
    float eps = 1e-5f;
    for (y = 0; y < h; y++) {
      ry=.5; M0 = M + y * w; O0 = O + y * w; Iy = I + y * w; Iy0 = Iy - w; Iy1 = Iy + w;
      if (y == 0) { Iy0 = Iy; ry = 1; } else if( y == h - 1) { Iy1 = Iy; ry=1; }
      for (x = 0; x < w; x++ ) {
        if (x == 0) { Ix0 = Iy - 0; Ix1 = Iy + 1; rx = 1; }
        if (x == 1) { Ix0 = Iy - 1; Ix1 = Iy + 1; rx = .5; }
        if (x == w - 1) { Ix0 = Iy - 1; Ix1 = Iy + 0; rx = 1; }
        dy = (*Iy1 - *Iy0) * ry; dx = (*Ix1 - *Ix0) * rx; m = dx * dx + dy * dy;
        for(c=1; c<d; c++) {
          dy1=(*(Iy1+c*a)-*(Iy0+c*a))*ry; dx1=(*(Ix1+c*a)-*(Ix0+c*a))*rx;
          m1=dx1*dx1+dy1*dy1; if(m1>m) { m=m1; dx=dx1; dy=dy1; }
        }
        if (m < eps) { o = 0; } else {
          m = sqrt(m); /* o=acos(dx/m); */
          o = acost[(int)((dx / m + 1.1f) * acmult_ )];
          if (o > PI - eps) o = 0; else if (dy < 0) o = (float) PI - o;
        }
        *(M0++) = m; *(O0++) = o;
        Ix0++; Ix1++; Iy0++; Iy1++; Iy++;
      }
    }
  }

  /* compute obin gradient histograms per sBin x sBin block of pixels */
  // H: array for histogram. Data are stored orientation-wise.
  void HOGDescriptor::GradHist(float *M, float *O, float *H, int h, int w, int d,
    int sBin, int obin, bool sSoft, bool oSoft ) {
      const int hb = h / sBin, wb = w / sBin, h0 = hb * sBin, w0 = wb * sBin, nb = nb_;
      const float s = (float) sBin, sInv = 1 / s, sInv2 = 1 / s / s;
      const float oMult = obin / PI;
      float *H0; int x, y, xy, o0, o1, xb0, yb0, obin1 = obin * nb;
      float od0, od1, o, m, m0, m1, xb, yb, xd0, xd1, yd0, yd1;
      std::fill_n(H, hist_length_, 0.0f);
      if (!sSoft || sBin == 1) { for (y = 0; y < h0; y++) for (x = 0; x < w0; x++) {
        /* interpolate w.r.t. orientation only, not spatial bin */
        xy = y * w + x; m = M[xy] * sInv2; o = O[xy] * oMult; o0 = (int) o;
        m1 = (o - o0) * m; m0 = m - m1; o0 *= nb; o1 = o0 + nb; if (o1 == obin1) o1 = 0;
        H0 = H + (y / sBin) * wb + x / sBin; H0[o0] += m0; H0[o1] += m1;
      } return; }
      for (y = 0; y < h0; y++) for (x = 0; x < w0; x++) {
        /* get interpolation coefficients */
        xy = y * w + x; m = M[xy] * sInv2; o = O[xy] * oMult; o0 = (int) o;
        xb = (((float) x) + .5f) * sInv - 0.5f; xb0 = (xb < 0) ? -1 : (int) xb;
        yb = (((float) y) + .5f) * sInv - 0.5f; yb0 = (yb < 0) ? -1 : (int) yb;
        xd0 = xb - xb0; xd1 = 1.0f - xd0; yd0 = yb - yb0; yd1 = 1.0f - yd0; H0 = H + yb0 * wb + xb0;
        /* interpolate using bilinear or trilinear interpolation */
        if( !oSoft || obin == 1 ) {
          o0 *= nb;
          if (xb0 >= 0     && yb0 >= 0    ) *(H0 + o0)          += xd1 * yd1 * m;
          if (xb0 + 1 < wb && yb0 >= 0    ) *(H0 + 1 + o0)      += xd0 * yd1 * m;
          if (xb0 >= 0     && yb0 + 1 < hb) *(H0 + wb + o0)     += xd1 * yd0 * m;
          if (xb0 + 1 < wb && yb0 + 1 < hb) *(H0 + wb + 1 + o0) += xd0 * yd0 * m;
        } else {
          od0 = o - o0; od1 = 1.0f - od0; o0 *= nb; o1 = o0 + nb; if (o1 == obin1) o1 = 0;
          if (xb0 >= 0     && yb0 >= 0    ) *(H0 + o0)          += od1 * xd1 * yd1 * m;
          if (xb0 + 1 < wb && yb0 >= 0    ) *(H0 + 1 + o0)      += od1 * xd0 * yd1 * m;
          if (xb0 >= 0     && yb0 + 1 < hb) *(H0 + wb + o0)     += od1 * xd1 * yd0 * m;
          if (xb0 + 1 < wb && yb0 + 1 < hb) *(H0 + wb + 1 + o0) += od1 * xd0 * yd0 * m;

          if (xb0 >= 0 && yb0 >= 0) *(H0 + o1)      += od0 * xd1 * yd1 * m;
          if (xb0 + 1 < wb && yb0 >= 0) *(H0 + 1 + o1)   += od0 * xd0 * yd1 * m;
          if (xb0 >= 0 && yb0 + 1 < hb) *(H0 + wb + o1)    += od0 * xd1 * yd0 * m;
          if (xb0 + 1 <wb && yb0 + 1 < hb) *(H0 + wb + 1 + o1) += od0 * xd0 * yd0 * m;
        }
      }
  }

  // Compute HOG features given gradient histograms. The maximum value for each bin is 0.2.
  void HOGDescriptor::Hog(float *H, float *HG, int h, int w, int d, int sBin, int obin ) {
    float *HG1; int o, x, y, x1, y1, hb, wb, nb, nb1;
    float eps = 1e-4f/4.0f/sBin/sBin/sBin/sBin; /* precise backward equality */
    float n, *N, *N1, *H1;
    hb = h / sBin; wb = w / sBin; nb = wb * hb; nb1 = hb1_ * wb1_;
    if(hb1_ <= 0 || wb1_ <= 0) return; 
    N = N_.get();
    std::fill_n(N, nb_, 0.0f);
    // Computes total count in each block.
    for (o = 0; o < obin; o++) 
      for(x = 0; x < nb; x++) {
        N[x] += H[x + o * nb] * H[x + o * nb];
      }
    for (y = 0; y < hb1_; y++ ) for (x = 0; x < wb1_; x++ ) {
      HG1 = HG + y * wb1_ + x; 
      for (y1 = fold_per_dim_ - 1; y1 >= 0; y1--) 
        for (x1 = fold_per_dim_ - 1; x1 >= 0; x1--) {
          N1 = N + (y + y1) * wb + (x + x1);  H1 = H + (y + 1) * wb + (x + 1);
          n = 1 / sqrt(*N1 + *(N1 + 1) + *(N1 + wb) + *(N1 + wb + 1) + eps);
          for (o = 0; o < obin; o++) { 
            *HG1 = Mind((float) (*H1 * n), 0.2f); 
            HG1 += nb1; H1 += nb; }
        }
    } 
  }

  // I: input image
  void HOGDescriptor::Compute(float* I, float* HG) {
    if (hb1_ == 0 || wb1_ == 0)
      return;
    GradMag(I, M_.get(), O_.get(), h_, w_, d_);
    GradHist(M_.get(), O_.get(), H_.get(), h_, w_, d_, sbin_, obin_, true, true);
    Hog(H_.get(), HG, h_, w_, d_, sbin_, obin_);
  }
}
