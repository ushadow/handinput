#include "pcheader.h"
#include "msharrisbuffer.h"

MultiScaleHarrisBuffer::MultiScaleHarrisBuffer(void)
{
  kparam=5e-4;
    nxplev=3;
    SignificantPointThresh=1E-9;
    Border=5;
    patchsizefactor=5.0;
    initpyrlevel=0;

  iFrame=0;

  // set pre-defined scales
  sx2all.push_back(4.0);
  sx2all.push_back(8.0);
  st2all.push_back(2.0);
  st2all.push_back(4.0);

  vis=NULL;

  //descriptortype="hoghof";
  descriptortype="of";
}

MultiScaleHarrisBuffer::~MultiScaleHarrisBuffer(void)
{
  if(vis)
    cvReleaseImage(&vis);

  /*
  if (framePyr) {
    for(int i=0; i<nxplev; i++)
      if (framePyr[i])
        cvReleaseImage(&framePyr[i]);
    delete [] framePyr;
  }

  if (framePrevPyr) {
    for(int i=0; i<nxplev; i++)
      if (framePrevPyr[i])
        cvReleaseImage(&framePrevPyr[i]);
    delete [] framePrevPyr;
  }
  */
}
bool MultiScaleHarrisBuffer::Init(IplImage* firstfrm,std::string fname,std::string sourcename,int offset)
{
  // create image pyramid
  IplImage *pyrLevel=cvCloneImage(firstfrm);
  IplImagePtr *framePyr = new IplImagePtr[nxplev];
        // get to the initial resolution level
        for (int i=0;i<initpyrlevel;i++){
    // spatial smoothing
    cvSmooth(pyrLevel,pyrLevel,CV_GAUSSIAN,3,3);
    int xsz=(int)((firstfrm->width)/pow(2,(double)i+1));
    int ysz=(int)((firstfrm->height)/pow(2,(double)i+1));
    IplImage* subframe=cvCreateImage(cvSize(xsz,ysz),firstfrm->depth,firstfrm->nChannels);
                // spatial subsampling
    cvResize(pyrLevel,subframe,CV_INTER_NN);
    cvReleaseImage(&pyrLevel);
    pyrLevel=subframe;
  }
  framePyr[0]=cvCloneImage(pyrLevel);
  for(int i=1; i<nxplev; i++){
    // spatial smoothing
    cvSmooth(pyrLevel,pyrLevel,CV_GAUSSIAN,3,3);
    int xsz=(int)((firstfrm->width)/pow(2,(double)i+initpyrlevel));
    int ysz=(int)((firstfrm->height)/pow(2,(double)i+initpyrlevel));
    IplImage* subframe=cvCreateImage(cvSize(xsz,ysz),firstfrm->depth,firstfrm->nChannels);
                // spatial subsampling
    cvResize(pyrLevel,subframe,CV_INTER_NN);
    framePyr[i]=subframe;
    cvReleaseImage(&pyrLevel);
    pyrLevel=cvCloneImage(subframe);
  }
  cvReleaseImage(&pyrLevel);

  
  framePrevPyr = new IplImagePtr[nxplev];
  for(int i=0; i<nxplev; i++)
    framePrevPyr[i] = NULL;


  vis = cvCreateImage(cvGetSize(firstfrm),IMGTYPE,1);

  // debug: show pyramid images
  //for(int i=0; i<nxplev; i++){
  //	cvShowImage("ST-Demo",framePyr[i]);
  //	printf("showing pyr-level %d of %d, img.size=(%d,%d)\n",i+1,nxplev,framePyr[i]->width,framePyr[i]->height);
  //	cvWaitKey();
  //}

  // initialize the buffer list
  hbListSize=(int)sx2all.size()*(int)st2all.size()*nxplev;
  hbList = new HarrisBuffer[hbListSize];

        //*** initialize multi-scale harris buffers
  int hb_ind=0;
  for(int i=0; i<nxplev; i++){
    for (int j=0; j<(int)sx2all.size(); j++)
      for (int k=0; k<(int)st2all.size(); k++){
        hbList[hb_ind].patchsizefactor=patchsizefactor;
        hbList[hb_ind].descriptortype=descriptortype;
        hbList[hb_ind].sig2=sx2all[j];
        hbList[hb_ind].tau2_ =st2all[k];
        hbList[hb_ind].kparam=kparam;
        hbList[hb_ind].SignificantPointThresh=SignificantPointThresh;
        hbList[hb_ind].Border=Border;
        hbList[hb_ind].framemax=framemax;
        hbList[hb_ind].Init(framePyr[i],"");
        hb_ind++;
      }
  }
  
    // frame offset
  frame_offset=offset;

  

  if(!fname.empty()){
      FeatureFile.open(fname.c_str());
      FeatureFile<<"# STIP-format-1.0"<<std::endl;
      FeatureFile<<"# source: \""<<sourcename<<"\" ";
      FeatureFile<<"frame-width:"<<firstfrm->width<<" ";
      FeatureFile<<"frame-height:"<<firstfrm->height<<" ";
      FeatureFile<<"frame-offset:"<<offset<<std::endl;
      FeatureFile<<"# point-type x y t sigma2 tau2 detector-confidence";
      //if (descriptortype=="hoghof") FeatureFile<<" dscr-hog(72) dscr-hof(90)";
      //if (descriptortype=="hog") FeatureFile<<" dscr-hog(72)";
      //if (descriptortype=="hof") FeatureFile<<" dscr-hof(90)";
      //if (descriptortype=="hnf") FeatureFile<<" dscr-hnf(144)";
      if (descriptortype=="of") FeatureFile<<" dscr-of(non-uniform-length)";
      FeatureFile<<std::endl;
    
  }
  for(int i=0; i<nxplev; i++){
    cvReleaseImage(&framePyr[i]);
  }
  delete [] framePyr;
  return true;
}


