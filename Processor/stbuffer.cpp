#include "pcheader.h"

#include "stbuffer.h"

DetectedTrackingPoint::DetectedTrackingPoint(bool inReject, int inx, int iny, int inTime, double insx2, double inst2, double inval,bool intrackingFinished=false)
{
  reject=inReject;
  ptype=5;
  x=inx;
  y=iny;
  t=inTime;
  sx2=insx2;
  st2=inst2;
  val=inval;
  trackingFinished=intrackingFinished;
}

DetectedTrackingPoint::DetectedTrackingPoint(const InterestPoint & ip)
{
  reject=ip.reject;
  ptype=ip.ptype;
  x=ip.x;
  y=ip.y;
  t=ip.t;
  sx2=ip.sx2;
  st2=ip.st2;
  val=ip.val;
}

STBuffer::STBuffer(void):Buffer(NULL),BufferSize(0),Width(0),Height(0)
{			
  CreateLocalMasks();
}

STBuffer::STBuffer(int size):Buffer(NULL),Width(0),Height(0)
{
  Init(size);
  CreateLocalMasks();
}


void STBuffer::CreateLocalMasks() {
  //creating neighbourhood masks
  int s=0,i,j,k;
  //3x3x3 mask
  s=0;
  for(k=-1;k<=1;k++)
    for(i=-1;i<=1;i++)
      for(j=-1;j<=1;j++)
      {
        if(i||j||k) //exclude the center of the mask
        {
          Neighbs3x3x3[s][0]=k;
          Neighbs3x3x3[s][1]=i;
          Neighbs3x3x3[s][2]=j;
          s++;
        }
      }
      //5x5x5 mask
      s=0;
      for(k=-2;k<=2;k++)
        for(i=-2;i<=2;i++)
          for(j=-2;j<=2;j++)
          {
            if(i||j||k) //exclude the center of the mask
            {
              Neighbs5x5x5[s][0]=k;
              Neighbs5x5x5[s][1]=i;
              Neighbs5x5x5[s][2]=j;
              s++;
            }
          }

          //3x3p2 mask
          s=0;
          for(i=-1;i<=1;i++)
            for(j=-1;j<=1;j++)
            {
              if(i||j) //exclude the center of the mask
              {
                Neighbs3x3p2[s][0]=0;
                Neighbs3x3p2[s][1]=i;
                Neighbs3x3p2[s][2]=j;
                s++;
              }
            }
            Neighbs3x3p2[s][0]=1;Neighbs3x3p2[s][1]=0;Neighbs3x3p2[s][2]=0;s++;
            Neighbs3x3p2[s][0]=-1;Neighbs3x3p2[s][1]=0;Neighbs3x3p2[s][2]=0;

            //8p4 mask
            s=0;
            for(i=-2;i<=2;i++)
              for(j=-2;j<=2;j++)
              {
                if(i||j) //exclude the center of the mask
                {
                  Neighbs5x5p4[s][0]=0;
                  Neighbs5x5p4[s][1]=i;
                  Neighbs5x5p4[s][2]=j;
                  s++;
                }
              }
              Neighbs5x5p4[s][0]=-2;Neighbs5x5p4[s][1]=Neighbs5x5p4[s][2]=0;s++;
              Neighbs5x5p4[s][0]=-1;Neighbs5x5p4[s][1]=Neighbs5x5p4[s][2]=0;s++;
              Neighbs5x5p4[s][0]=1;Neighbs5x5p4[s][1]=Neighbs5x5p4[s][2]=0;s++;
              Neighbs5x5p4[s][0]=2;Neighbs5x5p4[s][1]=Neighbs5x5p4[s][2]=0;
}

STBuffer::~STBuffer(void)
{
  if(Buffer)
    cvReleaseMat(&Buffer);
}

void STBuffer::Init(const int size) {
  BufferSize = size;
}

