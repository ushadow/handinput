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
  public class SimpleSkeletonHandTracker : IHandTracker {
    static readonly ILog Log = LogManager.GetCurrentClassLogger();
    static readonly int CamShiftIter = 4;
    static readonly int DefaultZDist = 1; // m
    static readonly float HandWidth = 0.095f; // m

    // Hand bounding box in detph image.
    public Rectangle HandRect { get; private set; }
    public Image<Gray, Byte> SmoothedDepth { get; private set; }
    public Image<Gray, Byte> HandMask { get; private set; }

    int width, height;
    ColorDepthMapper mapper;
    PlayerDetector playerDetector;
    MCvConnectedComp connectedComp = new MCvConnectedComp();
    MCvBox2D shiftedRect = new MCvBox2D();
    SkinDetector skinDetector;

    public SimpleSkeletonHandTracker(int width, int height, Byte[] kinectParams) {
      Init(width, height);
      mapper = new ColorDepthMapper(kinectParams, Parameters.ColorImageFormat,
                                    Parameters.DepthImageFormat);
    }

    public SimpleSkeletonHandTracker(int width, int height, CoordinateMapper coordMapper) {
      Init(width, height);
      mapper = new ColorDepthMapper(coordMapper, Parameters.ColorImageFormat,
                                    Parameters.DepthImageFormat);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="depthFrame"></param>
    /// <param name="cf"></param>
    /// <param name="skeleton"></param>
    /// <returns>If skeleton is null, returns an empty result.</returns>
    public TrackingResult Update(short[] depthFrame, byte[] cf, Skeleton skeleton) {
      if (skeleton != null) {
        float z = DefaultZDist;
        var rightHandJoint = SkeletonUtil.GetJoint(skeleton, JointType.HandRight);
        var point = mapper.MapSkeletonPointToDepthPoint(rightHandJoint.Position);
        z = rightHandJoint.Position.Z;
        HandRect = ComputeInitialRect(point, z);

        playerDetector.FilterPlayer(depthFrame, cf, mapper);
        var depthImage = playerDetector.DepthImage;
        CvInvoke.cvSmooth(depthImage.Ptr, SmoothedDepth.Ptr, SMOOTH_TYPE.CV_MEDIAN, 5, 5, 0, 0);
        FindBestBoundingBox(HandRect);

        var relPos = SkeletonUtil.RelativePosToShoulder(HandRect, SmoothedDepth.Data, width, 
            height, skeleton, mapper);
        return new TrackingResult(new Some<Vector3D>(relPos), SmoothedDepth,
                                  new Some<Rectangle>(HandRect));
      }
      return new TrackingResult();
    }

    void Init(int width, int height) {
      this.width = width;
      this.height = height;
      playerDetector = new PlayerDetector(width, height);
      SmoothedDepth = new Image<Gray, byte>(width, height);
      skinDetector = new SkinDetector(width, height);
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    Rectangle ComputeInitialRect(DepthImagePoint point, float z) {
      var scaledHandWidth = DepthUtil.GetDepthImageLength(width, HandWidth, z);
      return new Rectangle((int)(point.X - scaledHandWidth / 2),
        (int)(point.Y - scaledHandWidth / 2), (int)(scaledHandWidth), (int)(scaledHandWidth));
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
