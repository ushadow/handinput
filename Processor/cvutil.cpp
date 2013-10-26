#include "pcheader.h"

#include "cvutil.h"

CVUtil::CVUtil(void)
{
}

CVUtil::~CVUtil(void)
{
}


void CVUtil::ShowRealImage(char* win, IplImage* im)
{
  //double m, M;
  //cvMinMaxLoc(im, &m, &M, NULL, NULL, NULL);
  cvNormalize(im,im,1,0,CV_MINMAX); 
  //cvMinMaxLoc(im, &m, &M, NULL, NULL, NULL);
    //cvScale(im, im, 1.0/(M-m), 1.0*(-m)/(M-m));
  cvShowImage(win, im );
}
 

void CVUtil::DrawCross(IplImage* im, CvPoint pt, int length)
{
  int linetype = 8;// CV_AA;
  int s=2; 
  cvLine( im, cvPoint(pt.x,pt.y+s), cvPoint(pt.x,pt.y+length), CV_COLOR_RED , 1,linetype, 0 );
  cvLine( im, cvPoint(pt.x+s,pt.y), cvPoint(pt.x+length,pt.y), CV_COLOR_RED , 1,linetype, 0 );
  cvLine( im, cvPoint(pt.x,pt.y-s), cvPoint(pt.x,pt.y-length), CV_COLOR_RED , 1,linetype, 0 );
  cvLine( im, cvPoint(pt.x-s,pt.y), cvPoint(pt.x-length,pt.y), CV_COLOR_RED , 1,linetype, 0 );
}

void CVUtil::DrawCircleFeature(IplImage* im, CvPoint pt, int sc)
{
  int radius=(int)floor(3.0*sqrt((double)sc));
  int thickness=3;
  cvCircle(im, pt, radius, CV_COLOR_YELLOW, thickness);
}

std::vector<double> CVUtil::GaussianMask1D(double variance, int masksize, int szfct)
{
  //const double MINVAL=1E-6;
  const double MINVAL=0.0;
  int sz=masksize/2;
  if (!masksize)
    sz=(int)(sqrt(variance)*szfct);
  double *tmp=new double[2 * sz + 1];	
  double sum=0;
  int x;
  for(x=-sz ; x<=sz ; x++)
    sum+=tmp[x+sz]=exp(-x*x/(2*variance));
  for(x=-sz ; x<=sz && sum ; x++)
    tmp[x+sz]/=sum;
  int nnz=0;
  for(x=1; x<=sz ; x++)
    if(tmp[x]>MINVAL)
      nnz++;
  std::vector<double> mask;
  for(int i=0;i<2*nnz+1;i++)
    mask.push_back(0);
  for(x=-nnz; x<=nnz ; x++)
    mask[x+nnz]=tmp[x+nnz];	

  delete[] tmp;
  return mask;
}

//std::vector<double> CVUtil::GaussianMask1D(double variance, int masksize, int szfct)
//{
//	const double MINVAL=1E-5;
//	int sz=masksize/2;
//	if (!masksize)
//		sz=(int)(sqrt(variance)*szfct);
//	int sz3=(int)(sqrt(variance)*3.0);
//	if (sz3>sz) sz3=sz;
//	double *tmp=new double[2*sz+1];	
//	double sum=0;
//	int x;
//	for(x=-sz ; x<=sz ; x++)
//		tmp[x+sz]=0;
//	for(x=-sz3 ; x<=sz3 ; x++)
//		sum+=tmp[x+sz]=exp(-x*x/(2*variance));
//	for(x=-sz ; x<=sz && sum ; x++)
//		tmp[x+sz]/=sum;
//
//	printf("\n\nsz=%d; sz3=%d\n",sz,sz3);
//	for(x=-sz ; x<=sz && sum ; x++)
//		printf("%1.3f ",tmp[x+sz]);
//	printf("\n");
//
//	//int nnz=0;
//	//for(x=1; x<=sz ; x++)
//	//	if(tmp[x]>MINVAL)
//	//		nnz++;
//	//std::vector<double> mask;
//	//for(int i=0;i<2*nnz+1;i++)
//	//	mask.push_back(0);
//	//for(x=-nnz; x<=nnz ; x++)
//	//	mask[x+nnz]=tmp[x+nnz];	
//
//	std::vector<double> mask;
//	for(x=-sz ; x<=sz && sum ; x++)
//		mask.push_back(tmp[x+sz]);
//
//	delete[] tmp;
//	return mask;
//}

