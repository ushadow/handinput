using System;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.GPU;
using System.Drawing;

namespace HandInput.Util {
  public class SkinDetector : ISkinDetector {
    public static readonly StructuringElementEx Rect6 = new StructuringElementEx(5, 5, 3, 3,
        Emgu.CV.CvEnum.CV_ELEMENT_SHAPE.CV_SHAPE_RECT);
    
    int width, height;

    /// <summary>
    /// Binary mask of skin. 255 for skin and 0 for non-skin pixels.
    /// </summary>
    Image<Gray, Byte> skin;
    Image<Ycc, Byte> ycc;
    Image<Bgr, Byte> bgrImage;
   
    public SkinDetector(int width, int height) {
      this.width = width;
      this.height = height;
      skin = new Image<Gray, Byte>(width, height);
      ycc = new Image<Ycc, Byte>(width, height);
      bgrImage = new Image<Bgr, Byte>(width, height);
    }

    public Image<Gray, Byte> DetectSkin(Byte[] img) {
      return DetectSkin(img, Rectangle.Empty);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="img"></param>
    /// <returns>A reference of the skin mask. There is only one image allocated for skin mask and
    /// the image is reused.</returns>
    public Image<Gray, Byte> DetectSkin(Byte[] img, Rectangle roi) {
      //Code adapted from here
      // http://blog.csdn.net/scyscyao/archive/2010/04/09/5468577.aspx
      // Look at this paper for reference (Chinese!!!!!)
      // http://www.chinamca.com/UploadFile/200642991948257.pdf
      ImageUtil.UpdateBgrImage(img, bgrImage.Data, width, height);
      CvInvoke.cvCvtColor(bgrImage, ycc, COLOR_CONVERSION.CV_BGR2YCrCb);
      skin.ROI = roi;

      int y, cr, cb, x1, y1, value;

      Byte[, ,] YCrCbData = ycc.Data;
      Byte[, ,] skinData = skin.Data;

      var roiWidth = width;
      var roiHeight = height;
      if (!roi.IsEmpty) {
        roiWidth = roi.Width;
        roiHeight = roi.Height;
      }
      for (int i = roi.Top; i < roi.Top + roiHeight; i++)
        for (int j = roi.Left; j <roi.Left + roiWidth; j++) {
          y = YCrCbData[i, j, 0];
          cr = YCrCbData[i, j, 1];
          cb = YCrCbData[i, j, 2];

          cb -= 109;
          cr -= 152;
          x1 = (819 * cr - 614 * cb) / 32 + 51;
          y1 = (819 * cr + 614 * cb) / 32 + 77;
          x1 = x1 * 41 / 1024;
          y1 = y1 * 73 / 1024;
          value = x1 * x1 + y1 * y1;
          if (y < 100)
            skinData[i, j, 0] = (value < 700) ? (Byte)255 : (Byte)0;
          else
            skinData[i, j, 0] = (value < 850) ? (Byte)255 : (Byte)0;

        }

      CvInvoke.cvMorphologyEx(skin.Ptr, skin.Ptr, IntPtr.Zero, Rect6.Ptr, CV_MORPH_OP.CV_MOP_OPEN,
                              1);

      return skin;
    }
  }
}
