#ifndef HARRISBUFFER_H
#define HARRISBUFFER_H

#include "pcheader.h"
#include "stbuffer.h"

class PROCESSOR_API HarrisBuffer {
private:

  CvMat* normvec;
  CvMat JetFilter;

  std::vector<double> SpatialMaskSeparable;
  std::vector<double> SpatialMask;
  std::vector<double> TemporalMask1;
  std::vector<double> TemporalMask2;
  std::vector<double> DerivMask;

  STBuffer original;	

  STBuffer databuffer;
  STBuffer convbuffer;

  STBuffer cxxbuffer;
  STBuffer cxybuffer;
  STBuffer cxtbuffer;
  STBuffer cyybuffer;
  STBuffer cytbuffer;
  STBuffer cttbuffer;	

  STBuffer Hbuffer;

  STBuffer timgbuffer;    // pixel labels for histogram descriptors
  STBuffer wimgbuffer;   

  STBuffer OFtimgbuffer;    // pixel labels for OF histogram descriptors
  STBuffer OFwimgbuffer;   

  std::ofstream FeatureFile;

  int iFrame;


  //temp images
  IplImage* tmp;
  IplImage* tmp1;
  IplImage* tmp2;
  IplImage* tmp3;
  IplImage* tmp4;
  IplImage* tmp5;
  IplImage* tmp6;
  IplImage* header;
  IplImage* gray;	//input grayscale image 
  IplImage* prevgray; //previous gray image 
  IplImage* frame; //input image type=IMGTYPE
  IplImage* vis;	//hbuffer image


  IplImage* cxx;
  IplImage* cxy;
  IplImage* cxt;
  IplImage* cyy;
  IplImage* cyt;
  IplImage* ctt;

  IplImage* L;
  IplImage* Lt;
  IplImage* Lx;
  IplImage* Ly;
  IplImage* OFx;
  IplImage* OFy;

  IplImage* opticalFlowLastFrame;
  IplImage* opticalFlowNextFrame;
  IplImage* opticalFlowLastFrame8u;
  IplImage* opticalFlowNextFrame8u;



  IplImage* timg;
  IplImage* wimg;
  IplImage* OFtimg;
  IplImage* OFwimg;

  void GaussianSmoothingMul(IplImage* im1, IplImage* im2, IplImage* dst, double sigma);
  void HarrisFunction(/*IplImage* cxx,IplImage* cxy,IplImage* cxt,IplImage* cyy,IplImage* cyt,IplImage* ctt,*/ double k, IplImage* dst);
  void WriteFeatures(InterestPoint &ip);

public:

  InterestPointList ipList;
  TrackingPointList pList;
  TrackingPointStdList pointsToTrack;
  TrackingPointStdList pointsFinishedTracking;

  double kparam;	//parameter of point detection with the default value 0.0005
  double sig2;	//variance of spatial smoothing filter 
  double tau2;	
  int delay;
  double SignificantPointThresh;
  int Border;
  int framemax;

  double patchsizefactor;
  std::string descriptortype;

  HarrisBuffer(void);
  ~HarrisBuffer(void);

  IplImage* getHBufferImage(int type);

  bool Init(IplImage* firstfrm, std::string fname);
  void ProcessFrame(IplImage* frame, IplImage* OFx_precomp, IplImage* OFy_precomp);
  void DetectInterestPoints(int border=0);
  void DrawInterestPoints(IplImage* im);
  void OpticalFlowFromSMM();
  void OpticalFlowFromLK();
  int NumberOfDetectedIPs();
  int NumberOfDetectedTPs();	

  void CalculateVelocityHistories();
  void finishProcessing();
  void AccumulateHistogram(CvMat *tdata, CvMat *wdata, double *hist, int nbins, bool normflag=true);

  int CurrentlyDoneFrameID(){return iFrame-convbuffer.BufferSize;}

};




#endif //HARRISBUFFER_H
