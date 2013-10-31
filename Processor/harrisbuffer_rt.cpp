#include "pcheader.h"

#include "harrisbuffer_rt.h"
#include "cvutil.h"

HarrisBufferRt::HarrisBufferRt(double tau2):kparam(5e-4),sig2(8.0),tau2_(tau2),delay(0),SignificantPointThresh(1E-9),Border(5),patchsizefactor(9.0) {
  iFrame=0;

  normvec=NULL;

  tmp=NULL;
  tmp1=NULL;
  tmp2=NULL;
  tmp3=NULL;
  tmp4=NULL;
  tmp5=NULL;
  tmp6=NULL;

  frame=NULL;
  gray=NULL;
  prevgray=NULL;

  cxx=NULL;
  cxy=NULL;
  cxt=NULL;
  cyy=NULL;
  cyt=NULL;
  ctt=NULL;
  L = Lt = Lx = Ly = NULL;
  vis=NULL;
  timg=NULL;
  wimg=NULL;
  OFtimg=NULL;
  OFwimg=NULL;
  opticalFlowLastFrame=NULL;
  opticalFlowNextFrame=NULL;

  //descriptortype="hoghof";
  descriptortype="of";
}

HarrisBufferRt::~HarrisBufferRt(void)
{ 
  if(normvec) cvReleaseMat(&normvec);

  if(tmp)		cvReleaseImage(&tmp);
  if(tmp1)	cvReleaseImage(&tmp1);
  if(tmp2)	cvReleaseImage(&tmp2);
  if(tmp3)	cvReleaseImage(&tmp3);
  if(tmp4)	cvReleaseImage(&tmp4);
  if(tmp5)	cvReleaseImage(&tmp5);
  if(tmp6)	cvReleaseImage(&tmp6);

  if(frame)	cvReleaseImage(&frame);
  if(gray)	cvReleaseImage(&gray);
  if(prevgray)	cvReleaseImage(&prevgray);

  if(cxx)	cvReleaseImage(&cxx);
  if(cxy)	cvReleaseImage(&cxy);
  if(cxt)	cvReleaseImage(&cxt);
  if(cyy)	cvReleaseImage(&cyy);
  if(cyt)	cvReleaseImage(&cyt);
  if(ctt)	cvReleaseImage(&ctt);

  if(L)	cvReleaseImage(&L);
  if(Lt)	cvReleaseImage(&Lt);
  if(Lx)	cvReleaseImage(&Lx);
  if(Ly)	cvReleaseImage(&Ly);
  //if(OFx)	cvReleaseImage(&OFx);
  //if(OFy)	cvReleaseImage(&OFy);
  if(vis)	cvReleaseImage(&vis);

  if(timg) cvReleaseImage(&timg);
  if(wimg) cvReleaseImage(&wimg);
  if(OFtimg) cvReleaseImage(&OFtimg);
  if(OFwimg) cvReleaseImage(&OFwimg);
  if(opticalFlowLastFrame) cvReleaseImage(&opticalFlowLastFrame);
  if(opticalFlowNextFrame) cvReleaseImage(&opticalFlowNextFrame);
  if(opticalFlowLastFrame8u) cvReleaseImage(&opticalFlowLastFrame8u);
  if(opticalFlowNextFrame8u) cvReleaseImage(&opticalFlowNextFrame8u);
}