int STBuffer::GetSingleFrame(int i,IplImage* dst)
{
  if(i<0 || i>=BufferSize)
    return -1;
  assert(dst->widthStep * dst->height == Buffer->step);
  memcpy((void*)dst->imageData,Buffer->data.ptr + Buffer->step*i ,Buffer->step);
  /*CvMat *r,rh;
  r=cvGetRow( Buffer , &rh, i ) ;*/
  return 0;

}
int STBuffer::GetFrame(int istamp,IplImage* dst)
{
  int i=FrameIndices.find(istamp);
  return GetSingleFrame(i,dst);
}

void STBuffer::Update(IplImage* newframe)
{
  if(!Buffer) {
    FrameIndices.Init(BufferSize);
    Width = newframe->width;
    Height = newframe->height;
    Buffer = cvCreateMat(BufferSize, Width * Height, DATATYPE);
  }
  else
  {
    int k = FrameIndices.Add();
    assert(newframe->widthStep * newframe->height == Buffer->step);
    memcpy((void*)(Buffer->data.ptr + Buffer->step * k) ,
      newframe->imageData,Buffer->step);
  }

}

void STBuffer::Update(IplImage* newframe, int istamp) {
  if(!Buffer) {
    FrameIndices.Init(BufferSize, istamp);
    /*for (  int i=0;i<BufferSize;i++)
    Data.push_back(cvCloneImage(newframe)); */
    Width=newframe->width;Height=newframe->height;
    Buffer=cvCreateMat(BufferSize, Width*Height,DATATYPE);
  } else {
    int k = FrameIndices.Add(istamp);
    assert(newframe->widthStep * newframe->height == Buffer->step);
    memcpy((void*)(Buffer->data.ptr + Buffer->step * k) ,
      newframe->imageData,Buffer->step);
  }
}

int STBuffer::ExponentialSmooth(IplImage* dst, std::vector<double> mask) {
  int	tfsz = (int)mask.size();
  assert(tfsz<=BufferSize);

  int i;

  if(BufferSize) {
    assert(dst->widthStep * dst->height == Buffer->step);
  }

  int tstampres = FrameIndices.Last();

  if ((int)mask.size() < BufferSize)
    for(i=(int)mask.size();i<BufferSize;i++)
      mask.push_back(0);

  // If current frame is t,
  // Sorted[i] = the index of (t - i)th frame in FrameIndices. 
  std::vector<int> Sorted = FrameIndices.GetSortedIndices();

  CvMat *fil = cvCreateMat(1, BufferSize, DATATYPE);
  assert(BufferSize == (int)mask.size()); //filter is too big (it could be cut)
  IMG_ELEM_TYPE* filter = new IMG_ELEM_TYPE[BufferSize];

  for (i = 0; i < BufferSize; i++)
    filter[Sorted[i]] = (IMG_ELEM_TYPE)mask[i];

  for(i=0;i<BufferSize;i++)
    cvmSet(fil,0,i, filter[i]);

  CvMat *rdst, dsthdr;
  rdst = cvReshape(dst, &dsthdr, 0, 1);
  cvMatMul(fil, Buffer, rdst);

  //memcpy((void*)(Buffer->data.ptr + Buffer->step * FrameIndices.LastIndex),
         //dst->imageData, Buffer->step);

  delete[] filter;
  cvReleaseMat(&fil);

  return tstampres;
}
/*
mask should be a symmetric filter and its size should be an odd number
*/
int STBuffer::TemporalConvolve(IplImage* dst,std::vector<double> mask)
{
  int	tfsz=(int)mask.size();
  assert(tfsz<=BufferSize);

  int i;

  if(BufferSize) {
    assert(dst->widthStep * dst->height == Buffer->step);
    assert(BufferSize >= 3);
  }

  assert(tfsz % 2); //the size of filter should be odd

  int tstampres = FrameIndices.Middle(tfsz);

  if((int)mask.size() < BufferSize)
    for(i = (int)mask.size(); i < BufferSize;i++)
      mask.push_back(0);

  std::vector<int> Sorted = FrameIndices.GetSortedIndices();

  CvMat *fil = cvCreateMat(1, BufferSize, DATATYPE);
  assert(BufferSize == (int)mask.size()); //filter is too big (it could be cut)
  IMG_ELEM_TYPE* filter=new IMG_ELEM_TYPE[BufferSize];

  for (i = 0; i < BufferSize; i++)
    filter[Sorted[i]] = (IMG_ELEM_TYPE)mask[i];

  for(i=0;i<BufferSize;i++)
    cvmSet(fil,0,i, filter[i]);

  CvMat *rdst, dsthdr;
  rdst = cvReshape(dst, &dsthdr, 0, 1);
  cvMatMul(fil, Buffer, rdst);

  delete[] filter;
  cvReleaseMat(&fil);

  return tstampres;
}


