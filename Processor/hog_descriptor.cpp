#include "pcheader.h"
#include "hog_descriptor.h"

#include <math.h>
#define PI 3.1415926535897931

namespace handinput {
  HOGDescriptor::HOGDescriptor(int w, int h, int sbin, int obin) : w_(w), h_(h), d_(1), 
    sbin_(sbin), obin_(obin) {      int hb = h_ / sbin_; 
      int wb = w_ / sbin_; 
      int nb = wb * hb;
      hb1_ = hb > 2 ? hb - 2 : 0;       wb1_ = wb > 2 ? wb - 2 : 0;      M_.reset(new float[h * w]);      O_.reset(new float[h * w]);
      H_.reset(new float[nb * obin]);
      InitAcosTable();
  }

  int HOGDescriptor::Length() { return hb1_ * wb1_ * obin_ * 4; }

  double HOGDescriptor::mind(float x, float y) { return (x <= y ? x : y); }

  /* build lookup table a[] s.t. a[(dx+1.1)/2.2*(n-1)]~=acos(dx) */
  void HOGDescriptor::InitAcosTable() {
    int i; float t, ni;
    ni = 2.2f/(float) kTableSize;
    for( i=0; i < kTableSize; i++ ) {
      t = (i+1)*ni - 1.1f;
      t = t<-1 ? -1 : (t>1 ? 1 : t);
      a_[i] = (float) acos( t );
    }
  }

  /* compute gradient magnitude and orientation at each location */
  void HOGDescriptor::gradMag(float *I, float *M, float *O, int h, int w, int d) {
    int x, y, c, a=w*h; double m, m1, dx, dx1, dy, dy1, rx, ry;
    float *Ix, *Ix0, *Ix1, *Iy0, *Iy1, *M0; float o, *O0;
    float *acost = a_, acMult=(25000-1)/2.2f;
    for( x=0; x<w; x++ ) {
      rx=.5; M0=M+x*h; O0=O+x*h; Ix=I+x*h; Ix0=Ix-h; Ix1=Ix+h;
      if(x==0) { Ix0=Ix; rx=1; } else if(x==w-1) { Ix1=Ix; rx=1; }
      for( y=0; y<h; y++ ) {
        if(y==0) {   Iy0=Ix-0; Iy1=Ix+1; ry=1; }
        if(y==1) {   Iy0=Ix-1; Iy1=Ix+1; ry=.5; }
        if(y==h-1) { Iy0=Ix-1; Iy1=Ix+0; ry=1; }
        dy=(*Iy1-*Iy0)*ry; dx=(*Ix1-*Ix0)*rx; m=dx*dx+dy*dy;
        for(c=1; c<d; c++) {
          dy1=(*(Iy1+c*a)-*(Iy0+c*a))*ry; dx1=(*(Ix1+c*a)-*(Ix0+c*a))*rx;
          m1=dx1*dx1+dy1*dy1; if(m1>m) { m=m1; dx=dx1; dy=dy1; }
        }
        if( m==0 ) { o=0; } else {
          m=sqrt(m); /* o=acos(dx/m); */
          o = acost[(int)((dx/m+1.1f)*acMult)];
          if( o>PI-1e-5 ) o=0; else if( dy<0 ) o=(float)PI-o;
        }
        *(M0++) = m; *(O0++) = o;
        Ix0++; Ix1++; Iy0++; Iy1++; Ix++;
      }
    }
  }