bool HarrisBufferRt::Init(IplImage* firstfrm,std::string fname)
{
  SpatialMaskSeparable=CVUtil::GaussianMask1D(sig2);

  TemporalMask1.push_back(1.0 / 3);
  TemporalMask1.push_back(1.0 / 3);
  TemporalMask1.push_back(1.0 / 3);

  TemporalMask2.push_back(1.0 / 3);
  TemporalMask2.push_back(1.0 / 3);
  TemporalMask2.push_back(1.0 / 3);

  DerivMask.push_back(-0.5);
  DerivMask.push_back(0);
  DerivMask.push_back(0.5);

  int sz1 = (int)TemporalMask1.size();
  int sz2 = (int)TemporalMask2.size();

  if (SpatialMaskSeparable.size() < 3) {
    std::cerr<<"Spacial smooting variance is too low"<<std::endl;
    return false;
  }

  delay = 1;

  databuffer.Init(sz1);
  convbuffer.Init(sz2);
  cxxbuffer.Init(sz2);
  cxybuffer.Init(sz2);
  cxtbuffer.Init(sz2);
  cyybuffer.Init(sz2);
  cytbuffer.Init(sz2);
  cttbuffer.Init(sz2);	
  //original.Init(sz2);//prb:? ??? sz1? sz2? delay?
  original.Init(delay);//prb:? ??? sz1? sz2? delay?
  Hbuffer.Init(3);

  //todo:difference between cvMat and IplImage
  //		CvMat *work=cvCreateMat(frame->width,frame->height,CV_64FC1);
  tmp= cvCreateImage(cvGetSize(firstfrm), IMGTYPE, 1);
  tmp1= cvCreateImage(cvGetSize(firstfrm), IMGTYPE, 1);
  tmp2= cvCreateImage(cvGetSize(firstfrm), IMGTYPE, 1);
  tmp3= cvCreateImage(cvGetSize(firstfrm), IMGTYPE, 1);
  tmp4= cvCreateImage(cvGetSize(firstfrm), IMGTYPE, 1);
  tmp5= cvCreateImage(cvGetSize(firstfrm), IMGTYPE, 1);
  tmp6= cvCreateImage(cvGetSize(firstfrm), IMGTYPE, 1);

  frame = cvCreateImage(cvGetSize(firstfrm),IMGTYPE ,1);
  gray=cvCreateImage(cvGetSize(firstfrm),IPL_DEPTH_8U,1);
  prevgray=cvCreateImage(cvGetSize(firstfrm),IPL_DEPTH_8U,1);

  cxx= cvCreateImage(cvGetSize(firstfrm),IMGTYPE ,1);
  cxy= cvCreateImage(cvGetSize(firstfrm),IMGTYPE ,1);
  cxt= cvCreateImage(cvGetSize(firstfrm),IMGTYPE ,1);
  cyy= cvCreateImage(cvGetSize(firstfrm),IMGTYPE ,1);
  cyt= cvCreateImage(cvGetSize(firstfrm),IMGTYPE ,1);
  ctt= cvCreateImage(cvGetSize(firstfrm),IMGTYPE ,1);

  L  = cvCreateImage(cvGetSize(firstfrm),IMGTYPE ,1);
  Lt = cvCreateImage(cvGetSize(firstfrm),IMGTYPE ,1);
  Lx = cvCreateImage(cvGetSize(firstfrm),IMGTYPE ,1);
  Ly = cvCreateImage(cvGetSize(firstfrm),IMGTYPE ,1);

  //Initilizing normalization vector for JET features
  normvec= cvCreateMat(kLengthFeatures, 1, CV_64F );
  double sx1=sqrt(sig2);
  double st1=sqrt(tau2_);
  double sx2=sx1*sx1, sx3=sx1*sx2, sx4=sx1*sx3;
  double st2=st1*st1, st3=st1*st2, st4=st1*st3;
  double* data=(double*) normvec->data.ptr;

  data[0]= sx1;
  data[1]= sx1;
  data[2]= st1;
  data[3]= sx2;
  data[4]= sx2;
  data[5]= sx2;
  data[6]= sx1*st1;
  data[7]= sx1*st1;
  data[8]= st2;
  data[9]= sx3;
  data[10]= sx3;
  data[11]= sx3;
  data[12]= sx3;
  data[13]= sx2*st1;
  data[14]= sx2*st1;
  data[15]= sx2*st1;
  data[16]= sx1*st2;
  data[17]= sx1*st2;
  data[18]= st3;
  data[19]= sx4;
  data[20]= sx4;
  data[21]= sx4;
  data[22]= sx4;
  data[23]= sx4;
  data[24]= sx3*st1;
  data[25]= sx3*st1;
  data[26]= sx3*st1;
  data[27]= sx3*st1;
  data[28]= sx2*st2;
  data[29]= sx2*st2;
  data[30]= sx2*st2;
  data[31]= sx1*st3;
  data[32]= sx1*st3;
  data[33]= st4;

  return true;
}

