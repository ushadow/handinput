using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Drawing;
using window = System.Windows;

using Emgu.CV;
using Emgu.CV.Structure;

using HandInput.Util;

namespace HandInput.Engine {
  public class TrackingResult {
    /// <summary>
    /// Relative postion of the right hand with respect to the center of the shoulder.
    /// X axis is rightward, Y axis is upward, Z axis is away from the camera.
    /// </summary>
    public Option<Vector3D> RightHandRelPos { get; private set; }
    /// <summary>
    /// Absolute position of the right hand in the depth frame.
    /// </summary>
    public Option<window.Point> RightHandAbsPos { get; private set; }
    public Image<Gray, Byte> DepthImage { get; private set; }
    // Can be null.
    public Image<Gray, Byte> ColorImage { get; private set; }
    public List<Rectangle> DepthBoundingBoxes { get; private set; }
    public List<Rectangle> ColorBoundingBoxes { get; private set; }

    public TrackingResult() {
      RightHandRelPos = new None<Vector3D>();
      RightHandAbsPos = new None<window.Point>();
      DepthBoundingBoxes = new List<Rectangle>();
      ColorBoundingBoxes = new List<Rectangle>();
    }

    public TrackingResult(Option<Vector3D> relPos, Image<Gray, Byte> smoothedDepth,
        List<Rectangle> depthBox, Image<Gray, Byte> skin = null, 
        List<Rectangle> colorBox = null) {
      RightHandRelPos = relPos;
      DepthImage = smoothedDepth;
      DepthBoundingBoxes = depthBox;

      if (DepthBoundingBoxes.Count > 0) {
        var rect = DepthBoundingBoxes.Last();
        RightHandAbsPos = new Some<window.Point>(rect.Center());
      } else {
        RightHandAbsPos = new None<window.Point>();
      }

      ColorImage = skin;

      if (colorBox == null)
        colorBox = new List<Rectangle>();
      ColorBoundingBoxes = colorBox;
    }
  }
}
