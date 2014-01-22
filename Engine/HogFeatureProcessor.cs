using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Windows.Media.Media3D;
using System.Drawing;

using Microsoft.Kinect;

using HandInput.Util;

using Common.Logging;

using Emgu.CV.GPU;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

using handinput;
using System.Runtime.InteropServices;

namespace HandInput.Engine {
  public class HogFeatureProcessor : IFeatureProcessor {
    static readonly ILog Log = LogManager.GetCurrentClassLogger();

    public int FeatureLength { get; private set; }
    public int DescriptorLength { get; private set; }
    public int MotionFeatureLength {
      get {
        return 9;
      }
    }
    public bool Visualize { get; set; }

    MFeatureProcessor featureProcessor;
    public HogFeatureProcessor(float sampleRate = 1) {
      featureProcessor = new MFeatureProcessor(HandInputParams.FeatureImageWidth,
          HandInputParams.FeatureImageWidth, 
          (int) Math.Round(HandInputParams.SmoothWSize / sampleRate));
      DescriptorLength = featureProcessor.HOGLength();
      FeatureLength = MotionFeatureLength + DescriptorLength;
      Visualize = false;
    }

    /// <summary>
    /// Creates a new feature vector from the tracking result.
    /// </summary>
    /// <param name="result"></param>
    /// <returns>An option of newly created Single array.</returns>
    public Option<Array> Compute(TrackingResult result) {
      Single[] feature = null;
      if (result.RelPos.IsSome && result.DepthBoundingBoxes.Count > 0) {
        var pos = result.RelPos.Value;
        var ptr = ComputeFeature(pos, result.DepthImage, result.DepthBoundingBoxes.Last(),
            result.ColorImage, result.ColorBoundingBoxes.Last());
        if (!ptr.Equals(IntPtr.Zero)) {
          feature = new Single[FeatureLength];
          Marshal.Copy(ptr, feature, 0, FeatureLength);
          return new Some<Array>(feature);
        }
      }
      return new None<Array>();
    }

    /// <summary>
    /// Computes raw feature including both the motion features and the descriptor.
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="image"></param>
    /// <param name="bb"></param>
    /// <returns>A pointer to a Single array.</returns>
    IntPtr ComputeFeature(Vector3D pos, Image<Gray, Byte> image, Rectangle bb,
        Image<Gray, Byte> colorImage, Rectangle colorBB) {
      image.ROI = bb;
      colorImage.ROI = colorBB;
      var ptr = featureProcessor.Compute((float)pos.X, (float)pos.Y, (float)pos.Z, image.Ptr,
                                          Visualize);
      image.ROI = Rectangle.Empty;
      colorImage.ROI = Rectangle.Empty;
      return ptr;
    }
  }
}