void HarrisBufferRt::ProcessFrame(IplImage* frm, IplImage* OFx_precomp, IplImage* OFy_precomp) {
  if (!prevgray) cvCopy(frm, prevgray);
  else cvCopy(gray, prevgray);
  cvCopy(frm, gray);
  cvScale(gray, frame, 1.0 / 255.0, 0.0);

  //Ross moved this 'till later
  original.Update(frame);

  //spatial filtering
  CVUtil::GaussianSmooth(frame, tmp, sig2, FFT);
  databuffer.Update(tmp);

  //temporal filtering
  int tstamp1 = databuffer.ExponentialSmooth(tmp1, TemporalMask1);
  convbuffer.Update(tmp1, tstamp1);

  int tstamp1d = convbuffer.TemporalConvolve(Lt, DerivMask);

  convbuffer.GetFrame(tstamp1d, L);
  CVUtil::ImageGradient(L, Lx, Ly); //prb: a possible scale
  cvScale(Lx, Lx, sqrt(sig2) * 0.5, 0);
  cvScale(Ly, Ly, sqrt(sig2) * 0.5, 0);

  //update second-moment matrix
  GaussianSmoothingMul(Lx, Lx, tmp1, 2 * sig2);
  cxxbuffer.Update(tmp1,tstamp1d);
  GaussianSmoothingMul(Lx, Ly, tmp1, 2 * sig2);
  cxybuffer.Update(tmp1,tstamp1d);
  GaussianSmoothingMul(Lx, Lt, tmp1, 2 * sig2);
  cxtbuffer.Update(tmp1,tstamp1d);
  GaussianSmoothingMul(Ly, Ly, tmp1, 2 * sig2);
  cyybuffer.Update(tmp1,tstamp1d);
  GaussianSmoothingMul(Ly, Lt, tmp1, 2 * sig2);
  cytbuffer.Update(tmp1,tstamp1d);
  GaussianSmoothingMul(Lt, Lt, tmp1, 2 * sig2);
  cttbuffer.Update(tmp1, tstamp1d);

  //update Harris buffer
  int tstamp2=0;
  tstamp2=cxxbuffer.ExponentialSmooth(cxx, TemporalMask2);
  tstamp2=cxybuffer.ExponentialSmooth(cxy, TemporalMask2);
  tstamp2=cxtbuffer.ExponentialSmooth(cxt, TemporalMask2);
  tstamp2=cyybuffer.ExponentialSmooth(cyy, TemporalMask2);
  tstamp2=cytbuffer.ExponentialSmooth(cyt, TemporalMask2);
  tstamp2=cttbuffer.ExponentialSmooth(ctt, TemporalMask2);

  // compute 3D extension of Harris function
  HarrisFunction(kparam, tmp);
  Hbuffer.Update(tmp, tstamp2);

  //*** detect interest points
  DetectInterestPoints(Border);

  iFrame++;
  return;
}

void HarrisBufferRt::GaussianSmoothingMul(IplImage* im1, IplImage* im2, IplImage* dst, double var)
{
  cvMul(im1, im2, tmp4);
  CVUtil::GaussianSmooth(tmp4, dst, var, FFT);
}

void HarrisBufferRt::HarrisFunction(double k, IplImage* dst) {
  // Harris function in 3D
  // original space-time Harris
  /*detC=  
  cxx.*cyy.*ctt +		xx yy tt
  cxy.*cyt.*cxt +		2 * xy yt xt
  cxt.*cxy.*cyt -		.
  cxx.*cyt.*cyt -		xx yt^2
  cxy.*cxy.*ctt -		tt xy^2	
  cxt.*cyy.*cxt ;		yy xt^2
  */
  cvMul(cxx, cyy, tmp1);
  cvMul(ctt, tmp1, tmp1);

  cvMul(cxy, cxt, tmp2);
  cvMul(cyt, tmp2, tmp2,2);

  cvAdd(tmp1,tmp2,tmp1);

  cvMul(cyt,cyt,tmp2);
  cvMul(cxx,tmp2,tmp2);

  cvSub(tmp1,tmp2,tmp1);

  cvMul(cxy,cxy,tmp2);
  cvMul(ctt,tmp2,tmp2);

  cvSub(tmp1,tmp2,tmp1);

  cvMul(cxt,cxt,tmp2);
  cvMul(cyy,tmp2,tmp2);

  cvSub(tmp1,tmp2,tmp1);

  //trace3C=(cxx+cyy+ctt).^3;
  cvAdd(cxx,cyy,tmp2);
  cvAdd(ctt,tmp2,tmp2);
  cvPow(tmp2,tmp2,3);

  //H=detC-stharrisbuffer.kparam*trace3C;
  cvScale(tmp2,tmp2,k,0);
  cvSub(tmp1, tmp2, dst);
}

