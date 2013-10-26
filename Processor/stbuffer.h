#ifndef STBUFFER_H
#define STBUFFER_H

#include <vector>
#include <list>
#include "cv.h"

#define IMGTYPE			IPL_DEPTH_32F
#define IMG_ELEM_TYPE		float

#define DATATYPE		CV_32FC1
#define mod(x, y)   (x) - (int)floor((x)/(double)(y)) * (y)

class InterestPoint
{
public:
  bool reject;
  int ptype;
  int x,y,t;
  double sx2,st2;
  double val;
  CvMat *features; // <- to be removed
  double *descriptor_data;
  int     descriptor_size;
  std::string descriptor_name;
  double *descriptor2_data;
  int     descriptor2_size;
  std::string descriptor2_name;
  InterestPoint():reject(true),x(0),y(0),t(0),sx2(0),st2(0),val(0),features(NULL),descriptor_data(NULL),descriptor2_data(NULL)
  {
  }
  ~InterestPoint()
  {
    if(features) cvReleaseMat(&features);
    if(descriptor_data) delete [] descriptor_data;
    if(descriptor2_data) delete [] descriptor2_data;
  }
};

typedef std::vector<CvPoint2D32f> PointList;
typedef std::vector<CvPoint2D32f>::iterator PointListIterator;

class DetectedTrackingPoint
{
public:
  int ptype;
  bool reject;
  bool trackingFinished;
  int x,y,t;
  double sx2,st2;
  double val;
  PointList trajectory;


  DetectedTrackingPoint():ptype(5),reject(true),trackingFinished(false),x(0),y(0),t(0),sx2(0),st2(0),val(0)
  {
  }
  DetectedTrackingPoint(bool reject, int x, int y, int t, double sx2, double st2, double val,bool trackingFinished);
  DetectedTrackingPoint(const InterestPoint& ip);

  ~DetectedTrackingPoint()
  {
  }
};

typedef std::vector<DetectedTrackingPoint> TrackingPointList;
typedef std::list<DetectedTrackingPoint> TrackingPointStdList;

typedef std::vector<InterestPoint> InterestPointList;
typedef std::list<InterestPoint> InterestPointStdList;

class CircularIndex {
public:
  std::vector<int> Indices;
  std::vector<int> SortedIndices;
  int LastIndex;	
  int Total;

  CircularIndex() {}

  CircularIndex(int sz) {
    Init(sz);
  }

  void Init(int sz, int k = 0) {
    Indices.reserve(sz);
    SortedIndices.reserve(sz);
    for (int i = 0; i < sz; i++) {
      Indices.push_back(k);
      SortedIndices.push_back(i);
    }
    LastIndex=0;
    Total=0;
  }

  int Add() {
    return Add(Indices[LastIndex] + 1);
  }

  int Add(int k) {
    Total++;
    LastIndex = (LastIndex + 1) % (int)Indices.size();
    Indices[LastIndex] = k;
    return LastIndex;
  }

  int Middle(int sz) {
    int mid = (int) ceil(sz/2.0)-1;
    int t = mod( LastIndex - mid , (int)Indices.size());
    return Indices[t];
  }

  int Last() { return Indices[LastIndex]; }

  int get(int i) {
    assert(i<(int)Indices.size());
    return Indices[i];
  }

  int find(int q) {
    int i;
    for (i=0;i<(int)Indices.size();i++)
      if( Indices[i]==q)
        return i;
    return -1;
  }

  void printIndices() {
    for (int i=0;i<(int)Indices.size()-1;i++)
      printf("%d, ",Indices[i]);
    printf("%d\n",Indices[Indices.size()-1]);
  }

  int maxval() {
    assert(Indices.size()>0);
    int mv=Indices[0];
    for (int i=1;i<(int)Indices.size();i++)
      if (Indices[i]>mv)
        mv=Indices[i];
    return mv;
  }

  int minval() {
    assert(Indices.size()>0);
    int mv=Indices[0];
    for (int i=1;i<(int)Indices.size();i++)
      if (Indices[i]<mv)
        mv=Indices[i];
    return mv;
  }

  std::vector<int>& GetSortedIndices() {
    //increasing
    for (int i = 0; i < (int)Indices.size(); i++)
      SortedIndices[(i + Total + 1) % Indices.size()] = i;
    //decreasing
    for (int i=0;i<(int)Indices.size();i++)
      SortedIndices[i]=abs((int)Indices.size()-SortedIndices[i]-1);
    return SortedIndices;
  }

  void printSortedIndices() {
    std::cout << "Total = " << Total << std::endl;
    for (int i=0;i<(int)Indices.size();i++)
      SortedIndices[(i + Total + 1) % Indices.size()] = i;
    //decreasing
    for (int i=0;i<(int)Indices.size();i++)
      SortedIndices[i]=abs((int)Indices.size() - SortedIndices[i] - 1);
    for (int i=0;i<(int)SortedIndices.size()-1;i++)
      printf("%d, ",SortedIndices[i]);
    printf("%d\n",SortedIndices[SortedIndices.size()-1]);
  }

  void print(std::ostream& of) {
    of<<"D: ";
    for (int i=0;i<(int)Indices.size();i++)
      of<<Indices[i]<<"\t";
    of<<std::endl;
    of<<"S: ";
    GetSortedIndices();
    for (int i=0;i<(int)Indices.size();i++)
      of<<SortedIndices[i]<<"\t";
    of<<std::endl;
  }
};

class STBuffer {
public:
  CvMat* Buffer;
  int BufferSize;
  int Width,Height;
  CircularIndex FrameIndices;

  // to be sure that compiler generated copy const is not used
  // These are not supported
  // and are not implemented!
  STBuffer(const STBuffer& x);
  STBuffer& operator=(const STBuffer& x);

private:
  /* Neighbourhood masks are arrays of size n by 3 where 
  n is the number of neighbours in the mask and 
  second index indicates  t,x,y respectively */
  int Neighbs3x3x3[26][3]; //3^3 - 1
  int Neighbs3x3p2[10][3]; // 8 (3^2-1) neighbours in the space  and just 2 neighbours in the time
  int Neighbs5x5x5[124][3]; //5^3 - 1
  int Neighbs5x5p4[28][3];// 24 (5^2-1) neighbours in the space  and just 4 neighbours in the time
  void CreateLocalMasks();

public:
  STBuffer(void);
  STBuffer(int);

  ~STBuffer(void);

  void Init(int size);
  IplImage* GetFrame(int istamp);
  int GetFrame(int istamp,IplImage* dst);
  int GetSingleFrame(int istamp,IplImage* dst);
  void Update(IplImage*); 
  void Update(IplImage*,  int); 
  int TemporalConvolve(IplImage* dst,std::vector<double> mask);
  void FindLocalMaxima(InterestPointList& pts,bool full=false);
  void GetDataBBox(int x1,int y1,int t1,int x2,int y2,int t2, CvMat* rm);
  void GetLocalRegion(int x,int y,int t, int nx,int ny,int nt,CvMat* rm);
  int ExponentialSmooth(IplImage* ds, std::vector<double> mask);
};

#endif //STBUFFER_H
