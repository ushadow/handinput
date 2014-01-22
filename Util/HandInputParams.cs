using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;
using System.Collections.ObjectModel;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace HandInput.Util {
  // Common parameters used by all modules.
  static public class HandInputParams {
    public static readonly int MaxDepth = 2000; // mm
    public static readonly ColorImageFormat ColorImageFormat =
        ColorImageFormat.RgbResolution640x480Fps30;
    public static readonly DepthImageFormat DepthImageFormat =
        DepthImageFormat.Resolution640x480Fps30;
    public static readonly int DepthWidth = 640;
    public static readonly int DepthHeight = 480;
    public static readonly int ColorWidth = 640;
    public static readonly int ColorHeight = 480;

    // Kinect out of range readings: 
    // too near: 0, too far: 0x0FFF (4095), unknown: 0x1FFF (8191) 
    public static int MinDepth = 800; // mm
    public static int FeatureImageWidth = 32;
    public static float ColorFocalLength = 531.15f;
    public static float DepthFocalLength = 571.26f;
    public static int SmoothWSize = 15;

    public static byte[] GetKinectParams(byte[] bytes) {
      var bf = new BinaryFormatter();
      var stream = new MemoryStream(bytes);
      IEnumerable<byte> kinectParams = bf.Deserialize(stream) as IEnumerable<byte>;
      stream.Close();
      return kinectParams.ToArray<byte>();
    }
  }
}