IplImage* HarrisBufferRt::getHBufferImage(int type) {
  int r;
  //cvAbs(tmp3,tmp3);cvLog(tmp3,tmp3);
  if(type==0)
    r=original.GetFrame(iFrame-convbuffer.BufferSize,vis);
  else
    r=Hbuffer.GetFrame(iFrame-convbuffer.BufferSize,vis);

  if(r==-1)
    cvZero(vis);

  return vis;
}

void HarrisBufferRt::WriteFeatures(InterestPoint &ip)
{
  assert(ip.features);
  double *data=(double*)ip.features->data.ptr;
  FeatureFile<<ip.x<<"\t"<<ip.y<<"\t"<<ip.t<<"\t"<<ip.val<<"\t";
  for(int i=0;i<34;i++)
    FeatureFile<<data[i]<<"\t";
  FeatureFile<<std::endl;
}

void HarrisBufferRt::DetectInterestPoints(int border) {
  ipList.clear();
  Hbuffer.FindLocalMaxima(ipList, true);
  CvMat *reg = cvCreateMat(kSizeNeighb, 1, CV_64F);	

  int sz2=(int)TemporalMask2.size();

  //remove border
  if(border<2)
    border = 2; // interest points in the boundary should be remove to have a valid local 5x5x5 mask
  // an alternative could be extending by 2 pixel in space dimensions	

  //select significant points which are not in the boundary
  for(int i = 0; i < (int)ipList.size(); i++){
    // set s-t scales
    ipList[i].sx2= sig2; ipList[i].st2 = tau2_;

    // set feature type : 5 for multi-scale Harris with this implementation
    ipList[i].ptype = 5;

    if(ipList[i].x >= border && (ipList[i].x<frame->width-border) &&
      ipList[i].y>=border && (ipList[i].y<frame->height-border) && 
      ipList[i].val > SignificantPointThresh && 
      ipList[i].t>(double)sz2/2.0)
      ipList[i].reject=false;
  }

  //computing JET features around an interest point by 5x5x5 local mask
  cvReleaseMat(&reg);
}		

int HarrisBufferRt::NumberOfDetectedIPs()
{
  //return ipList.size();
  int n=0;
  for(int i=0;i<(int)ipList.size();i++)
    if(!ipList[i].reject)
      n++;
  return n;
}

int HarrisBufferRt::NumberOfDetectedTPs()
{
  //printf("Started hb::numDetTPs\n");
  //printf("hb::size=%d\n",(int)pList.size());
  //return ipList.size();
  int n=0;
  for(int i=0;i<(int)pList.size();i++)
    if(!pList[i].reject)
      n++;
  //printf("finished hb::numDetTPs\n");
  return n;
}

void HarrisBufferRt::DrawInterestPoints(IplImage* im)
{	
  //if(ipList.size()>0)
  //	if(ipList[0].t!=iFrame-convbuffer.BufferSize)
  //		return;
  for(int i=0;i<(int)ipList.size();i++)
    if(!ipList[i].reject)
      //CVUtil::DrawCross(im,cvPoint(ipList[i].x,ipList[i].y));
        CVUtil::DrawCircleFeature(im,cvPoint(ipList[i].x,ipList[i].y),(int)ipList[i].sx2);
  //CVUtil::DrawCircleFeature(im,cvPoint(ipList[i].x,ipList[i].y),8);
}