int CVUtil::RGB2GRAY(IplImage* rgb, IplImage* gray)
{
  if(!rgb)
    return -1;
  if(!gray)
    return -1;
  if(rgb->nChannels<3)
    return -1;
  //todo: CV_BGR2GRAY or CV_RGB2GRAY
  cvCvtColor(rgb,gray,CV_BGR2GRAY);
  return 0;
}


void CVUtil::DrawCross(CvPoint* pt,int sz)
{
}

int CVUtil::GaussianSmooth(IplImage* src, IplImage* dst, double sigma2, SmoothingMethod method)
{
  //automatically set kernel size
  cvSmooth(src, dst, CV_GAUSSIAN, 0, 0, sqrt(sigma2));

  //3x3 7x7 kernels  are more efficient
  //cvSmooth(src, dst, CV_GAUSSIAN, 7, 7, sqrt(sigma2));

  return 0;
}

int CVUtil::GaussianSmooth(IplImage* src, IplImage* dst, CvArr* gker, SmoothingMethod method)
{
  cvSmooth(src, dst, CV_GAUSSIAN, 0, 0, 2.0); 
  //cvSmooth( src, dst, CV_BLUR, 15, 15, 0, 0 );

   /*//Computing 2D Convolution using DFT

   CvMat* A = cvCreateMat( M1, N1, CV_32F );
   CvMat* B = cvCreateMat( M2, N2, A->type );

   // it is also possible to have only abs(M2-M1)+1×abs(N2-N1)+1
   // part of the full convolution result
   CvMat* conv = cvCreateMat( A->rows + B->rows - 1, A->cols + B->cols - 1, A->type );

   // initialize A and B
   ...

   int dft_M = cvGetOptimalDFTSize( A->rows + B->rows - 1 );
   int dft_N = cvGetOptimalDFTSize( A->cols + B->cols - 1 );

   CvMat* dft_A = cvCreateMat( dft_M, dft_N, A->type );
   CvMat* dft_B = cvCreateMat( dft_M, dft_N, B->type );
   CvMat tmp;

   // copy A to dft_A and pad dft_A with zeros
   cvGetSubRect( dft_A, &tmp, cvRect(0,0,A->cols,A->rows));
   cvCopy( A, &tmp );
   cvGetSubRect( dft_A, &tmp, cvRect(A->cols,0,dft_A->cols - A->cols,A->rows));
   cvZero( &tmp );
   // no need to pad bottom part of dft_A with zeros because of
   // use nonzero_rows parameter in cvDFT() call below

   cvDFT( dft_A, dft_A, CV_DXT_FORWARD, A->rows );

   // repeat the same with the second array
   cvGetSubRect( dft_B, &tmp, cvRect(0,0,B->cols,B->rows));
   cvCopy( B, &tmp );
   cvGetSubRect( dft_B, &tmp, cvRect(B->cols,0,dft_B->cols - B->cols,B->rows));
   cvZero( &tmp );
   // no need to pad bottom part of dft_B with zeros because of
   // use nonzero_rows parameter in cvDFT() call below

   cvDFT( dft_B, dft_B, CV_DXT_FORWBRD, B->rows );

   cvMulSpectrums( dft_A, dft_B, dft_A, 0 );// or CV_DXT_MUL_CONJ to get correlation rather than convolution 

   cvDFT( dft_A, dft_A, CV_DXT_INV_SCALE, conv->rows ); // calculate only the top part
   cvGetSubRect( dft_A, &tmp, cvRect(0,0,conv->cols,conv->rows) );

   cvCopy( &tmp, conv );*/

   /*if(src==dst)
     cvReleaseImage(&dst2);*/

  return 0;
}



int CVUtil::ImageGradient(IplImage* src, IplImage* dX, IplImage* dY)
{
#if 0

#endif
  cvSobel( src, dX, 1, 0, 3 );
    cvSobel( src, dY, 0, 1, 3 );
  return 0;
}
