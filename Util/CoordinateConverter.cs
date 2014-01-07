using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.ObjectModel;

using Microsoft.Kinect;
using System.Drawing;

namespace HandInput.Util {
  /// <summary>
  /// Maps points between color, depth and skeleton coordinates.
  /// </summary>
  public class CoordinateConverter {
    
    CoordinateMapper mapper;
    ColorImageFormat cif;
    DepthImageFormat dif;

    public CoordinateConverter(IEnumerable<byte> kinectParams, ColorImageFormat cif, 
                            DepthImageFormat dif) {
      mapper = new CoordinateMapper(kinectParams);
      this.cif = cif;
      this.dif = dif;
    }

    public CoordinateConverter(CoordinateMapper mapper, ColorImageFormat cif, DepthImageFormat dif) {
      this.mapper = mapper;
      this.cif = cif;
      this.dif = dif;
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

      return mapper.MapDepthPointToColorPoint(dif, dp, cif);
    }

    public DepthImagePoint MapSkeletonPointToDepthPoint(SkeletonPoint sp) {
      return mapper.MapSkeletonPointToDepthPoint(sp, dif);
    }

    public ColorImagePoint MapSkeletonPointToColorPoint(SkeletonPoint sp) {
      return mapper.MapSkeletonPointToColorPoint(sp, cif);
    }
    public SkeletonPoint MapDepthPointToSkeletonPoint(DepthImagePoint depthImagePoint) {
      return mapper.MapDepthPointToSkeletonPoint(dif, depthImagePoint);
    }

    public SkeletonPoint MapDepthPointToSkeletonPoint(int x, int y, int depth) {
      var depthPoint = new DepthImagePoint() {
        X = x,
        Y = y,
        Depth = depth
      };

      return MapDepthPointToSkeletonPoint(depthPoint);
    }

    public Rectangle MapDepthRectToColorRect(Rectangle depthRect, short[] depthPixel, int width, 
        int height) {
      var y = Clip(depthRect.Top, 0, height);
      var x = Clip(depthRect.Left, 0, width);
      int depth = DepthUtil.RawToDepth(depthPixel[y * width + x]);
      var cpUpperLeft = MapDepthPointToColorPoint(depthRect.X, depthRect.Y, depth);

      y = Clip(depthRect.Bottom, 0, height);
      x = Clip(depthRect.Right, 0, width);
      depth = DepthUtil.RawToDepth(depthPixel[y * width + x]);
      var cpBottomRight = MapDepthPointToColorPoint(depthRect.Right, depthRect.Bottom, depth);
      return new Rectangle(cpUpperLeft.X, cpUpperLeft.Y, cpBottomRight.X - cpUpperLeft.X,
          cpBottomRight.Y - cpUpperLeft.Y);
    }

    private int Clip(int v, int min, int max) {
      if (v < min)
        return min;
      if (v > max)
        return max;
      return v;
    }
  }
}