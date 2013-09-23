using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Windows.Media.Media3D;

using Microsoft.Kinect;

using HandInput.Util;

using Common.Logging;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;

namespace HandInput.Engine {
  class SalientFeatureProcessor {
    private static readonly ILog Log = LogManager.GetCurrentClassLogger();
    private static readonly int FeatureImageWidth = 64;

    private List<Byte[]> imageList = new List<Byte[]>();
    private Vector3D prevRelPos;
    private Vector3D prevVel;

    public void ProcessFeature(Option<Vector3D> relPos, Image<Gray, Byte> depthImage, 
        Rectangle bb) {

      if (relPos.IsSome) {
        if (prevRelPos != null) {
          var v = Vector3D.Subtract(relPos.Value, prevRelPos);
          if (prevVel != null) {
            var acc = Vector3D.Subtract(v, prevVel);
          }
          prevVel = v;
        }
        prevRelPos = relPos.Value;
      }
    }

    private void AddImageFeature(Image<Gray, Byte> depthImage, Rectangle bb) {
      byte[] image = new byte[FeatureImageWidth * FeatureImageWidth];
      ImageUtil.ScaleImage<Byte>(depthImage, bb, image, FeatureImageWidth);
      imageList.Add(image);
    }
  }
}
