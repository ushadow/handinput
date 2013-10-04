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

namespace HandInput.Engine {
  public class SalientFeatureProcessor {
    private static readonly ILog Log = LogManager.GetCurrentClassLogger();
    private static readonly int FeatureImageWidth = 64;

    private Vector3D prevRelPos;
    private Vector3D prevVel;
    private MFeatureProcessor featureProcessor = new MFeatureProcessor(FeatureImageWidth, 
        FeatureImageWidth);
    private Image<Gray, Byte> scaled = new Image<Gray,byte>(FeatureImageWidth, FeatureImageWidth);

    public void Compute(Option<Vector3D> relPos, Image<Gray, Byte> depthImage, 
        Rectangle bb) {

      if (relPos.IsSome) {
        if (prevRelPos != null) {
          var v = Vector3D.Subtract(relPos.Value, prevRelPos);
          if (prevVel != null) {
            var acc = Vector3D.Subtract(v, prevVel);
            ComputeImageFeature(depthImage, bb);
          }
          prevVel = v;
        }
        prevRelPos = relPos.Value;
      }
    }

    public void ComputeImageFeature(Image<Gray, Byte> image, Rectangle bb) {
      image.ROI = bb;
      featureProcessor.Compute(image.Ptr);
      image.ROI = Rectangle.Empty;
    }
  }
}
