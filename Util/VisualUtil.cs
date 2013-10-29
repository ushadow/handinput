using System;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using drawing = System.Drawing;

namespace HandInput.Util {
  /// <summary>
  /// Utility functions for visualization.
  /// </summary>
  public class VisualUtil {

    /// <summary>
    /// Number of bits per pixel in a Bgr32 format bitmap.
    /// </summary>
    private static readonly int Bgr32BytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;
    /// <summary>
    /// Byte offset to the red byte of a Bgr32 pixel.
    /// </summary>
    private const int RedIndex = 2;

    /// <summary>
    /// Byte offset to the green byte of a Bgr32 pixel.
    /// </summary>
    private const int GreenIndex = 1;

    /// <summary>
    /// Byte offset to the blue byte of a Bgr32 pixel.
    /// </summary>
    private const int BlueIndex = 0;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="point">Point in the image coordinate.</param>
    /// <param name="s"></param>
    /// <param name="strokeThickness"></param>
    /// <param name="diameter"></param>
    /// <returns></returns>
    public static Ellipse DrawPoint(Canvas canvas, Point point, Brush s, int strokeThickness,
                                    int diameter) {
      var planeBoundary = new Ellipse();
      // Describes the brush's color using RGB values. 
      // Each value has a range of 0-255.


      planeBoundary.Fill = s;
      planeBoundary.StrokeThickness = strokeThickness;
      planeBoundary.Stroke = s;

      // Set the width and height of the Ellipse.
      planeBoundary.Width = diameter;
      planeBoundary.Height = diameter;

      Canvas.SetLeft(planeBoundary, point.X - (diameter / 2));
      Canvas.SetTop(planeBoundary, point.Y - (diameter / 2));
      canvas.Children.Add(planeBoundary);
      return planeBoundary;
    }

    public static Rectangle DrawRectangle(Canvas canvas, Point topLeft, Size size, Brush s) {
      var rect = new Rectangle();
      rect.Stroke = s;
      rect.StrokeThickness = 2;
      rect.Width = size.Width;
      rect.Height = size.Height;

      Canvas.SetLeft(rect, topLeft.X);
      Canvas.SetTop(rect, topLeft.Y);
      canvas.Children.Add(rect);
      return rect;
    }

    public static Rectangle DrawRectangle(Canvas canvas, drawing.Rectangle rect, Brush s) {
      var loc = rect.Location;
      var size = rect.Size;
      return DrawRectangle(canvas, new Point(loc.X, loc.Y), new Size(size.Width, size.Height), s);
    }

    public static Point ToImageCoordinate(Point p, double width, double height) {
      return new Point(p.X, height - p.Y);
    }

    public static void ColorPlayers(short[] depthFrame, byte[] colorFrame) {
      Array.Clear(colorFrame, 0, colorFrame.Length);
      for (int depthIndex = 0, colorIndex = 0; colorIndex < colorFrame.Length; depthIndex++,
           colorIndex += Bgr32BytesPerPixel) {
        short pixel = depthFrame[depthIndex];
        int player = DepthUtil.RawToPlayerIndex(pixel);

        if (player != 0) {
          colorFrame[colorIndex + RedIndex] = 0xff;
          colorFrame[colorIndex + GreenIndex] = 0;
          colorFrame[colorIndex + BlueIndex] = 0;
        }
      }
    }
  }
}
