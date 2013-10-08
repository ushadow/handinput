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
  public struct TrackingResult {
    public Option<Vector3D> RelPos;
    public Image<Gray, Byte> SmoothedDepth;
    public Option<Rectangle> BoundingBox;
  }
}
