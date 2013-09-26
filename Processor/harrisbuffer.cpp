#include "pcheader.h"

#include "harrisbuffer.h"
#include "cvutil.h"

//std::ofstream logfile("log.txt");


const int LengthFeatures=34;//length of feature vector
const int SizeNeighb=125; //mask of 5x5x5 (vectorized)
//JET filter computed in MATLAB
double jet[LengthFeatures][SizeNeighb]={
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.5,0,0,0,0,0,0,0,0,0,0.5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.5,0,0.5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0.5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,-2,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0.25,0,-0.25,0,0,0,0,0,0,0,-0.25,0,0.25,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,-2,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0.25,0,0,0,0,0,0,0,0,0,-0.25,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.25,0,0,0,0,0,0,0,0,0,0.25,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0.25,0,-0.25,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.25,0,0.25,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.5,0,0,0,0,1,0,0,0,0,0,0,0,0,0,-1,0,0,0,0,0.5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.5,0,0.5,0,0,1,0,-1,0,0,-0.5,0,0.5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.5,1,-0.5,0,0,0,0,0,0,0,0.5,-1,0.5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.5,1,0,-1,0.5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.5,0,0,0,0,1,0,0,0,0,-0.5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0.5,0,0,0,0,-1,0,0,0,0,0.5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.125,0,0.125,0,0,0,0,0,0,0,0.125,0,-0.125,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0.125,0,-0.125,0,0,0,0,0,0,0,-0.125,0,0.125,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.5,1,-0.5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0.5,-1,0.5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.5,0,0,0,0,0,0,0,0,0,0.5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,-1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.5,0,0,0,0,0,0,0,0,0,0.5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.5,0,0.5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,-1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.5,0,0.5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,-0.5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0.5,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,-4,0,0,0,0,6,0,0,0,0,-4,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0.25,0,-0.25,0,0,-0.5,0,0.5,0,0,0,0,0,0,0,0.5,0,-0.5,0,0,-0.25,0,0.25,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,-2,1,0,0,-2,4,-2,0,0,1,-2,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0.25,-0.5,0,0.5,-0.25,0,0,0,0,0,-0.25,0.5,0,-0.5,0.25,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,-4,6,-4,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0.25,0,0,0,0,-0.5,0,0,0,0,0,0,0,0,0,0.5,0,0,0,0,-0.25,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.25,0,0,0,0,0.5,0,0,0,0,0,0,0,0,0,-0.5,0,0,0,0,0.25,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0.25,0,-0.25,0,0,-0.5,0,0.5,0,0,0.25,0,-0.25,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.25,0,0.25,0,0,0.5,0,-0.5,0,0,-0.25,0,0.25,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0.0625,0,-0.125,0,0.0625,0,0,0,0,0,-0.0625,0,0.125,0,-0.0625,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.0625,0,0.125,0,-0.0625,0,0,0,0,0,0.0625,0,-0.125,0,0.0625,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0.25,-0.5,0,0.5,-0.25,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.25,0.5,0,-0.5,0.25,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,-2,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-2,0,0,0,0,4,0,0,0,0,-2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,-2,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0.25,0,-0.25,0,0,0,0,0,0,0,-0.25,0,0.25,0,0,0,0,0,0,0,0,0,0,0,0,-0.5,0,0.5,0,0,0,0,0,0,0,0.5,0,-0.5,0,0,0,0,0,0,0,0,0,0,0,0,0.25,0,-0.25,0,0,0,0,0,0,0,-0.25,0,0.25,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,-2,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-2,4,-2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,-2,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0.25,0,0,0,0,0,0,0,0,0,-0.25,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.5,0,0,0,0,0,0,0,0,0,0.5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0.5,0,0,0,0,0,0,0,0,0,-0.5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.25,0,0,0,0,0,0,0,0,0,0.25,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0.25,0,-0.25,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.5,0,0.5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0.5,0,-0.5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-0.25,0,0.25,0,0,0,0,0,0,0,0,0,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,6,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0},
};


void LogMinMax(CvArr* mat,std::ostream& os)
{
    //cvNormalize(gray,frame,1,0,CV_MINMAX);
    double m, M;
    cvMinMaxLoc(mat, &m, &M, NULL, NULL, NULL);
    os<<m<<"\t"<<M<<std::endl;
}

