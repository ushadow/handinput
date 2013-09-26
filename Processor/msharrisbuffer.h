
#ifndef MSHARRISBUFFER_H
#define MSHARRISBUFFER_H

#include "cv.h"
#include "stbuffer.h"
#include "harrisbuffer.h"

typedef IplImage* IplImagePtr;

class MultiScaleHarrisBuffer
{
private:
    std::ofstream FeatureFile;
    HarrisBuffer *hbList;

    IplImage* vis;
    
    //IplImagePtr *framePyr;
    IplImagePtr *framePrevPyr;

    int iFrame;

    void WriteFeature(InterestPoint &ip);
    void WriteDetectedTrackingFeature(DetectedTrackingPoint &ip);
    
public:
    int hbListSize;
        int frame_offset; // temporal offset

    std::vector<double> sx2all;
    std::vector<double> st2all;

    InterestPointList msipList;

    double kparam;	  //parameter of point detection with the default value 0.0005
    double SignificantPointThresh;
    int Border;
    int framemax;

    int nxplev;
    int initpyrlevel;

    double patchsizefactor;
    std::string descriptortype;

    MultiScaleHarrisBuffer(void);
    ~MultiScaleHarrisBuffer(void);

    IplImage* getMSHBufferImage(int type);

    bool Init(IplImage* firstfrm, std::string fname, std::string sourcename, int offset);
    void ProcessFrame(IplImage* frame);
    void finishProcessing();
    void DetectInterestPoints(int border=0);
    void DrawInterestPoints(IplImage* im);
    int NumberOfDetectedIPs();
    int NumberOfDetectedTPs();

    void OpticalFlowFromLK(const IplImage* prevgray, const IplImage* gray, IplImage* OFx,IplImage* OFy);
};

#endif //MSHARRISBUFFER_H
