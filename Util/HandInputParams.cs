using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;
using System.Collections.ObjectModel;

namespace HandInput.Util {
  // Common parameters used by all modules.
  static public class HandInputParams {
    public static readonly int MaxDepth = 2000; // mm
    public static readonly int MinDepth = 800; // mm
    public static readonly ColorImageFormat ColorImageFormat =
        ColorImageFormat.RgbResolution640x480Fps30;
    public static readonly DepthImageFormat DepthImageFormat =
        DepthImageFormat.Resolution640x480Fps30;
    public static readonly int DepthWidth = 640;
    public static readonly int DepthHeight = 480;
    public static readonly int ColorWidth = 640;
    public static readonly int ColorHeight = 480;

    public static int FeatureImageWidth = 64;
    public static float ColorFocalLength = 531.15f;
    public static float DepthFocalLength = 571.26f;
  }
}