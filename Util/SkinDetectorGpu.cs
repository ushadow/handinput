using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Emgu.CV.GPU;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Drawing;

using Common.Logging;

namespace HandInput.Util {
  public class SkinDetectorGpu : ISkinDetector {
    [DllImport("GpuProcessor.dll", EntryPoint="FilterSkin")]
    public static extern void FilterSkin(IntPtr src, IntPtr dst);

    static readonly ILog Log = LogManager.GetCurrentClassLogger();

    static readonly Byte[,] StructElem = new Byte[,] {{1, 1, 1, 1, 1}, 
                                                      {1, 1, 1, 1, 1},
                                                      {1, 1, 1, 1, 1},
                                                      {1, 1, 1, 1, 1},
                                                      {1, 1, 1, 1, 1}};
    static readonly Matrix<Byte> Kernel = new Matrix<Byte>(StructElem); 
    
    GpuImage<Bgr, Byte> bgrImageGpu;
    GpuImage<Ycc, Byte> yccImageGpu;
    GpuImage<Gray, Byte> skinGpu, buffer1, buffer2;
    Image<Bgr, Byte> bgrImage;
    Image<Gray, Byte> skin;
    int width, height;

    public SkinDetectorGpu(int width, int height) {
      this.width = width;
      this.height = height;
      bgrImageGpu = new GpuImage<Bgr, Byte>(height, width);
      yccImageGpu = new GpuImage<Ycc, byte>(height, width);
      skinGpu = new GpuImage<Gray, byte>(height, width);
      buffer1 = new GpuImage<Gray, byte>(height, width);
      buffer2 = new GpuImage<Gray, byte>(height, width);
      bgrImage = new Image<Bgr, Byte>(width, height);
      skin = new Image<Gray, byte>(width, height);
    }

    public Image<Gray, Byte> DetectSkin(byte[] image) {
      ImageUtil.UpdateBgrImage(image, bgrImage.Data, width, height);
      bgrImageGpu.Upload(bgrImage);
      GpuInvoke.CvtColor(bgrImageGpu, yccImageGpu, Emgu.CV.CvEnum.COLOR_CONVERSION.CV_BGR2YCrCb, 
          IntPtr.Zero);
      FilterSkin(yccImageGpu.Ptr, skinGpu.Ptr);
      GpuInvoke.MorphologyEx(skinGpu, skinGpu, CV_MORPH_OP.CV_MOP_OPEN, Kernel.Ptr,
        buffer1, buffer2, new Point(2, 2), 1, IntPtr.Zero);
      skinGpu.Download(skin);
      return skin;
    }
  }
}
