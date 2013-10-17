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
  public class SalienceFeatureProcessor {
    static readonly ILog Log = LogManager.GetCurrentClassLogger();
    static readonly int FeatureImageWidth = 64;

    public int FeatureLength { get; private set; }
    public int DescriptorLength { get; private set; }
    public bool Visualize { get; set; }

    MFeatureProcessor featureProcessor = new MFeatureProcessor(FeatureImageWidth,
        FeatureImageWidth);

    public SalienceFeatureProcessor() {
      DescriptorLength = featureProcessor.HOGLength();
      FeatureLength = 3 * 3 + DescriptorLength;
      Visualize = false;
    }

    /// <summary>
    /// Computes the feature vector from the tracking result.
    /// </summary>
    /// <param name="result"></param>
    /// <returns>An option of newly created Single array.</returns>
    public Option<Single[]> Compute(TrackingResult result) {
      Single[] feature = null;
      if (result.RelPos.IsSome && result.BoundingBox.IsSome) {
        var pos = result.RelPos.Value;
        var ptr = ComputeImageFeature(pos, result.SmoothedDepth, result.BoundingBox.Value);
        if (!ptr.Equals(IntPtr.Zero)) {
          feature = new Single[FeatureLength];
          Marshal.Copy(ptr, feature, 0, FeatureLength);
          return new Some<Single[]>(feature);
        }
      }
      return new None<Single[]>();
    }

    IntPtr ComputeImageFeature(Vector3D pos, Image<Gray, Byte> image, Rectangle bb) {
      image.ROI = bb;
      var ptr = featureProcessor.Compute((float)pos.X, (float)pos.Y, (float)pos.Z, image.Ptr,
                                          Visualize);
      image.ROI = Rectangle.Empty;
      return ptr;
    }
  }
}
