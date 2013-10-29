using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;

namespace HandInput.Engine {
  static public class Parameters {
    public static int FeatureImageWidth {
      get {
        return featureImageWidth;
      }

      set {
        featureImageWidth = value;
      }
    }

    public static String ModelFile {
      get {
        return modelFile;
      }

      set {
        modelFile = value;
      }
    }

    public static readonly int MaxDepth = 2000; // mm
    public static readonly int MinDepth = 800; // mm
    public static readonly ColorImageFormat ColorImageFormat =
        ColorImageFormat.RgbResolution640x480Fps30;
    public static readonly DepthImageFormat DepthImageFormat =
        DepthImageFormat.Resolution640x480Fps30;

    // Default values;
    static int featureImageWidth = 64;
    static String modelFile = "G:\\salience\\model.mat";
  }
}