void MultiScaleHarrisBuffer::ProcessFrame(IplImage* frm)
{	
  // create image pyramid
  IplImage *pyrLevel=cvCloneImage(frm);
  IplImagePtr *framePyr = new IplImagePtr[nxplev];
        // get to the initial resolution level
        for (int i=0;i<initpyrlevel;i++){
    // spatial smoothing
    cvSmooth(pyrLevel,pyrLevel,CV_GAUSSIAN,3,3);
    int xsz=(int)((frm->width)/pow(2,(double)i+1));
    int ysz=(int)((frm->height)/pow(2,(double)i+1));
    IplImage* subframe=cvCreateImage(cvSize(xsz,ysz),frm->depth,frm->nChannels);
                // spatial subsampling
    cvResize(pyrLevel,subframe,CV_INTER_NN);
    cvReleaseImage(&pyrLevel);
    pyrLevel=subframe;
  }
  framePyr[0]=cvCloneImage(pyrLevel);
        //printf("construction: pyr.level: Width=%d, Height=%d\n",framePyr[0]->width,framePyr[0]->height);
  for(int i=1; i<nxplev; i++){
    // spatial smoothing
    cvSmooth(pyrLevel,pyrLevel,CV_GAUSSIAN,3,3);
    int xsz=(int)((frm->width)/pow(2,(double)i+initpyrlevel));
    int ysz=(int)((frm->height)/pow(2,(double)i+initpyrlevel));
    framePyr[i]=cvCreateImage(cvSize(xsz,ysz),frm->depth,frm->nChannels);
                // spatial subsampling
    cvResize(pyrLevel,framePyr[i],CV_INTER_NN);
    cvReleaseImage(&pyrLevel);
    pyrLevel=cvCloneImage(framePyr[i]);
          //printf("construction: pyr.level: Width=%d, Height=%d\n",framePyr[i]->width,framePyr[i]->height);
  }
        cvReleaseImage(&pyrLevel);

  for(int i=0; i<nxplev; i++)
    if (!framePrevPyr[i])
      framePrevPyr[i]=cvCloneImage(framePyr[i]);


  //msipList.clear();
  int hb_ind=0;
  for(int i=0; i<nxplev; i++){
    // estimate Optical Flow once per spatial resolution level
    IplImage* OFx;
    IplImage* OFy;
    
    
    OFx=NULL;
    OFy=NULL;
    
    
    //printf("pyr.level: Width=%d, Height=%d\n",framePyr[i]->width,framePyr[i]->height);
    for (int j=0; j<(int)sx2all.size(); j++)
      for (int k=0; k<(int)st2all.size(); k++){
        //printf("  processing pyr.level at scale %.1f,%.1f\n",hbList[hb_ind].sig2,hbList[hb_ind].tau2);
        hbList[hb_ind].ProcessFrame(framePyr[i],OFx,OFy);
          //Here's where I've gotta correct the feature list, and write it
          std::list<DetectedTrackingPoint>::iterator it;
          
          for(it=hbList[hb_ind].pointsFinishedTracking.begin();it!=hbList[hb_ind].pointsFinishedTracking.end(); ++it)
          {
            (*it).sx2*=pow(2.0,2.0*(double)(i+initpyrlevel));
            (*it).x*=(int)(pow(2.0,(double)i+initpyrlevel));
            (*it).y*=(int)(pow(2.0,(double)i+initpyrlevel));
            (*it).t+=frame_offset;
            WriteDetectedTrackingFeature(*it);
            
          }
          hbList[hb_ind].pointsFinishedTracking.clear();
        
        
        hb_ind++;
      }
    //cvWaitKey();
    
  }

  for(int i=0; i<nxplev; i++)
    cvCopy(framePyr[i],framePrevPyr[i]);


  for(int i=0; i<nxplev; i++){
  cvReleaseImage(&framePyr[i]);
  }
  delete [] framePyr;

  iFrame++;
}

