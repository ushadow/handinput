
#include "pcheader.h"

#include <cstdio>
#include <cassert>

#include "highgui.h"
#include "cvutil.h"
#include "harrisbuffer.h"
#include "msharrisbuffer.h"
#include "cmdline.h"
 

#ifdef USE_CVCAM
#include "cvcam.h"
#endif 

std::string outfile="";
std::string sourcename="";
CCmdLine cmdLine;
bool show=true;
bool Processing=false;
IplImage* frame = 0;
HarrisBuffer hb;
MultiScaleHarrisBuffer mshb;
CvCapture* capture = 0;  
char* win1="ST-Demo";
char* win2="Win2";
char* wincvcam="cvcam";
IplImage* vis  = NULL;
IplImage* vis2 = NULL;	
IplImage* vis3 = NULL;	
IplImage* camimg = NULL;	
IplImage* gray = NULL;		
IplImage* grayvis = NULL;	
double avg=0;
int ifr=0;
int TotalIPs=0;
int nIPs=0;
int frame_begin=0;
int frame_end=100000000;

const int camresx[]={80,160,320,400,640};
const int camresy[]={60,120,240,300,480};	
int resid=1;



void ConvertRealImage(IplImage* im,IplImage* gray8u,IplImage* rgb8u)
{
	cvNormalize(im,im,1,0,CV_MINMAX); 
	cvScale(im,gray8u,255,0);
	cvCvtColor(gray8u,rgb8u,CV_GRAY2BGR);
}

void CapProperties( CvCapture* capture)
{
	//char* fourcc  = (char*) cvGetCaptureProperty(capture, CV_CAP_PROP_FOURCC);
	int frameH    = (int) cvGetCaptureProperty(capture, CV_CAP_PROP_FRAME_HEIGHT);
	int frameW    = (int) cvGetCaptureProperty(capture, CV_CAP_PROP_FRAME_WIDTH);
	double fps    =  cvGetCaptureProperty(capture, CV_CAP_PROP_FPS);
	int numFrames = (int) cvGetCaptureProperty(capture,  CV_CAP_PROP_FRAME_COUNT);
	double curPos = cvGetCaptureProperty(capture,  CV_CAP_PROP_POS_MSEC);
	printf("%.1f  ",curPos);
	//printf("fourcc=%s",fourcc);
	printf("%d frames ",numFrames);
	printf("[%dx%d]", frameW, frameH); 
	printf(" fps=%.1f  ",fps);
	printf("\n");
 //CV_CAP_PROP_POS_AVI_RATIO 
// CV_CAP_PROP_POS_FRAMES
}


bool first=true;;

void dostuff(IplImage *frm)
{
	frame=frm;
    // Linux and Windows OpenCV implementations seems to differ
	// Flip input rfames upside down for win-version

#ifdef WIN32
    cvFlip(frame,NULL,0);
#endif

	if(first)
	{	
		first=false;
		
		if(!hb.Init(frm,outfile))
			exit(2);

		if(!mshb.Init(frm,outfile,sourcename,frame_begin))
			exit(2);

		gray = cvCreateImage(cvGetSize(frm), IPL_DEPTH_8U, 1);
	}

	// CVUtil::RGB2GRAY(frm,gray);
	if (frm->nChannels==1) cvCopy(frm,gray);
	else cvCvtColor(frm,gray,CV_RGB2GRAY);

	//CVUtil::DrawCircleFeature(frm,cvPoint(10,10),4.0);
	//cvShowImage(win2, frm );

	double t,ft;	
	t = (double)cvGetTickCount();Processing=true;
	//hb.ProcessFrame(gray);
	mshb.ProcessFrame(gray);
    t = (double)cvGetTickCount() - t;Processing=false;
	ifr++;
	ft=t/(cvGetTickFrequency()*1000.);
	avg=((ifr-1)* avg + ft)/ifr;
	//printf("%4d: ",ifr);
	//nIPs=hb.NumberOfDetectedIPs();TotalIPs+=nIPs;
	nIPs=mshb.NumberOfDetectedIPs();TotalIPs+=nIPs;
	if (ifr%20==0){
		printf("Fame: %5d - ",ifr);
		printf("IPs[this:%2d, total:%4d]",nIPs,TotalIPs);
		//printf(" - Perf: Time= %.1f  -  Avg Time=%.1f - Avg FPS=%.1f ", ft,avg, 1000/avg);
		printf(" - Perf: Avg FPS=%.1f ",1000/avg);
		printf("\n");
	}
}

