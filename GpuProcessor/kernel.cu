#define _SIZE_T_DEFINED 
#ifndef __CUDACC__ 
#define __CUDACC__ 
#endif 
#ifndef __cplusplus 
#define __cplusplus 
#endif

#include "stdafx.h"

__global__ void SkinKernel(const uchar *src, uchar *dst, const int rows, const int cols,
                           const int srcStep, const int srcChannels, const int dstStep) {
  int r = blockIdx.y * blockDim.y + threadIdx.y;
  int c = blockIdx.x * blockDim.x + threadIdx.x;
  if (r >= rows || c >= cols) return;

  int iSrc = r * srcStep + c * srcChannels;
  int iDst = r * dstStep + c;

  int y = src[iSrc];
  int cr = src[iSrc + 1];
  int cb = src[iSrc + 2];

  cb -= 109;
  cr -= 152;
  int x1 = (819 * cr - 614 * cb) / 32 + 51;
  int y1 = (819 * cr + 614 * cb) / 32 + 77;
  x1 = x1 * 41 / 1024;
  y1 = y1 * 73 / 1024;
  int value = x1 * x1 + y1 * y1;
  if (y < 100)
    dst[iDst] = (value < 700) ? (unsigned char)255 : (unsigned char)0;
  else
    dst[iDst] = (value < 850) ? (unsigned char)255 : (unsigned char)0; 

}

extern "C" {

__declspec(dllexport) void __stdcall FilterSkin(void* src_, void* dst_) {
  using cv::gpu::GpuMat;

  GpuMat* src = reinterpret_cast<GpuMat*>(src_);
  GpuMat* dst = reinterpret_cast<GpuMat*>(dst_);

  dim3 dimBlock(20, 15);
  int rows = src->rows;
  int cols = src->cols;
  int srcStep = src->step1();
  int dstStep = dst->step1();
  int srcChannels = src->channels();
  
  // Find ceilings.
  dim3 dimGrid((cols + dimBlock.x - 1) / dimBlock.x, (rows + dimBlock.y - 1) / dimBlock.y);
  SkinKernel<<<dimGrid, dimBlock>>>(src->ptr(), dst->ptr(), rows, cols, srcStep, srcChannels,
                                    dstStep);
  cudaDeviceSynchronize();
}

}


