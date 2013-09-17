using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;

using Microsoft.Kinect;

namespace HandInput.Util {
  public static class DepthUtil {
    public const float CamerDepthNominalFocalLengthInPixels = 285.63f;

    public static readonly Size RefDepthImageSize = new Size(320, 240);

    public static int RawToDepth(short raw) {
      return ((ushort)raw) >> DepthImageFrame.PlayerIndexBitmaskWidth;
    }

    public static int RawToPlayerIndex(short raw) {
      return raw & DepthImageFrame.PlayerIndexBitmask;
    }

    public static Point GetDepthImagePoint(double renderWidth, double renderHeight,
                                           SkeletonPoint sp) {
      var p = new Point();
      var x = sp.X * CamerDepthNominalFocalLengthInPixels / sp.Z +
              RefDepthImageSize.Width / 2;
      p.X = (int) (renderWidth * x / RefDepthImageSize.Width);
      var y = -sp.Y * CamerDepthNominalFocalLengthInPixels / sp.Z +
              RefDepthImageSize.Height / 2;
      p.Y = (int) (renderHeight * y / RefDepthImageSize.Height);
      return p;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="renderWidth">in pixel</param>
    /// <param name="lengthWorld">in m</param>
    /// <param name="distWorld">in m</param>
    /// <returns></returns>
    public static double GetDepthImageLength(double renderWidth, float lengthWorld,
                                             float distWorld) {
      double lengthImage = lengthWorld * CamerDepthNominalFocalLengthInPixels * renderWidth /
                          (distWorld * RefDepthImageSize.Width);
      return lengthImage;
    }

  }
}