void dovisstuff()
{
	if(!grayvis)  grayvis  = cvCreateImage(cvGetSize(mshb.getMSHBufferImage(0)), IPL_DEPTH_8U, 1);
	if(!vis)  vis  = cvCreateImage(cvGetSize(mshb.getMSHBufferImage(0)), IPL_DEPTH_8U, 3);
	if(!vis2) vis2 = cvCreateImage(cvGetSize(mshb.getMSHBufferImage(0)), IPL_DEPTH_8U, 3);
	if(!vis3) vis3 = cvCreateImage(cvGetSize(mshb.getMSHBufferImage(0)), IPL_DEPTH_8U, 3);
	//IplImage* realimg = cvCreateImage(cvGetSize(frame), IPL_DEPTH_64F, 1);

	//cvScale(gray,realimg,1.0);
	//cvCvtColor(gray,vis,CV_GRAY2BGR);
	//hb.DrawInterestPoints(vis);
	//mshb.DrawInterestPoints(vis);
	//cvShowImage(win1,vis);
	//ShowRealImage(win2, hb.getHBufferImage() );
	
	//ConvertRealImage(mshb.getMSHBufferImage(1),gray,vis2);
	//mshb.DrawInterestPoints(vis2);

	ConvertRealImage(mshb.getMSHBufferImage(0),grayvis,vis3);
	mshb.DrawInterestPoints(vis3);

	//CVUtil::cvShowManyImages(win1,3,vis,vis2,vis3);
	//CVUtil::cvShowManyImages(win1,2,vis3,vis2);
			
	// For some reason cvShowImage seems to x-flip the frame
	// after it has been gray-scale converted. Flip it back

	//cvFlip(vis3,NULL,0);
	cvShowImage(win2, vis3 );
	//cvWaitKey();
}
#ifdef USE_CVCAM
void grabframe(IplImage *frm)
{
	
	if(!Processing)
	{
		cvResize(frm,camimg);
		cvFlip( camimg, camimg,0);

		dostuff(camimg);
		if(show)
			dovisstuff();
	}
	else
	{
		printf("skiping frame \n");
	}

}

bool InitCVCAM(int c)
{
	printf("CamTest started ..\n");
	int cameras = cvcamGetCamerasCount();
	printf("Cameras detected: %d \n",cameras);

	if(c>=cameras)
		return false;
	int cameraSelected = -1;
	
	/*if(cameras>0)
		cameraSelected=0;*/
	if(c==-1)
	{
		int * out;
		int nselected = cvcamSelectCamera(&out);
		if(nselected>0)	cameraSelected = out[0];
	}
	else
		cameraSelected=c;

	if (cameraSelected > -1)
	{
		printf("The selected camera is camera number %d \n", cameraSelected);
		printf("Starting Camera %d \n",cameraSelected );
	// starting camera 1


	int h = 240;
	int w = 320;
	int t=0;
	cvcamSetProperty(cameraSelected,CVCAM_RNDWIDTH , &w);
	cvcamSetProperty(cameraSelected,CVCAM_RNDHEIGHT , &h);
	cvcamSetProperty(cameraSelected,CVCAM_PROP_ENABLE, &t);
	cvcamSetProperty(cameraSelected,CVCAM_PROP_RENDER, &t);
	//cvcamSetProperty(0,CVCAM_PROP_WINDOW, NULL);
	printf("It's working !!! \n");
	//Sleep(10000);
	//cvcamStop();
	//cvcamExit();
	//printf("Camera stopped. \n");

	}
	else 
	{
		printf("No Camera selected - terminating! \n");
		return false;
	}

	camimg=cvCreateImage(cvSize(camresx[resid],camresy[resid]), IPL_DEPTH_8U, 3);
	

	cvNamedWindow("cvcam", CV_WINDOW_AUTOSIZE);
	HWND hWnd = (HWND)cvGetWindowHandle(wincvcam);
	cvcamSetProperty(cameraSelected, CVCAM_PROP_WINDOW, &hWnd);
	cvMoveWindow(wincvcam,112,0);
	cvResizeWindow(wincvcam,320,240);
	cvcamSetProperty(cameraSelected, CVCAM_PROP_CALLBACK, grabframe);
	cvcamInit();
	cvcamStart();
	return true;
}
#else

