using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;

using HandInput.Util;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using Common.Logging;
using System.Windows.Media.Media3D;
using System.Collections;

using handinput;

namespace HandInput.Engine {
  public class StipHandTracker : IHandTracker {
    static readonly ILog Log = LogManager.GetCurrentClassLogger();

    public ArrayList StipList { get; private set; }
    public Image<Gray, Byte> Gray { get; private set; }

    byte[, ,] imageStorage;
    int width, height;
    MHarrisBuffer buffer = new MHarrisBuffer();
    bool initialized = false;
    Image<Gray, Byte> smallGray;

    public StipHandTracker(int width, int height, Byte[] kinectParams) {
      Init(width, height);
    }

    public StipHandTracker(int width, int height, CoordinateMapper coordMapper) {
      Init(width, height);
    }

    public TrackingResult Update(short[] depthFrame, byte[] cf, Skeleton skeleton) {
      if (depthFrame != null) {
        ConvertColorImage(cf);
        if (initialized) {
          buffer.ProcessFrame(smallGray.Ptr);
          StipList = buffer.GetInterestPoints();
        } else {
          buffer.Init(smallGray.Ptr);
          initialized = true;
        }
      }
      return new TrackingResult() { RelPos = new None<Vector3D>() };
    }

    void Init(int width, int height) {
      this.width = width;
      this.height = height;
      imageStorage = new byte[height, width, 3];
      Gray = new Image<Gray, byte>(width, height);
      smallGray = new Image<Gray, byte>(width / 4, height / 4);
    }

    void ConvertColorImage(byte[] colorFrame) {
      var image = ImageUtil.CreateBgrImage(colorFrame, imageStorage, width, height);
      CvInvoke.cvCvtColor(image, Gray, COLOR_CONVERSION.CV_BGR2GRAY);
      CvInvoke.cvResize(Gray.Ptr, smallGray.Ptr, INTER.CV_INTER_LINEAR);
    }

    void ConvertDepthImage(short[] depthFrame) {
      var data = Gray.Data;
      var scale = (float)255 / Parameters.MaxDepth;
      for (int r = 0; r < height; r++)
        for (int c = 0; c < width; c++) {
          var index = r * width + c;
          var pixel = depthFrame[index];
          var depth = DepthUtil.RawToDepth(pixel);
          //depth = (depth < Parameters.MinDepth || depth > Parameters.MaxDepth) ?
          //        Parameters.MaxDepth : depth;
          data[r, c, 0] = (byte)((Parameters.MaxDepth - depth) * scale);
        }
    }
  }
}
