using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Collections;
using System.Drawing;

using Microsoft.Kinect;

using HandInput.Util;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using Common.Logging;

using handinput;

namespace HandInput.Engine {
  public class StipHandTracker : IHandTracker {
    static readonly ILog Log = LogManager.GetCurrentClassLogger();
    static readonly int ImageScale = 4;
    static readonly int CamShiftIter = 4;
    static readonly int DefaultZDist = 1; // m
    static readonly float HandWidth = 0.095f; // m

    public List<Point> InterestPoints { get; private set; }
    public Image<Gray, Byte> Gray { get; private set; }
    public Rectangle HandRect { get; private set; }
    public Image<Gray, Byte> SmoothedDepth { get; private set; }

    byte[, ,] imageStorage;
    int width, height;
    MHarrisBuffer buffer = new MHarrisBuffer();
    bool initialized = false;
    Image<Gray, Byte> smallGray;
    CoordinateConverter mapper;
    PlayerDetector playerDetector;
    MCvConnectedComp connectedComp = new MCvConnectedComp();
    MCvBox2D shiftedRect = new MCvBox2D();

    public StipHandTracker(int width, int height, Byte[] kinectParams) {
      mapper = new CoordinateConverter(kinectParams, HandInputParams.ColorImageFormat,
                                    HandInputParams.DepthImageFormat);
      Init(width, height);
    }

    public StipHandTracker(int width, int height, CoordinateMapper coordMapper) {
      mapper = new CoordinateConverter(coordMapper, HandInputParams.ColorImageFormat,
                                    HandInputParams.DepthImageFormat);
      Init(width, height);
    }

    public TrackingResult Update(short[] depthFrame, byte[] cf, Skeleton skeleton) {
      InterestPoints.Clear();

      ConvertDepthImage(depthFrame);
      if (!initialized) {
        buffer.Init(smallGray.Ptr);
        initialized = true;
      }
      buffer.ProcessFrame(smallGray.Ptr);
      var stipList = buffer.GetInterestPoints();
      foreach (Object p in stipList) {
        InterestPoints.Add(new Point(((Point)p).X * ImageScale, ((Point)p).Y * ImageScale));
      }

      float z = DefaultZDist;
      if (skeleton != null) {
        var rightHandJoint = SkeletonUtil.GetJoint(skeleton, JointType.HandRight);
        if (rightHandJoint.TrackingState == JointTrackingState.Tracked) {
          var point = mapper.MapSkeletonPointToColorPoint(rightHandJoint.Position);
          InterestPoints.Add(new Point(point.X, point.Y));
          z = rightHandJoint.Position.Z;
        }
      }

      HandRect = ComputeInitialRect(z);

      playerDetector.FilterPlayer(depthFrame, cf);
      var depthImage = playerDetector.DepthImage;
      CvInvoke.cvSmooth(depthImage.Ptr, SmoothedDepth.Ptr, SMOOTH_TYPE.CV_MEDIAN, 5, 5, 0, 0);

      FindBestBoundingBox(HandRect);

      var depthBBs = new List<Rectangle>();
      depthBBs.Add(HandRect);
      return new TrackingResult(new None<Vector3D>(), SmoothedDepth, depthBBs);
    }

    void Init(int width, int height) {
      this.width = width;
      this.height = height;
      playerDetector = new PlayerDetector(width, height, mapper);
      imageStorage = new byte[height, width, 3];
      Gray = new Image<Gray, byte>(width, height);
      smallGray = new Image<Gray, byte>(width / ImageScale, height / ImageScale);
      SmoothedDepth = new Image<Gray, byte>(width, height);
      InterestPoints = new List<Point>();
    }

    void ConvertColorImage(byte[] colorFrame) {
      var image = ImageUtil.CreateBgrImage(colorFrame, imageStorage, width, height);
      CvInvoke.cvCvtColor(image, Gray, COLOR_CONVERSION.CV_BGR2GRAY);
      CvInvoke.cvResize(Gray.Ptr, smallGray.Ptr, INTER.CV_INTER_LINEAR);
    }

    void ConvertDepthImage(short[] depthFrame) {
      var data = Gray.Data;
      var scale = (float)255 / HandInputParams.MaxDepth;
      for (int r = 0; r < height; r++)
        for (int c = 0; c < width; c++) {
          var index = r * width + c;
          var pixel = depthFrame[index];
          var depth = DepthUtil.RawToDepth(pixel);
          depth = (depth < HandInputParams.MinDepth || depth > HandInputParams.MaxDepth) ?
                  HandInputParams.MaxDepth : depth;
          data[r, c, 0] = (byte)((HandInputParams.MaxDepth - depth) * scale);
        }
      CvInvoke.cvResize(Gray.Ptr, smallGray.Ptr, INTER.CV_INTER_LINEAR);
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    Rectangle ComputeInitialRect(float z) {
      if (InterestPoints.Count > 0) {
        int x = 0, y = 0;
        foreach (Point ip in InterestPoints) {
          x += ip.X;
          y += ip.Y;
        }
        x /= InterestPoints.Count;
        y /= InterestPoints.Count;

        double xx = 0, yy = 0;
        foreach (Point ip in InterestPoints) {
          xx += (ip.X - x) * (ip.X - x);
          yy += (ip.Y - y) * (ip.Y - y);
        }
        xx /= InterestPoints.Count;
        yy /= InterestPoints.Count;
        xx = Math.Sqrt(xx);
        yy = Math.Sqrt(yy);
        var scaledHandWidth = DepthUtil.GetDepthImageLength(width, HandWidth, z);
        var rectWidth = (int) Math.Max(xx, scaledHandWidth);
        var rectHeight = (int) Math.Max(yy, scaledHandWidth);
        return new Rectangle(x - rectWidth / 2, y - rectHeight / 2, rectWidth, rectHeight);
      }
      return HandRect;
    }

    void FindBestBoundingBox(Rectangle initialRect) {
      if (!initialRect.IsEmpty) {
        CvInvoke.cvCamShift(SmoothedDepth.Ptr, initialRect, new MCvTermCriteria(CamShiftIter),
            out connectedComp, out shiftedRect);
        if (!connectedComp.rect.IsEmpty)
          HandRect = connectedComp.rect;
      }
    }
  }
}