void MultiScaleHarrisBuffer::finishProcessing()
{
  
    int hb_ind=0;
    for(int i=0; i<nxplev; i++)
      for (int j=0; j<(int)sx2all.size(); j++)
        for (int k=0; k<(int)st2all.size(); k++)
        {
          hbList[hb_ind].finishProcessing();
          std::list<DetectedTrackingPoint>::iterator it;
          
          for(it=hbList[hb_ind].pointsFinishedTracking.begin();it!=hbList[hb_ind].pointsFinishedTracking.end(); ++it)
          {
            (*it).sx2*=pow(2.0,2.0*(double)(i+initpyrlevel));
            (*it).x*=(int)(pow(2.0,(double)i+initpyrlevel));
            (*it).y*=(int)(pow(2.0,(double)i+initpyrlevel));
            (*it).t+=frame_offset;
            WriteDetectedTrackingFeature(*it);
            
          }
          hbList[hb_ind].pointsFinishedTracking.clear();
        
        
          hb_ind++;
        }
    
  

}


void MultiScaleHarrisBuffer::DetectInterestPoints(int border)
{
  for(int i=0;i<hbListSize;i++)
    hbList[i].DetectInterestPoints(border);
}

int MultiScaleHarrisBuffer::NumberOfDetectedIPs()
{
  int n=0;
  for(int i=0;i<hbListSize;i++)
    n=n+hbList[i].NumberOfDetectedIPs();
  return n;
}

int MultiScaleHarrisBuffer::NumberOfDetectedTPs()
{
  //printf("Started mshb::numDetTPs\n");
  int retval=0;
  int hb_ind=0;
  for(int i=0; i<nxplev; i++){
    for (int j=0; j<(int)sx2all.size(); j++)
      for (int k=0; k<(int)st2all.size(); k++){
        retval+=hbList[hb_ind].NumberOfDetectedTPs();
        hb_ind++;
      }
  }
  //printf("finished mshb::numDetTPs\n");
  return retval;
}

void MultiScaleHarrisBuffer::DrawInterestPoints(IplImage* im)
{	
  for(int i=0;i<hbListSize;i++)
    hbList[i].DrawInterestPoints(im);
}


IplImage* MultiScaleHarrisBuffer::getMSHBufferImage(int type)
{
  IplImage *hbimg=hbList[0].getHBufferImage(type);
  cvResize(hbimg,vis);
  return vis;

  //return hbList[0].getHBufferImage(type);
}

