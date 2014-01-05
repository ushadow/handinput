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
  /// <summary>
  /// Hand tracking based on Kinect skeleton.
  /// </summary>
  public class SimpleSkeletonHandTracker : IHandTracker {
    static readonly ILog Log = LogManager.GetCurrentClassLogger();
    static readonly int CamShiftIter = 4;
    static readonly int DefaultZDist = 1; // m
    static readonly float HandWidth = 0.095f; // m

    // Hand bounding box in detph image.
    public Rectangle HandRect { get; private set; }
    public Rectangle InitialHandRect { get; private set; }
    public Image<Gray, Byte> SmoothedDepth { get; private set; }
    public Image<Gray, Byte> HandMask { get; private set; }

    int width, height;
    CoordinateConverter mapper;
    PlayerDetector playerDetector;
    MCvConnectedComp connectedComp = new MCvConnectedComp();
    MCvBox2D shiftedRect = new MCvBox2D();
    SkinDetector skinDetector;

    public SimpleSkeletonHandTracker(int width, int height, Byte[] kinectParams) {
      mapper = new CoordinateConverter(kinectParams, Parameters.ColorImageFormat,
                                    Parameters.DepthImageFormat);
      Init(width, height);
    }

    public SimpleSkeletonHandTracker(int width, int height, CoordinateMapper coordMapper) {
      mapper = new CoordinateConverter(coordMapper, Parameters.ColorImageFormat,
                                    Parameters.DepthImageFormat);
      Init(width, height);
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
        var rightHandDepthPos = mapper.MapSkeletonPointToDepthPoint(rightHandJoint.Position);
        z = rightHandJoint.Position.Z;
        InitialHandRect = ComputeInitialRect(rightHandDepthPos, z);

        playerDetector.UpdateMasks(depthFrame, cf, skeleton, InitialHandRect, true, true);
        var depthImage = playerDetector.DepthImage;
        CvInvoke.cvSmooth(depthImage.Ptr, SmoothedDepth.Ptr, SMOOTH_TYPE.CV_MEDIAN, 5, 5, 0, 0);
        FindBestBoundingBox(InitialHandRect);

        var relPos = SkeletonUtil.RelativePosToShoulder(HandRect, SmoothedDepth.Data, width, 
            height, skeleton, mapper);
        return new TrackingResult(new Some<Vector3D>(relPos), SmoothedDepth, 
                                  new Some<Rectangle>(HandRect), playerDetector.Skin,
                                  new Some<Rectangle>(mapper.MapDepthRectToColorRect(HandRect,
                                    depthFrame, width)));
      }
      return new TrackingResult();
    }

    void Init(int width, int height) {
      this.width = width;
      this.height = height;
      playerDetector = new PlayerDetector(width, height, mapper);
      SmoothedDepth = new Image<Gray, byte>(width, height);
      skinDetector = new SkinDetector(width, height);
    }

    /// <summary>
    /// Computes initial hand searching rectangle in the depth image.
    /// </summary>
    /// <returns></returns>
    Rectangle ComputeInitialRect(DepthImagePoint point, float z) {
      var scaledHandWidth = DepthUtil.GetDepthImageLength(width, HandWidth, z) * 2;
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