void HarrisBufferRt::CalculateVelocityHistories() {
  if (pointsToTrack.size()>0) {

    CvPoint2D32f* prev_features=(CvPoint2D32f*)malloc((int)pointsToTrack.size()*sizeof(CvPoint2D32f));
    CvPoint2D32f* curr_features=(CvPoint2D32f*)malloc((int)pointsToTrack.size()*sizeof(CvPoint2D32f));
    char * foundFeature=(char *)malloc((int)pointsToTrack.size()*sizeof(char));

    CvTermCriteria optical_flow_termination_criteria = cvTermCriteria( 
      CV_TERMCRIT_ITER | CV_TERMCRIT_EPS, 20, .3 );

    int i=0;
    std::list<DetectedTrackingPoint>::iterator it;

    for(it=pointsToTrack.begin();it!=pointsToTrack.end(); ++it) {
      if ((*it).trajectory.size()==0)
        (*it).trajectory.push_back(cvPoint2D32f((*it).x,(*it).y));
      prev_features[i]= (*it).trajectory.back();
      i++;
    }

    int tempFrameNum = pointsToTrack.begin()->t;
    tempFrameNum += (int) (pointsToTrack.begin()->trajectory.size()) - 1;

    original.GetFrame(tempFrameNum, opticalFlowLastFrame);
    original.GetFrame(tempFrameNum + 1, opticalFlowNextFrame);

    cvScale(opticalFlowLastFrame, opticalFlowLastFrame8u, 255.0, 0.0);
    cvScale(opticalFlowNextFrame, opticalFlowNextFrame8u, 255.0, 0.0);

    if (pointsToTrack.size() > 0) {
      cvCalcOpticalFlowPyrLK(opticalFlowLastFrame8u,opticalFlowNextFrame8u,NULL,NULL,prev_features,
        curr_features,(int)pointsToTrack.size(),cvSize(3,3),0,foundFeature,NULL,
        optical_flow_termination_criteria,0);
    }	

    i=0;

    for (it = pointsToTrack.begin(); it != pointsToTrack.end(); ++it) {
      if (foundFeature[i])
        (*it).trajectory.push_back(cvPoint2D32f(curr_features[i].x, curr_features[i].y));
      i++;
    }

    i=0;

    for (it=pointsToTrack.begin(); it!=pointsToTrack.end(); ++it) {
      if (!(foundFeature[i]))
      { 
        (*it).trackingFinished=true;
        pointsFinishedTracking.push_back(*it);
      }
      i++;
    }

    free(prev_features);
    free(curr_features);
    free(foundFeature);

  }// if pointsToTrack.size()>0
}

void HarrisBufferRt::finishProcessing()
{
  int tempFrameNum;
  std::list<DetectedTrackingPoint>::iterator it;

  if (pointsToTrack.size()>0)
  {
    tempFrameNum= pointsToTrack.begin()->t;
    tempFrameNum += (int) (pointsToTrack.begin()->trajectory.size());

    while (original.FrameIndices.find(tempFrameNum)!=-1)
    {
      CalculateVelocityHistories();
      tempFrameNum = pointsToTrack.begin()->t;
      tempFrameNum += (int) (pointsToTrack.begin()->trajectory.size());
    }

    for(it=pointsToTrack.begin();it!=pointsToTrack.end(); ++it)
    {
      (*it).trackingFinished=true;
      pointsFinishedTracking.splice(pointsFinishedTracking.end(),pointsToTrack,it);
    }

  }

}

void HarrisBufferRt::AccumulateHistogram(CvMat *tdata, CvMat *wdata, double *hist, int nbins, bool normflag)
{
  // assuming the matrix values are doubles
  double *td=(double*)tdata->data.ptr;
  int bin, ncols=tdata->cols, nrows=tdata->rows;
  for (int i=0;i<nbins;i++) hist[i]=0.0;

  if (wdata) { // *** case: weighted histogram
    double *wd=(double*)wdata->data.ptr;
    for (int i=0;i<ncols*nrows;i++){
      bin=(int)td[i];
      if (bin>=0 && bin<nbins)
        hist[bin]+=wd[i];
    }
  } else { // *** case: UNweighted histogram
    for (int i=0;i<ncols*nrows;i++){
      bin=(int)td[i];
      if (bin>=0 && bin<nbins)
        hist[bin]+=1.0;
    }
  }

  if (normflag) {//*** normalize the histogram
    double sum=0.0;
    for (int i=0;i<nbins;i++) sum+=hist[i];
    if (sum>0.0)
      for (int i=0;i<nbins;i++) hist[i]=hist[i]/sum;
  }
}
