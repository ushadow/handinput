using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

using Emgu.CV;
using Emgu.CV.Structure;

using HandInput.Util;
using System.Drawing;

namespace HandInput.Engine {
  public class TrackingResult {
    /// <summary>
    /// Relative postion of the right hand with respect to the center of the shoulder.
    /// X axis is rightward, Y axis is upward, Z axis is away from the camera.
    /// </summary>
    public Option<Vector3D> RelPos { get; private set; }
    public Image<Gray, Byte> SmoothedDepth { get; private set; }
    public Option<Rectangle> BoundingBox { get; private set; }

    public TrackingResult() {
      RelPos = new None<Vector3D>();
      BoundingBox = new None<Rectangle>();
    }

    public TrackingResult(Option<Vector3D> relPos, Image<Gray, Byte> smoothedDepth,
        Option<Rectangle> box) {
      RelPos = relPos;
      SmoothedDepth = smoothedDepth;
      BoundingBox = box;
    }
  }
}