void STBuffer::GetDataBBox(int x1,int y1,int t1,int x2,int y2,int t2, CvMat* rm)
{
  //printf("GetDataBBox > bbox=[%d %d %d %d %d %d]\n",x1,y1,t1,x2,y2,t2);
  int tmin=FrameIndices.minval();
  int tmax=FrameIndices.maxval();
  //printf("GetDataBBox > tmin=%d; tmax=%d\n",tmin,tmax);
  int nx=x2-x1+1; assert(nx>0);
  int ny=y2-y1+1; assert(ny>0);
  int nt=t2-t1+1; assert(nt>0);
  assert(rm);
  assert(rm->rows*rm->cols==nx*ny*nt);

  int *xvalid = new int[nx];
  int *yvalid = new int[ny];
  int *tvalid = new int[nt];
  for (int i=0;i<nx;i++){
    xvalid[i]=x1+i;
    if (x1+i<0) xvalid[i]=0; 
    if (x1+i>=Width) xvalid[i]=Width-1;
  }
  for (int i=0;i<ny;i++){
    yvalid[i]=y1+i;
    if (y1+i<0) yvalid[i]=0;
    if (y1+i>=Height) yvalid[i]=Height-1;
  }
  for (int i=0;i<nt;i++){
    tvalid[i]=t1+i;
    if (t1+i<tmin) {tvalid[i]=tmin;}
    if (t1+i>tmax) {tvalid[i]=tmax;}
  }

  int cols=Width;
  int rows=Height;
  int cc=rows*cols;
  IMG_ELEM_TYPE *D = (IMG_ELEM_TYPE*)Buffer->data.ptr; //prb: if DATATYPE changes 
#define GDBBELEM(k,i,j)  *(D + (k)*(cc) + (i)*(cols)+(j) )

  //FrameIndices.print(std::cout);
  int x,y,t;
  double *dat=(double*)rm->data.ptr;
  for (int it=0;it<nt;it++){
    t=tvalid[it];
    int buffind=FrameIndices.find(t);
    //printf("GetLocalRegion > it=%d; t=%d; buffind=%d\n",it,t,buffind);
    for(int ix=0;ix<nx;ix++){
      x=xvalid[ix];
      for(int iy=0;iy<ny;iy++){
        y=yvalid[iy];
        dat[it*nx*ny+iy*nx+ix]=(IMG_ELEM_TYPE)GDBBELEM(buffind,y,x);
      }
    }
  }
  //printf("GetLocalRegion > done.\n");
  delete [] xvalid;
  delete [] yvalid;
  delete [] tvalid;
}

