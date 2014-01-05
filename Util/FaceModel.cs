using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Emgu.CV.Structure;

namespace HandInput.Util {
  public class FaceModel {
    static readonly float FaceDistanceThresh = 0.05f; // in m.
    /// <summary>
    /// Head center in the depth image.
    /// </summary>
    Point center;

    float radius;

    /// <summary>
    /// Distance of the head in m.
    /// </summary>
    float faceDistance;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="center">Head center in the depth image.</param>
    /// <param name="radius">Head radius in the depth image.</param>
    /// <param name="distance">Distance of the head in m.</param>
    public FaceModel(Point center, float radius, float distance) {
      this.center = center;
      this.radius = radius;
      this.faceDistance = distance;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="distance">Distance in m.</param>
    /// <returns></returns>
    public bool IsPartOfFace(int x, int y, float distance) {
      var d2 = (x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y);
      return d2 < radius * radius && Math.Abs(distance - faceDistance) < FaceDistanceThresh;
    }
  }
}