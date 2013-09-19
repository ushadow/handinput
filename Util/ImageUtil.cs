using System;
using System.Windows.Media;
using System.Windows;
using drawing = System.Drawing;
using Microsoft.Kinect;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.GPU;
using System.Threading.Tasks;

namespace HandInput.Util {
  /// <summary>
  /// Utility functions related to image manipulation.
  /// </summary>
  public static class ImageUtil {
    public const int BlueIndex = 0;
    public const int GreenIndex = 1;
    public const int RedIndex = 2;

    public static void ColorPixel(byte[] pixels, int index, Color color) {
      pixels[index + BlueIndex] = color.B;
      pixels[index + GreenIndex] = color.G;
      pixels[index + RedIndex] = color.R;
    }

    public static bool PixelHasColor(byte[] pixels, int index, Color color) {
      return pixels[index + BlueIndex] == color.B && pixels[index + GreenIndex] == color.G &&
          pixels[index + RedIndex] == color.R;
    }

    public static void UpdateBgrImage(byte[] src, Byte[, ,] dst, int width, int height) {
      for (int r = 0; r < height; r++)
        for (int c = 0; c < width; c++)
          for (int channel = 0; channel < 3; channel++) {
            dst[r, c, channel] = src[(r * width + c) * 4 + channel];
          }
    }

    /// <summary>
    /// Creates a Emgu CV Image from source image using a pre-allocated array.
    /// src.Length must be width * height * 4, and dst.Length must be width * height * 3.
    /// </summary>
    /// <param name="src"></param>
    /// <param name="dst"></param>
    /// <param name="width"></param>reinterpret_cast<IplImage*>(image.ToPointer())
    /// <param name="height"></param>
    /// <returns></returns>
    public static Image<Bgr, Byte> CreateBgrImage(byte[] src, Byte[, ,] dst, int width,
        int height) {
      for (int r = 0; r < height; r++)
        for (int c = 0; c < width; c++)
          for (int channel = 0; channel < 3; channel++) {
            dst[r, c, channel] = src[(r * width + c) * 4 + channel];
          }
      return new Image<Bgr, Byte>(dst);
    }

    public static Image<Bgr, Single> CreateCVImage(byte[] src, Single[, ,] dst, int width,
        int height) {
      for (int r = 0; r < height; r++)
        for (int c = 0; c < width; c++)
          for (int channel = 0; channel < 3; channel++) {
            dst[r, c, channel] = src[(r * width + c) * 4 + channel] / 255;
          }
      return new Image<Bgr, Single>(dst);
    }

    public static Image<Gray, Single> CreateCVImage(short[] src, Single[, ,] dst, int width,
        int height) {
      for (int r = 0; r < height; r++)
        for (int c = 0; c < width; c++) {
          dst[r, c, 0] = DepthUtil.RawToDepth(src[r * width + c]);
        }
      return new Image<Gray, Single>(dst);
    }

    /// <summary>
    /// Converts a point in the image coordinate (origin at top left) to a new point in the 
    /// virtual image coordinate (origin at bottom left).
    /// </summary>
    /// <param name="point">Point in the image coordinate.</param>
    /// <param name="size">Size of the image.</param>
    /// <returns>The new point in the virtual image coordinate.</returns>
    public static drawing.Point ToVirtualImageCoord(drawing.Point point, drawing.Size size) {
      return new drawing.Point(point.X, size.Height - point.Y);
    }

    /// <summary>
    /// Converts a point in the image coordinate (origin at top left) to a new point in the 
    /// virtual image coordinate (origin at bottom left).
    /// </summary>
    /// <param name="point"><c>System.Windows.Point</c> in the image coordinate.</param>
    /// <param name="size"><c>System.Windows.Size</c> of the image.</param>
    /// <returns>The new <c>Syste.Windows.Point</c> in the virtual image coordinate.</returns>
    public static drawing.Point ToVirtualImageCoord(DepthImagePoint point, drawing.Size size) {
      return new drawing.Point(point.X, (int)size.Height - point.Y);
    }

    public static drawing.PointF ToVirtualImageCoord(drawing.PointF point, drawing.Size size) {
      return new drawing.PointF(point.X, size.Height - point.Y);
    }

    /// <summary>
    /// Converts a point in the image coordinate (origin at top left) to a new point in the 
    /// virtual image coordinate (origin at bottom left).
    /// </summary>
    /// <param name="point"><c>System.Windows.Point</c> in the image coordinate.</param>
    /// <param name="size"><c>System.Windows.Size</c> of the image.</param>
    /// <returns>The new <c>Syste.Windows.Point</c> in the virtual image coordinate.</returns>
    public static drawing.Point ToVirtualImageCoord(ColorImagePoint point, drawing.Size size) {
      return new drawing.Point(point.X, (int)size.Height - point.Y);
    }

    public static drawing.Point ToImageCoord(drawing.Point point, drawing.Size size) {
      return new drawing.Point(point.X, size.Height - point.Y);
    }

    public static drawing.PointF ToImageCoord(drawing.PointF point, drawing.Size size) {
      return new drawing.PointF(point.X, size.Height - point.Y);
    }