void STBuffer::GetLocalRegion(int x,int y,int t, 
                              int nx,int ny,int nt,	CvMat* rm)
{
  assert(nx%2);
  assert(ny%2);
  assert(nt%2);
  assert(rm);
  assert( (rm->rows==nx*ny*nt && rm->cols==1) ||  
    (rm->cols==nx*ny*nt && rm->rows==1) );

  //t=FrameIndices.find(t);

  int dx=nx/2,dy=ny/2,dt=nt/2;
  int i,j,k,s;


  std::vector<int> Sorted =FrameIndices.GetSortedIndices();
  int *SI=new int[Sorted.size()];
  for(s=0;s<(int)Sorted.size();s++) SI[s]=Sorted[s];

#ifdef DEBUG
  if(!(x>dx && x<Width-dx))
    printf("problem in X\\n");

  if(!(y>dy && y<Height-dy))
    printf("problem in Y\\n");

  if(!(SI[t]>dt && SI[t]<BufferSize-dt))
    printf("problem in T: %d\\n",t);
#endif

  assert(x>dx && x<Width-dx);
  assert(y>dy && y<Height-dy);
  assert(SI[t]>dt && SI[t]<BufferSize-dt);

  /* in MEX 3D-Array  M(i,j,k) with size [rows cols depth]
  linearized: k*rows*cols+ j * rows + i
  */
  int cols=Width;
  int rows=Height;
  int cc=rows*cols;
  IMG_ELEM_TYPE *D = (IMG_ELEM_TYPE*)Buffer->data.ptr; //prb: if DATATYPE changes 
#define NN(k,i,j)  *(D + (k)*(cc) + (i)*(cols)+(j) )

  //printf("GetLocalRegion > SI[t]+k-dt = %d\n",SI[t]+0-dt);

  FrameIndices.print(std::cout);
  printf("GetLocalRegion > t_orig=%d\n",t);
  IMG_ELEM_TYPE kir;
  double *dat=(double*)rm->data.ptr;	
  for(k=0;k<nt;k++){
    int buffind=FrameIndices.find(t-dt+k);
    printf("GetLocalRegion > t=%d; buffind=%d\n",t-dt+k,buffind);
    for(i=0;i<nx;i++)
      for(j=0;j<ny;j++)
      {
        kir=NN(buffind,x+i-dx,y+j-dy);//????????  or SI[t+k-dt]
        //kir=NN(SI[t]+k-dt,x+i-dx,y+j-dy);//????????  or SI[t+k-dt]
        dat[k*nx*ny+j*nx+i]=kir;
      }
  }
  delete[] SI;
}

void STBuffer::FindLocalMaxima(InterestPointList& pts, bool full)
{
  int cols=Width;
  int rows=Height;
  int cc=rows*cols;
  IMG_ELEM_TYPE *D = (IMG_ELEM_TYPE*)Buffer->data.ptr; //prb: if DATATYPE changes 
#define MM(k,i,j)  *(D + (k)*(cc) + (i)*(cols)+(j) )

  int i,j,k,s,n;
  IMG_ELEM_TYPE v;

  int db;//exclude borders
  int ns;//number of neighbours in the mask
  int *neighbs;// pointer to the mask array

  db=1;
  if (full) {
    ns=26; neighbs=(int*)Neighbs3x3x3;
  } else {
    ns=10; neighbs=(int*)Neighbs3x3p2;
  }

  std::vector<int> Sorted =FrameIndices.GetSortedIndices();
  int *SI=new int[Sorted.size()];
  for(s=0;s<(int)Sorted.size();s++) SI[s]=Sorted[s];

  InterestPoint pt;
  for(k=db;k<BufferSize-db;k++)
    for(i=db;i<rows-db;i++)
      for(j=db;j<cols-db;j++)
      {
        s=0;
        v=MM(SI[k],i,j);
        for(n=0;n<ns;n++)
          if ( v > MM(SI[neighbs[3*n+0]+k], neighbs[3*n+1]+i, neighbs[3*n+2]+j) )
            s++;
        if(s==ns)//local maxima
        {
          pt.x=j;pt.y=i;pt.t=FrameIndices.get(SI[k]);pt.val=v;
          pts.push_back(pt);
        }
      }
      delete[] SI;
}
