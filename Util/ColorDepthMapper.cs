using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.ObjectModel;

using Microsoft.Kinect;

namespace HandInput.Util {
  /// <summary>
  /// Maps points between color, depth and skeleton coordinates.
  /// </summary>
  public class ColorDepthMapper {
    
    CoordinateMapper mapper;
    ColorImageFormat cif;
    DepthImageFormat dif;

    public ColorDepthMapper(IEnumerable<byte> kinectParams, ColorImageFormat cif, 
                            DepthImageFormat dif) {
      mapper = new CoordinateMapper(kinectParams);
      this.cif = cif;
      this.dif = dif;
    }

    public ColorDepthMapper(CoordinateMapper mapper, ColorImageFormat cif, DepthImageFormat dif) {
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
  }
}