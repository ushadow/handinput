using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.ObjectModel;

using Microsoft.Kinect;

namespace HandInput.Util {
  public class ColorDepthMapper {
    private static readonly ColorImageFormat ColorFormat =
        ColorImageFormat.RgbResolution640x480Fps30;
    private static readonly DepthImageFormat DepthFormat = DepthImageFormat.Resolution640x480Fps30;
    private CoordinateMapper mapper;

    public ColorDepthMapper(int width, int height, IEnumerable<byte> kinectParams) {
      mapper = new CoordinateMapper(kinectParams);
    }

    /// <summary>
    /// Maps a point in the depth image to a point in the color image.
    /// </summary>
    /// <param name="x">x coordinate in the depth image.</param>
    /// <param name="y">y coordinate in the depth image.</param>
    /// <param name="depth">correspoinding depth at (x, y).</param>
    /// <returns></returns>
    public ColorImagePoint MapDepthPointToColorPoint(int x, int y, int depth) {
      var dp = new DepthImagePoint() {
        X = x,
        Y = y,
        Depth = depth
      };

      return mapper.MapDepthPointToColorPoint(DepthFormat, dp, ColorFormat);
    }

    public DepthImagePoint MapSkeletonPointToDepthPoint(SkeletonPoint sp) {
      return mapper.MapSkeletonPointToDepthPoint(sp, DepthFormat);
    }

    public SkeletonPoint MapDepthPointToSkeletonPoint(DepthImagePoint depthImagePoint) {
      return mapper.MapDepthPointToSkeletonPoint(DepthFormat, depthImagePoint);
    }

    public SkeletonPoint MapDepthPointToSkeletonPoint(int x, int y, int depth) {
      var depthPoint = new DepthImagePoint() {
        X = x,
        Y = y,
        Depth = depth
      };

      return MapDepthPointToSkeletonPoint(depthPoint);
    }
  }
}