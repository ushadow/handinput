using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Emgu.CV.Structure;
using Microsoft.Kinect;

namespace HandInput.Util {
  public class FaceModel {
    static readonly float FaceDistanceThresh = 50f; // in mm.
    /// <summary>
    /// Head center in the depth image.
    /// </summary>
    DepthImagePoint center;

    float radius;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="center">Head center in the depth image.</param>
    /// <param name="radius">Head radius in the depth image.</param>
    public FaceModel(DepthImagePoint center, float radius) {
      this.center = center;
      this.radius = radius;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="distance">Distance in mm.</param>
    /// <returns></returns>
    public bool IsPartOfFace(int x, int y, float distance) {
      var d2 = (x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y);
      return d2 < radius * radius && Math.Abs(distance - center.Depth) < FaceDistanceThresh;
    }
  }
}