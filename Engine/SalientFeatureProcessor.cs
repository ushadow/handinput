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

    Vector3D prevRelPos;
    Vector3D prevVel;
    MFeatureProcessor featureProcessor = new MFeatureProcessor(FeatureImageWidth,
        FeatureImageWidth);
    bool visualize;

    public SalienceFeatureProcessor(bool visualize = false) {
      DescriptorLength = featureProcessor.HOGLength();
      FeatureLength = 3 * 3 + DescriptorLength;
      this.visualize = visualize;
    }

    public Option<Single[]> Compute(TrackingResult result) {
      Single[] feature = null;
      if (result.RelPos.IsSome && result.BoundingBox.IsSome) {
        if (prevRelPos != null) {
          var v = Vector3D.Subtract(result.RelPos.Value, prevRelPos);
          if (prevVel != null) {
            var acc = Vector3D.Subtract(v, prevVel);
            feature = new Single[FeatureLength];
            UpdateFeature(feature, result.RelPos.Value, 0);
            UpdateFeature(feature, v, 3);
            UpdateFeature(feature, acc, 6);
            var ptr = ComputeImageFeature(result.SmoothedDepth, result.BoundingBox.Value);
            Marshal.Copy(ptr, feature, 9, DescriptorLength);
          }
          prevVel = v;
        }
        prevRelPos = result.RelPos.Value;
      }
      if (feature == null)
        return new None<Single[]>();
      else return new Some<Single[]>(feature);
    }

    IntPtr ComputeImageFeature(Image<Gray, Byte> image, Rectangle bb) {
      image.ROI = bb;
      var ptr = featureProcessor.Compute(image.Ptr, visualize);
      image.ROI = Rectangle.Empty;
      return ptr;
    }

    void UpdateFeature(Single[] feature, Vector3D v, int startIndex) {
      feature[startIndex] = (Single)v.X;
      feature[startIndex + 1] = (Single)v.Y;
      feature[startIndex + 2] = (Single)v.Z;
    }
  }
}