void MultiScaleHarrisBuffer::WriteFeature(InterestPoint &ip)
{
  assert(ip.features);
  //double *data=(double*)(ip.features->data.ptr);
  FeatureFile<<ip.ptype<<"\t";
  FeatureFile<<ip.x<<"\t"<<ip.y<<"\t"<<ip.t<<"\t";
  FeatureFile<<ip.sx2<<"\t"<<ip.st2<<"\t";
  FeatureFile<<ip.val<<"\t";
  //for(int i=0;i<34;i++)
  //	FeatureFile<<data[i]<<"\t";
  if (ip.descriptor_data)
    for(int i=0;i<ip.descriptor_size;i++)
      FeatureFile<<ip.descriptor_data[i]<<"\t";
  if (ip.descriptor2_data)
    for(int i=0;i<ip.descriptor2_size;i++)
      FeatureFile<<ip.descriptor2_data[i]<<"\t";
  FeatureFile<<std::endl;
}

void MultiScaleHarrisBuffer::WriteDetectedTrackingFeature(DetectedTrackingPoint &ip)
{
  FeatureFile<<ip.ptype<<"\t";
  FeatureFile<<ip.x<<"\t"<<ip.y<<"\t"<<ip.t<<"\t";
  FeatureFile<<ip.sx2<<"\t"<<ip.st2<<"\t";
  FeatureFile<<ip.val<<"\t";
  PointListIterator it;
  for (it=ip.trajectory.begin();it<ip.trajectory.end();it++)
    FeatureFile<<(*it).x<<"\t"<<(*it).y<<"\t";
  FeatureFile<<std::endl;
}

void MultiScaleHarrisBuffer::OpticalFlowFromLK(const IplImage* prevgray, const IplImage* gray, IplImage* OFx, IplImage* OFy)
{
  //cvCalcOpticalFlowLK(prevgray, gray, cvSize(15,15), OFx, OFy);
  //cvCalcOpticalFlowHS(prevgray, gray, 0, OFx, OFy, 0.1, cvTermCriteria(CV_TERMCRIT_ITER | CV_TERMCRIT_EPS,100,1e5));

  float subf=5;
  int xsz=gray->width, ysz=gray->height;
  int pxn=int(xsz/subf), pyn=int(ysz/subf);
  CvPoint2D32f *p1 = new CvPoint2D32f[pxn*pyn];
  CvPoint2D32f *p2 = new CvPoint2D32f[pxn*pyn];
  for (int i=0; i<pyn; i++)
    for (int j=0; j<pxn; j++){
      p1[i*pxn+j].x=j*subf+subf/2; p1[i*pxn+j].y=i*subf+subf/2;
      p2[i*pxn+j].x=j*subf+subf/2; p2[i*pxn+j].y=i*subf+subf/2;
    }

  char *sts = new char[pxn*pyn];
  CvTermCriteria termination = cvTermCriteria(CV_TERMCRIT_ITER | CV_TERMCRIT_EPS, 100, 1e5);
  int nlevels=3;
  int winsemisize=5;
  cvCalcOpticalFlowPyrLK(prevgray, gray, NULL, NULL, 
               p1, p2, int(pxn*pyn), cvSize(winsemisize,winsemisize),
               nlevels,sts,NULL,termination,CV_LKFLOW_INITIAL_GUESSES);
  
  IplImage* OFxsub= cvCreateImage(cvSize(pxn,pyn),IMGTYPE,1);
  IplImage* OFysub= cvCreateImage(cvSize(pxn,pyn),IMGTYPE,1);
  IMG_ELEM_TYPE *ptrOFxsub=(IMG_ELEM_TYPE*)cvPtr2D(OFxsub,0,0);
  IMG_ELEM_TYPE *ptrOFysub=(IMG_ELEM_TYPE*)cvPtr2D(OFysub,0,0);
  for (int i=0; i<pyn; i++)
    for (int j=0; j<pxn; j++){
      ptrOFxsub[i*pxn+j]=p2[i*pxn+j].x-p1[i*pxn+j].x;
      ptrOFysub[i*pxn+j]=p2[i*pxn+j].y-p1[i*pxn+j].y;
    }

  cvResize(OFxsub,OFx,CV_INTER_NN);
  cvResize(OFysub,OFy,CV_INTER_NN);

  cvReleaseImage(&OFxsub);
  cvReleaseImage(&OFysub);

  delete [] p1;
  delete [] p2;
  delete [] sts;
}