HarrisBuffer::HarrisBuffer(void):kparam(5e-4),sig2(8.0),tau2(2.0),delay(0),SignificantPointThresh(1E-9),Border(5),patchsizefactor(9.0)
{
    iFrame=0;

    /*SpatialMaskSeparable=NULL;
    SpatialMask=NULL;
    TemporalMask1=NULL;
    TemporalMask2=NULL;*/
    
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
    L=Lt=Lx=Ly=OFx=OFy=NULL;
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

HarrisBuffer::~HarrisBuffer(void)
{ 
/*	if(SpatialMaskSeparable) cvReleaseMat(&SpatialMaskSeparable);
    if(SpatialMask) cvReleaseMat(&SpatialMask);
    if(TemporalMask1) cvReleaseMat(&TemporalMask1);
    if(TemporalMask2) cvReleaseMat(&TemporalMask2);*/

    
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

bool HarrisBuffer::Init(IplImage* firstfrm,std::string fname)
{
    SpatialMaskSeparable=CVUtil::GaussianMask1D(sig2);
    TemporalMask1=CVUtil::GaussianMask1D(tau2);
    TemporalMask2=CVUtil::GaussianMask1D(2*tau2);
    //TemporalMask2=CVUtil::GaussianMask1D(2*tau2,0,7);
    DerivMask.push_back(-0.5);DerivMask.push_back(0.0);DerivMask.push_back(0.5);

    int sz1=(int)TemporalMask1.size();
    int sz2=(int)TemporalMask2.size();

    if(SpatialMaskSeparable.size()<3)
    {
        std::cerr<<"Spacial smooting variance is too low"<<std::endl;
        return false;
    }

    if(sz1<3 || sz2<5)
    {
        std::cerr<<"Temporal smooting variance is too low"<<std::endl;
        return false;
    }

    // estimate delay in point detection (in frames)
    if(!delay)
        delay= (int)((sz1+sz2)/2.0) +2;

    //printf("Initializing HBuffer sx2:%1.1f, sx2:%1.1f; ",sig2,tau2);
    //printf("TemporalMask2.size: %d; delay=%d\n",TemporalMask2.size(),delay);

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

    
    //header= cvCreateImageHeader(cvGetSize(aframe),IMGTYPE ,1);
    //tmp=cvCreateData(header);
    //todo:difference between cvMat and IplImage
    //		CvMat *work=cvCreateMat(frame->width,frame->height,CV_64FC1);
    tmp= cvCreateImage(cvGetSize(firstfrm),IMGTYPE ,1);
    tmp1= cvCreateImage(cvGetSize(firstfrm),IMGTYPE ,1);
    tmp2= cvCreateImage(cvGetSize(firstfrm),IMGTYPE ,1);
    tmp3= cvCreateImage(cvGetSize(firstfrm),IMGTYPE ,1);
    tmp4= cvCreateImage(cvGetSize(firstfrm),IMGTYPE ,1);
    tmp5= cvCreateImage(cvGetSize(firstfrm),IMGTYPE ,1);
    tmp6= cvCreateImage(cvGetSize(firstfrm),IMGTYPE ,1);

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
    //OFx= cvCreateImage(cvGetSize(firstfrm),IMGTYPE ,1);
    //OFy= cvCreateImage(cvGetSize(firstfrm),IMGTYPE ,1);

        opticalFlowLastFrame=cvCreateImage(cvGetSize(firstfrm),IMGTYPE,1);
        opticalFlowNextFrame=cvCreateImage(cvGetSize(firstfrm),IMGTYPE,1);
        opticalFlowLastFrame8u=cvCreateImage(cvGetSize(firstfrm),IPL_DEPTH_8U,1);
        opticalFlowNextFrame8u=cvCreateImage(cvGetSize(firstfrm),IPL_DEPTH_8U,1);
    
    if(!fname.empty())
        FeatureFile.open(fname.c_str());


    //JetFilter=cvCreateMat( LengthFeatures, SizeNeighb, CV_64F );
    cvInitMatHeader(&JetFilter,LengthFeatures,SizeNeighb,CV_64FC1,jet);
    //Initilizing normalization vector for JET features
    normvec= cvCreateMat( LengthFeatures, 1, CV_64F );
    double sx1=sqrt(sig2);
    double st1=sqrt(tau2);
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



void HarrisBuffer::ProcessFrame(IplImage* frm, IplImage* OFx_precomp, IplImage* OFy_precomp)
{
    int i;
    if (!prevgray) cvCopy(frm,prevgray);
    else cvCopy(gray,prevgray);
    cvCopy(frm,gray);
    //gray=frm;
    //todo:scale depending on input type and IMGTYPE 
    //cvNormalize(gray,frame,1,0,CV_MINMAX);
    /*double m, M;
    cvMinMaxLoc(gray, &m, &M, NULL, NULL, NULL);*/
    
    //if (!prevframe) {
    //	cvScale(gray, prevframe, 1.0/255.0, 0.0);
    //	cvScale(gray, frame, 1.0/255.0, 0.0);
    //} else {
    //	cvCopy(frame, prevframe);
    //	cvScale(gray, frame, 1.0/255.0, 0.0);
    //}
    cvScale(gray, frame, 1.0/255.0, 0.0);
    
    /*double m, M;
    cvMinMaxLoc(frame, &m, &M, NULL, NULL, NULL); 
    std::cout<<m<<"\t"<<M<<endl;*/

    //Ross moved this 'till later
    original.Update(frame);

    //spatial filtering
    CVUtil::GaussianSmooth(frame,tmp,sig2,FFT);
    databuffer.Update(tmp);

    //temporal filtering
    int tstamp1=databuffer.TemporalConvolve(tmp1, TemporalMask1);
    convbuffer.Update(tmp1,tstamp1);

    int tstamp1d=convbuffer.TemporalConvolve(Lt,DerivMask);
    cvScale(Lt,Lt, sqrt(tau2) , 0);

    convbuffer.GetFrame(tstamp1d,L);
    CVUtil::ImageGradient(L,Lx,Ly);//prb: a possible scale
    cvScale(Lx,Lx, sqrt(sig2)*0.5 , 0);
    cvScale(Ly,Ly, sqrt(sig2)*0.5 , 0);

    //update second-moment matrix
    GaussianSmoothingMul(Lx,Lx, tmp1,2*sig2);
    cxxbuffer.Update(tmp1,tstamp1d);
    GaussianSmoothingMul(Lx,Ly, tmp1,2*sig2);
    cxybuffer.Update(tmp1,tstamp1d);
    GaussianSmoothingMul(Lx,Lt, tmp1,2*sig2);
    cxtbuffer.Update(tmp1,tstamp1d);
    GaussianSmoothingMul(Ly,Ly, tmp1,2*sig2);
    cyybuffer.Update(tmp1,tstamp1d);
    GaussianSmoothingMul(Ly,Lt, tmp1,2*sig2);
    cytbuffer.Update(tmp1,tstamp1d);
    GaussianSmoothingMul(Lt,Lt, tmp1,2*sig2);
    cttbuffer.Update(tmp1,tstamp1d);

    //update Harris buffer
    int tstamp2=0;
    tstamp2=cxxbuffer.TemporalConvolve(cxx, TemporalMask2);
    tstamp2=cxybuffer.TemporalConvolve(cxy, TemporalMask2);
    tstamp2=cxtbuffer.TemporalConvolve(cxt, TemporalMask2);
    tstamp2=cyybuffer.TemporalConvolve(cyy, TemporalMask2);
    tstamp2=cytbuffer.TemporalConvolve(cyt, TemporalMask2);
    tstamp2=cttbuffer.TemporalConvolve(ctt, TemporalMask2);

    // Estimate L&K optical flow from second moment matrix
    //OpticalFlowFromSMM();
    //OpticalFlowFromLK();
    OFx=OFx_precomp;
    OFy=OFy_precomp;

    // compute 3D extension of Harris function
    HarrisFunction(kparam, tmp);
    Hbuffer.Update(tmp,tstamp2);
    
    //LogMinMax(Hbuffer.Buffer,logfile);
    //databuffer.FrameIndices.print(std::cout);
    //databuffer.FrameIndices.print(logfile);
    //convbuffer.FrameIndices.print(logfile);
    //Hbuffer.FrameIndices.print(logfile);
    //std::cout<<iFrame<<std::endl;
    
    //*** update pixel labels
    //int xsz=frm->width, ysz=frm->height;
    //IplImage *timg=cvCreateImage(cvSize(xsz,ysz),IMGTYPE,1);
    //IplImage *wimg=cvCreateImage(cvSize(xsz,ysz),IMGTYPE,1);
    


    //PixelClassifyOpticalFlow();

    //timgbuffer.Update(timg,tstamp1d);
    //wimgbuffer.Update(wimg,tstamp1d);


    //cvReleaseImage(&timg);
    //cvReleaseImage(&wimg);

    //*** detect interest points
    DetectInterestPoints(Border);

    //Ross moved this here, added tstamp2
    //original.Update(frame,tstamp2);

    //bool tempFirst=true;
    //int firstTime;

    //*** compute point descriptors

    
    for(i=0;i<(int)ipList.size();i++) 
    { 
      if(!ipList[i].reject)
        
        {
            DetectedTrackingPoint dtp(ipList[i]);
            //pList.push_back(dtp);
            //Here I want to add the interest point to the list of interest-points-to-track		
            pointsToTrack.push_back(dtp);
            
        
        }
        
      }
      
    //here's where I do the tracking
    // go through pointsToTrack 
    // for everything that hasn't finished tracking
    // add it's most recent location to the list that the openCV KLT function will take
    // run the opencv KLT function
    // using the same "for" structure that added the points to the tracking list,
    //  go through those points, and either add the new tracked position to the point
    //  or, if tracking was lost, 
    //   remove the point from pointsToTrack, and add it to pointsFinishedTracking
    
    
    //first, allocate the prev_features structure
    
        CalculateVelocityHistories();
    

    iFrame++;
    return;
}

void HarrisBuffer::GaussianSmoothingMul(IplImage* im1, IplImage* im2, IplImage* dst, double var)
{
    cvMul(im1,im2,tmp4);
    CVUtil::GaussianSmooth(tmp4,dst,var,FFT);
}


void HarrisBuffer::HarrisFunction(double k, IplImage* dst)
{
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
        cvSub(tmp1,tmp2,dst);
}


void HarrisBuffer::OpticalFlowFromLK()
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
            p1[i*pxn+j]=cvPoint2D32f(j*subf,i*subf);
            p2[i*pxn+j]=cvPoint2D32f(j*subf,i*subf);
        }

    char *sts = new char[pxn*pyn];
    CvTermCriteria termination = cvTermCriteria(CV_TERMCRIT_ITER | CV_TERMCRIT_EPS, 100, 1e5);
    cvCalcOpticalFlowPyrLK(prevgray, gray, NULL, NULL, 

                           p1, p2, int(pxn*pyn), cvSize(int(10),int(10)),

                           3,sts,NULL,termination,CV_LKFLOW_INITIAL_GUESSES);

    
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



    //                         const CvPoint2D32f* prev_features, CvPoint2D32f* curr_features,

    //                         int count, CvSize win_size, int level, char* status,

    //                         float* track_error, CvTermCriteria criteria, int flags );
}

void HarrisBuffer::OpticalFlowFromSMM()
{
    // ref: Laptev et al. CVIU 2007, eq.(8)
        cvMul(cxx, cyy, tmp1);
        cvMul(cxy, cxy, tmp2);
        cvSub(tmp1,tmp2,tmp5);

        cvMul(cyy, cxt, tmp3);
        cvMul(cxy, cyt, tmp4);
        cvSub(tmp3,tmp4,tmp6);

        cvDiv(tmp6,tmp5,OFx);
        
        cvMul(cxx, cyt, tmp3);
        cvMul(cxy, cxt, tmp4);
        cvSub(tmp3,tmp4,tmp6);

        cvDiv(tmp6,tmp5,OFy);
}

IplImage* HarrisBuffer::getHBufferImage(int type)
{
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

void HarrisBuffer::WriteFeatures(InterestPoint &ip)
{
    assert(ip.features);
    double *data=(double*)ip.features->data.ptr;
    FeatureFile<<ip.x<<"\t"<<ip.y<<"\t"<<ip.t<<"\t"<<ip.val<<"\t";
    for(int i=0;i<34;i++)
        FeatureFile<<data[i]<<"\t";
    FeatureFile<<std::endl;
}

void HarrisBuffer::DetectInterestPoints(int border)
{
    ipList.clear();
    Hbuffer.FindLocalMaxima(ipList,true);
    CvMat *reg=cvCreateMat( SizeNeighb, 1, CV_64F );	
    
    //int sz1=(int)TemporalMask1.size();
    int sz2=(int)TemporalMask2.size();

    //remove border
    if(border<2)
        border=2; // interest points in the boundary should be remove to have a valid local 5x5x5 mask
                  // an alternative could be extending by 2 pixel in space dimensions	

    //select significant points which are not in the boundary
    for(int i=0;i<(int)ipList.size();i++){
        // set s-t scales
        ipList[i].sx2=sig2; ipList[i].st2=tau2;

        // set feature type : 5 for multi-scale Harris with this implementation
        ipList[i].ptype=5;

        if(ipList[i].x>=border && (ipList[i].x<frame->width-border) &&
            ipList[i].y>=border && (ipList[i].y<frame->height-border) && 
            ipList[i].val>SignificantPointThresh && 
            ipList[i].t>(double)sz2/2.0)
            ipList[i].reject=false;
    }
    
    
    //computing JET features around an interest point by 5x5x5 local mask
    
    cvReleaseMat(&reg);

    //check tstamp for any possible error

    //writing selected interest points to file
    //for(int i=0;i<(int)ipList.size();i++)
    //	if(!ipList[i].reject)
    //		WriteFeatures(ipList[i]);
}		

int HarrisBuffer::NumberOfDetectedIPs()
{
    //return ipList.size();
    int n=0;
    for(int i=0;i<(int)ipList.size();i++)
        if(!ipList[i].reject)
            n++;
    return n;
}

int HarrisBuffer::NumberOfDetectedTPs()
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

void HarrisBuffer::DrawInterestPoints(IplImage* im)
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



void HarrisBuffer::CalculateVelocityHistories()
{
    if (pointsToTrack.size()>0)
        {
            
            CvPoint2D32f* prev_features=(CvPoint2D32f*)malloc((int)pointsToTrack.size()*sizeof(CvPoint2D32f));
            CvPoint2D32f* curr_features=(CvPoint2D32f*)malloc((int)pointsToTrack.size()*sizeof(CvPoint2D32f));
            char * foundFeature=(char *)malloc((int)pointsToTrack.size()*sizeof(char));
    
            CvTermCriteria optical_flow_termination_criteria = cvTermCriteria( CV_TERMCRIT_ITER | CV_TERMCRIT_EPS, 20, .3 );
    
            int i=0;
            std::list<DetectedTrackingPoint>::iterator it;
            
            for(it=pointsToTrack.begin();it!=pointsToTrack.end(); ++it)
            {
                if ((*it).trajectory.size()==0)
                    (*it).trajectory.push_back(cvPoint2D32f((*it).x,(*it).y));
                prev_features[i]= (*it).trajectory.back();
                i++;
            }

            int tempFrameNum = pointsToTrack.begin()->t;
            tempFrameNum+=pointsToTrack.begin()->trajectory.size()-1;
            
            
            //opticalFlowLastFrame=cvCreateImage(cvGetSize(gray),IPL_DEPTH_8U,1);
            //opticalFlowNextFrame=cvCreateImage(cvGetSize(gray),IPL_DEPTH_8U,1);
            
            //printf("yo0\n");fflush(stdout);
            original.GetFrame(tempFrameNum,opticalFlowLastFrame);
            //printf("yo1\n");fflush(stdout);
            original.GetFrame(tempFrameNum+1,opticalFlowNextFrame);
            //printf("yo2\n");fflush(stdout);
            
            cvScale(opticalFlowLastFrame, opticalFlowLastFrame8u, 255.0, 0.0);
            cvScale(opticalFlowNextFrame, opticalFlowNextFrame8u, 255.0, 0.0);
            
            if (pointsToTrack.size()>0)
            {
                //printf("Tracking frame %d to %d\n",tempFrameNum,tempFrameNum+1);fflush(stdout);
                cvCalcOpticalFlowPyrLK(opticalFlowLastFrame8u,opticalFlowNextFrame8u,NULL,NULL,prev_features,curr_features,(int)pointsToTrack.size(),cvSize(3,3),0,foundFeature,NULL,optical_flow_termination_criteria,0);
                //printf("done Tracking frame %d to %d\n",tempFrameNum,tempFrameNum+1);fflush(stdout);
            }	
            
            //if(opticalFlowLastFrame) cvReleaseImage(&opticalFlowLastFrame);
            //if(opticalFlowNextFrame) cvReleaseImage(&opticalFlowNextFrame);
            
            i=0;
            
            for(it=pointsToTrack.begin();it!=pointsToTrack.end(); ++it)
            {
                if (foundFeature[i])
                    (*it).trajectory.push_back(cvPoint2D32f(curr_features[i].x,curr_features[i].y));
                i++;
            }
        
            i=0;
            
            for(it=pointsToTrack.begin();it!=pointsToTrack.end(); ++it)
            {
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

void HarrisBuffer::finishProcessing()
{
    int tempFrameNum;
    std::list<DetectedTrackingPoint>::iterator it;
    
    if (pointsToTrack.size()>0)
    {
        tempFrameNum= pointsToTrack.begin()->t;
        tempFrameNum+=pointsToTrack.begin()->trajectory.size();
    
        while (original.FrameIndices.find(tempFrameNum)!=-1)
        {
            CalculateVelocityHistories();
            tempFrameNum= pointsToTrack.begin()->t;
            tempFrameNum+=pointsToTrack.begin()->trajectory.size();
        }
    
        for(it=pointsToTrack.begin();it!=pointsToTrack.end(); ++it)
            {
                (*it).trackingFinished=true;
                pointsFinishedTracking.splice(pointsFinishedTracking.end(),pointsToTrack,it);
            }
    
    }
    
}

void HarrisBuffer::AccumulateHistogram(CvMat *tdata, CvMat *wdata, double *hist, int nbins, bool normflag)
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
