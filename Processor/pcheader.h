/* precompiled header 
 include headers or project specific include files that are used frequently, but
 are changed infrequently
*/
#pragma once
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
#include <math.h>
#include <exception>
#include <stdexcept>

#include "cv.h"
#include "opencv2\core\core.hpp"
#include "opencv2\highgui\highgui.hpp"
#include "opencv2\imgproc\imgproc.hpp"

#include "mat.h"

#include <Eigen/Dense>

#include "svm.h"

#include "json_spirit.h"

