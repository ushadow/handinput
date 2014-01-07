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
    static readonly float FaceDistanceThresh = 100f; // in mm.
    /// <summary>
    /// Head center in the depth image.
    /// </summary>
    public DepthImagePoint Center { get; private set; }

    public float Radius { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="center">Head center in the depth image.</param>
    /// <param name="radius">Head radius in the depth image.</param>
    public FaceModel(DepthImagePoint center, float radius) {
      this.Center = center;
      this.Radius = radius;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="distance">Distance in mm.</param>
    /// <returns></returns>
    public bool IsPartOfFace(int x, int y, float distance) {
      var d2 = (x - Center.X) * (x - Center.X) + (y - Center.Y) * (y - Center.Y);
      return d2 < Radius * Radius && Math.Abs(distance - Center.Depth) < FaceDistanceThresh;
    }
  }
}