    public static void CreateMask(Single[, ,] orig, Byte[] mask, int width, bool transparent = true) {
      var height = orig.Length / width;

      Array.Clear(mask, 0, mask.Length);
      int b = 0, g = 1, r = 2, a = 3;
      for (int i = 0; i < height; i++)
        for (int j = 0; j < width; j++) {
          var value = orig[i, j, 0];
          var baseNdx = (i * width + j) * 4;
          if (value != 0) {
            mask[baseNdx + a] = 255;
            mask[baseNdx + b] = 0;
            mask[baseNdx + g] = (Byte)((1 - value) * 255);
            mask[baseNdx + r] = (Byte)(value * 255);
          } else if (!transparent) {
            mask[baseNdx + a] = 255;
          }
        }
    }

    public static void CreateMask(byte[] orig, byte[] mask, bool transparent = true) {
      Array.Clear(mask, 0, mask.Length);
      int b = 0, g = 1, r = 2, a = 3;
      for (int i = 0; i < orig.Length; i++) {
        byte value = orig[i];
        if (value != 0) {
          mask[i * 4 + a] = 255;
          mask[i * 4 + b] = value;
          mask[i * 4 + g] = value;
          mask[i * 4 + r] = value;
        } else if (!transparent) {
          mask[i * 4 + a] = 255;
        }
      }
    }

    public static void AlignColorImage(Image<Gray, Byte> colorImg, Image<Gray, Byte> alignedImg,
        short[] depthFrame, ColorDepthMapper mapper) {
      CvInvoke.cvZero(alignedImg.Ptr);
      var data = colorImg.Data;
      var alignedData = alignedImg.Data;
      var width = colorImg.Width;
      var height = colorImg.Height;
      for (int r = 0; r < height; r++)
        for (int c = 0; c < width; c++) {
          var depthPixel = depthFrame[r * width + c];
          var depth = DepthUtil.RawToDepth(depthPixel);
          var cp = mapper.MapDepthPointToColorPoint(c, r, depth);
          if (cp.X >= 0 && cp.X < width && cp.Y >= 0 && cp.Y < height)
            alignedData[r, c, 0] = data[cp.Y, cp.X, 0];
        }
    }

    public static void CreateMask(byte[] orig, byte[] mask, int width, drawing.Rectangle rect,
                                  bool transparent) {
      Array.Clear(mask, 0, mask.Length);
      var height = orig.Length / width;
      int b = 0, g = 1, r = 2, a = 3;
      for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++) {
          var index = y * width + x;
          if (rect.Contains(x, y)) {
            byte value = orig[index];
            if (value != 0) {
              mask[index * 4 + a] = 255;
              mask[index * 4 + b] = 0;
              mask[index * 4 + g] = 0;
              mask[index * 4 + r] = value;
            }
          }
          if (!transparent) {
            mask[index * 4 + a] = 255;
          }
        }
    }

    public static void CreateTransparentMask(short[, ,] orig, byte[] transparent,
        int width, int height) {
      var max = 0;
      var min = Int16.MaxValue;

      for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++) {
          var value = orig[y, x, 0];
          if (value > 0) {
            max = Math.Max(max, value);
            min = Math.Min(min, value);
          }
        }

      var scale = 255f / (max - min);

      for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++) {
          short value = orig[y, x, 0];
          int bndx = (y * width + x) * 4;
          if (value == 0) {
            transparent[bndx + 3] = 0;
          } else {
            transparent[bndx + 3] = 255;
            transparent[bndx] = (byte)((max - value) * scale);
          }
        }
    }

    static public void ScaleImage<T>(Image<Gray, T> origImage, drawing.Rectangle rect, T[] image,
                                  int scaledWidth) where T : new() {
      Array.Clear(image, 0, image.Length);

      var top = Math.Max(0, rect.Top);
      top = Math.Min(top, origImage.Height);

      var left = Math.Max(0, rect.Left);
      left = Math.Min(left, origImage.Width);

      var width = rect.Width + rect.Left - left;
      var height = rect.Height + rect.Top - top;
      // Keeps aspect ratio.
      var scale = (float)scaledWidth / Math.Max(width, height);

      var data = origImage.Data;
      for (int y = top; y < top + height && y < origImage.Height; y++)
        for (int x = left; x < left + width && x < origImage.Width; x++) {
          var newX = (int)((x - left) * scale);
          var newY = (int)((y - top) * scale);
          if (newX >= 0 && newX < scaledWidth && newY >= 0 &&
              newY < scaledWidth) {
            image[newY * scaledWidth + newX] = data[y, x, 0];
          }
        }
    }

    static public void ScaleImage<T>(Image<Gray, T> origImage, T[] image,
                                     int scaledWidth) where T : new() {
      Array.Clear(image, 0, image.Length);

      var xscale = (float)scaledWidth / origImage.Width;
      var yscale = (float)scaledWidth / origImage.Height;
      var data = origImage.Data;
      for (int y = 0; y < origImage.Height; y++)
        for (int x = 0; x < origImage.Width; x++) {
          var newX = (int)(x * xscale);
          var newY = (int)(y * yscale);
          if (newX >= 0 && newX < scaledWidth && newY >= 0 &&
              newY < scaledWidth) {
            image[newY * scaledWidth + newX] = data[y, x, 0];
          }
        }
    }
  }
}