bool InitCVCAM(int c)
{
	std::cout<<"the macro USE_CVCAM was not enabled in compile time,"<<std::endl;
	std::cout<<"can not use cvcam lib for accessing the camera,"<<std::endl;
	return false;
}
#endif //USE_CVCAM

bool InitCapture(const char* param)
{
    /*if( param!=NULL || (param==NULL && strlen(param) == 1 && isdigit(param[0])))
        capture = cvCaptureFromCAM( !param ? param[0] - '0' : CV_CAP_ANY );
	 
    else //if( argc == 2 )*/
        capture = cvCaptureFromFile( param ); 

    if( !capture )
    {
        fprintf(stderr,"Could not initialize capturing from %s...\n",param);
        return false;
    }

	// set frame offset
	if (frame_begin>0)
	  if (!cvSetCaptureProperty(capture,CV_CAP_PROP_POS_FRAMES,frame_begin))
	  {
	    printf("Could not start capture from frame %d in %s\n",frame_begin,param);
	    printf(" -> bugfix: fast-forward to the frame %d ...\n",frame_begin);
	    if(capture){
	      int fn=0;
	      while (fn<frame_begin){
		frame = cvQueryFrame( capture );
		if( !frame )
		  break;
		fn++;
	      }
	    }
	    printf("    done.\n");
	    //return false;
	  }
	 
	return true;
}

