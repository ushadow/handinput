/* precompiled header 
 include headers or project specific include files that are used frequently, but
 are changed infrequently
*/

#define _HAS_ITERATOR_DEBUGGING 0

#ifdef PROCESSOR_EXPORTS 
# define PROCESSOR_API __declspec(dllexport)
#else
# define PROCESSOR_API __declspec(dllimport)
#endif

#include <cmath>
#include <cstdarg>
#include <cstdio>
#include <cassert>

#include <string>
#include <iostream>
#include <fstream>
#include <vector>
#include <algorithm>
#include <functional>
#include <memory>

#include "cv.h"
#include "highgui.h"
#include "opencv2\objdetect\objdetect.hpp"

#include "mat.h"

#include <Eigen/Dense>