  /* compute obin gradient histograms per sBin x sBin block of pixels */
  void HOGDescriptor::gradHist(float *M, float *O, float *H, int h, int w, int d,
    int sBin, int obin, bool sSoft, bool oSoft ) {
      const int hb=h/sBin, wb=w/sBin, h0=hb*sBin, w0=wb*sBin, nb=wb*hb;
      const double s=sBin, sInv=1/s, sInv2=1/s/s, oMult=(double)obin/PI;
      float *H0; int x, y, xy, o0, o1, xb0, yb0, obin1=obin*nb;
      double od0, od1, o, m, m0, m1, xb, yb, xd0, xd1, yd0, yd1;
      if( !sSoft || sBin==1 ) { for( x=0; x<w0; x++ ) for( y=0; y<h0; y++ ) {
        /* interpolate w.r.t. orientation only, not spatial bin */
        xy=x*h+y; m=M[xy]*sInv2; o=O[xy]*oMult; o0=(int) o;
        m1=(o-o0)*m; m0=m-m1; o0*=nb; o1=o0+nb; if(o1==obin1) o1=0;
        H0=H+(x/sBin)*hb+y/sBin; H0[o0]+=m0; H0[o1]+=m1;
      } return; }
      for( x=0; x<w0; x++ ) for( y=0; y<h0; y++ ) {
        /* get interpolation coefficients */
        xy=x*h+y; m=M[xy]*sInv2; o=O[xy]*oMult; o0=(int) o;
        xb=(((double) x)+.5)*sInv-0.5; xb0=(xb<0) ? -1 : (int) xb;
        yb=(((double) y)+.5)*sInv-0.5; yb0=(yb<0) ? -1 : (int) yb;
        xd0=xb-xb0; xd1=1.0-xd0; yd0=yb-yb0; yd1=1.0-yd0; H0=H+xb0*hb+yb0;
        /* interpolate using bilinear or trilinear interpolation */
        if( !oSoft || obin==1 ) {
          o0*=nb;
          if( xb0>=0 && yb0>=0     ) *(H0+o0)      += xd1*yd1*m;
          if( xb0+1<wb && yb0>=0   ) *(H0+hb+o0)   += xd0*yd1*m;
          if( xb0>=0 && yb0+1<hb   ) *(H0+1+o0)    += xd1*yd0*m;
          if( xb0+1<wb && yb0+1<hb ) *(H0+hb+1+o0) += xd0*yd0*m;
        } else {
          od0=o-o0; od1=1.0-od0; o0*=nb; o1=o0+nb; if(o1==obin1) o1=0;
          if( xb0>=0 && yb0>=0     ) *(H0+o0)      += od1*xd1*yd1*m;
          if( xb0+1<wb && yb0>=0   ) *(H0+hb+o0)   += od1*xd0*yd1*m;
          if( xb0>=0 && yb0+1<hb   ) *(H0+1+o0)    += od1*xd1*yd0*m;
          if( xb0+1<wb && yb0+1<hb ) *(H0+hb+1+o0) += od1*xd0*yd0*m;
          if( xb0>=0 && yb0>=0     ) *(H0+o1)      += od0*xd1*yd1*m;
          if( xb0+1<wb && yb0>=0   ) *(H0+hb+o1)   += od0*xd0*yd1*m;
          if( xb0>=0 && yb0+1<hb   ) *(H0+1+o1)    += od0*xd1*yd0*m;
          if( xb0+1<wb && yb0+1<hb ) *(H0+hb+1+o1) += od0*xd0*yd0*m;
        }
      }
  }

  /* compute HOG features given gradient histograms */
  void HOGDescriptor::hog(float *H, float *HG, int h, int w, int d, int sBin, int obin ) {
    float *N, *N1, *H1, *HG1, n; int o, x, y, x1, y1, hb, wb, nb, hb1, wb1, nb1;
    double eps = 1e-4/4.0/sBin/sBin/sBin/sBin; /* precise backward equality */
    hb=h/sBin; wb=w/sBin; nb=wb*hb; hb1=hb-2; wb1=wb-2; nb1=hb1*wb1;
    if(hb1<=0 || wb1<=0) return; 
    N = new float[nb];
    for(o=0; o<obin; o++) for(x=0; x<nb; x++) N[x]+=H[x+o*nb]*H[x+o*nb];
    for( x=0; x<wb1; x++ ) for( y=0; y<hb1; y++ ) {
      HG1 = HG + x*hb1 + y; /* perform 4 normalizations per spatial block */
      for(x1=1; x1>=0; x1--) for(y1=1; y1>=0; y1--) {
        N1 = N + (x+x1)*hb + (y+y1);  H1 = H + (x+1)*hb + (y+1);
        n = 1.0/sqrt(*N1 + *(N1+1) + *(N1+hb) + *(N1+hb+1) + eps);
        for(o=0; o<obin; o++) { *HG1=mind(*H1*n, 0.2); HG1+=nb1; H1+=nb; }
      }
    } 
    delete N;
  }

  /* H = hog( I, [sBin], [obin] ) - see hog.m */
  // I: input image
  // sbin: spatial bin size
  void HOGDescriptor::Compute(float* I, float* HG) {
    if (hb1_ == 0 || wb1_ == 0)
      return;
    gradMag(I, M_.get(), O_.get(), h_, w_, d_);
    gradHist(M_.get(), O_.get(), H_.get(), h_, w_, d_, sbin_, obin_, true, true );
    hog(H_.get(), HG, h_, w_, d_, sbin_, obin_);
  }
}