void ShowHelp()
{
	printf("\n");
	printf("This program detects space-time interest points at multiple\n");
	printf("space-time scales and computes corresponding descriptors.\n");
	printf("See README for the type of point detectors/descriptors\n");
	printf("that are currently implemented.\n");
	printf("\n");
	printf("Usage:\n");
	printf("\n");
	printf("Input/Output options:\n");
	printf("   -f   : input video file\n");
	printf("   -ff  : first frame index\n");
	printf("   -lf  : last frame index\n");
	printf("   -o   : file name for saving detected features\n");
	printf("   -cam : the camera number (starts from 0) \n");
	printf("          (if you don't specify any number, it shows a dialog for choosing a camera)\n");
	printf("   -res : camera resolution for processing \n");
	printf("          the following resolutions are available \n");
	printf("	    0 : 80  x 60 \n");			
	printf("	    1 : 160 x 120 (default) \n");			
	printf("	    2 : 320 x 240 \n");
	printf("	    3 : 400 x 300 \n");			
	printf("	    4 : 640 x 480 \n");			
	printf("\n");
	printf("Detection options:\n");
	printf("   -nplev : number of levels in spatial frame pyramid (default=%d)\n",mshb.nxplev);
	printf("            factor 2 subsampling is used; for each pyramid level\n");
	printf("            points are detected at four combinations of spatial\n");
	printf("            and temporal scales obtained by Gaussian smoothing \n");
	printf("            with spatial variance sigma2={%1.1f,%1.1f} and \n",mshb.sx2all[0],mshb.sx2all[1]);
	printf("            temporal variance tau2={%1.1f,%1.1f}\n",mshb.st2all[0],mshb.st2all[1]);
	printf("   -plev0 : initial level of spatial frame pyramid (default=%d)\n",mshb.initpyrlevel);
	printf("   -kparam: K parameter in Harris function (default=%.5f)\n",mshb.kparam);
	printf("   -thresh: threshold for ommiting weak points (default=%.3e)\n",mshb.SignificantPointThresh);
	printf("	    (to get all interest points set to zero)\n");
	printf("   -border: reject interest points within image boundary (default=%d)\n",mshb.Border);
	printf("\n");
	printf("Descriptor options:\n");
	printf("   -dscr  : type of descriptor [hoghof|hog|hof|hnf] (default=%s)\n",mshb.descriptortype.c_str());
	printf("   -szf   : factor used to compute descriptor patch size (default=%.1f)\n",mshb.patchsizefactor);
	printf("            patch size along spatial/temporal dimensions is defined as\n");
	printf("            size_dim=szf*2*sqrt(Gauss variance_dim)\n");
	printf("\n");
	printf("Other options:\n");
	printf("   -h    : shows this message\n");
	printf("   -vis  : [yes|no] visulization stuffs (default=%s)\n",show?"yes":"no");
	

}
int main( int argc, char** argv )
{

#ifdef USE_CVCAM
	bool cvcam=true;
#else 
	bool cvcam=false;
#endif

	
	if (cmdLine.SplitLine(argc, argv) < 1)
	{
      // no switches were given on the command line, abort
      //ShowHelp();
      //exit(-1);
	}

	std::string infile="../../../stip_online/video/walk-complex.avi";
	std::string tmp;
	int cam=-1;
	try
	{
		if( cmdLine.HasSwitch("-h") || 
			cmdLine.HasSwitch("-help") ||
			cmdLine.HasSwitch("--help"))
		{
			ShowHelp();
			exit(0);
		}

		if( cmdLine.HasSwitch("-f") && cmdLine.HasSwitch("-cam") )
		{
			std::cout<<"You can't specify both file and camera as input!"<<std::endl;
			ShowHelp();
			exit(-1);
		}

	

		if( !(cmdLine.HasSwitch("-f") || cmdLine.HasSwitch("-cam") ))
		{
			std::cout<<"no input..."<<std::endl;
			ShowHelp();
			exit(-1);
		}

		//*** input/output options
		if(cmdLine.GetArgumentCount("-f")>0) 
			infile = cmdLine.GetArgument("-f", 0);
		if(cmdLine.GetArgumentCount("-o")>0) 
			outfile = cmdLine.GetArgument("-o", 0);
	
		if(cmdLine.HasSwitch("-cam"))
			if(cmdLine.GetArgumentCount("-cam")>0)
				cam =  atoi(cmdLine.GetArgument("-cam", 0).c_str());
			else
				cam = -1;

		if(cmdLine.GetArgumentCount("-res")>0) resid =  atoi(cmdLine.GetArgument( "-res", 0 ).c_str());
		if(resid<0 || resid>4) resid=1;

		if(cmdLine.GetArgumentCount("-vis")>0) show = cmdLine.GetArgument("-vis", 0)=="yes"?true:false;
		
		//*** descriptor options
		if(cmdLine.GetArgumentCount("-dscr")>0) mshb.descriptortype = cmdLine.GetArgument("-dscr", 0);
		if(cmdLine.GetArgumentCount("-szf")>0) mshb.patchsizefactor = atof(cmdLine.GetArgument("-szf", 0).c_str());

		//*** detection options
		if(cmdLine.GetArgumentCount("-nplev")>0) mshb.nxplev=atoi(cmdLine.GetArgument("-nplev", 0).c_str());
		if(cmdLine.GetArgumentCount("-plev0")>0) mshb.initpyrlevel=atoi(cmdLine.GetArgument("-plev0", 0).c_str());
		//if(cmdLine.GetArgumentCount("-sigma")>0) hb.sig2 =  atof(cmdLine.GetArgument( "-sigma", 0 ).c_str());
		//if(cmdLine.GetArgumentCount("-tau")>0) hb.tau2 =  atof(cmdLine.GetArgument( "-tau", 0 ).c_str());
		if(cmdLine.GetArgumentCount("-kparam")>0) mshb.kparam =  atof(cmdLine.GetArgument( "-kparam", 0 ).c_str());
		if(cmdLine.GetArgumentCount("-thresh")>0) mshb.SignificantPointThresh =  atof(cmdLine.GetArgument( "-thresh", 0 ).c_str());
		if(cmdLine.GetArgumentCount("-border")>0) mshb.Border =  atoi(cmdLine.GetArgument( "-border", 0 ).c_str());

		//*** video capture options
		mshb.framemax = 100000000;
		if(cmdLine.GetArgumentCount("-framemax")>0) mshb.framemax =  atoi(cmdLine.GetArgument( "-framemax", 0 ).c_str());
		if(cmdLine.GetArgumentCount("-ff")>0) frame_begin =  atoi(cmdLine.GetArgument( "-ff", 0 ).c_str());
		if(cmdLine.GetArgumentCount("-lf")>0) frame_end =  atoi(cmdLine.GetArgument( "-lf", 0 ).c_str());
		
		
	}
	catch (...)
	{
		ShowHelp();
		exit(-1);
	}


	if(infile=="")  //prb:both can handle cam and file
	{
		if(!InitCVCAM(cam))
			return -2;
		// initialize source name string
        sourcename="CameraStream";
	} else {
		cvcam=false;
		if(!InitCapture(infile.c_str()))
			return -2;	
		// initialize source name string
        sourcename=infile;
		std::cout<<"Options summary: "<<std::endl;
		std::cout<<"  video input:     "<<sourcename<<std::endl;
		std::cout<<"  frame interval:  "<<frame_begin<<"-"<<frame_end<<std::endl;
		std::cout<<"  output file:     "<<outfile<<std::endl;
		std::cout<<"  #pyr.levels:     "<<mshb.nxplev<<std::endl;
		std::cout<<"  init.pyr.level:  "<<mshb.initpyrlevel<<std::endl;
		std::cout<<"  patch size fct.: "<<mshb.patchsizefactor<<std::endl;
		std::cout<<"  descriptor type: "<<mshb.descriptortype<<std::endl;
	}

		
        
	if(show)
	{
		//cvNamedWindow( win1,  CV_WINDOW_AUTOSIZE  );
		cvNamedWindow( win2, 0 );
	}


	//cvNamedWindow("Original");

	if(capture)
	{
    int fn=0;
    for(;;)
    {
        if (fn>=mshb.framemax) break;
        if (fn>=frame_end-frame_begin) break;

        fn++;
		frame = cvQueryFrame( capture );
			
        if( !frame )
            break;

		//CVUtil::DrawCircleFeature(frame,cvPoint(10,10),4.0);
		//CapProperties(capture);		
		//cvShowImage("Original",frame);

		dostuff(frame);
		if(show)
		{
			dovisstuff();
			//cvWaitKey();
			cvWaitKey(10);
			//if(cvWaitKey(10) >= 0 )
			//    break;
		}  
    }
	}
	std::cout<<"-> detected "<<TotalIPs<<" points"<<std::endl;

#ifdef USE_CVCAM
	if(cvcam)
	{
		cvWaitKey(0);
		cvcamExit();
	}
#endif
	
	if(capture)
		cvReleaseCapture( &capture );
	

	if(show)
	{
		cvDestroyWindow(win1);
	}

	if(gray) cvReleaseImage(&gray);
	if(vis)  cvReleaseImage(&vis);
	if(vis2)	cvReleaseImage(&vis2);
	if(vis3)	cvReleaseImage(&vis3);
	if(camimg) cvReleaseImage(&camimg);
    return 0;
}
