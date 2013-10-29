using System;
using System.Collections.Generic;
using System.Linq;
using windows = System.Windows;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using Microsoft.Kinect;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;

using HandInput.Util;
using System.Windows.Media.Media3D;

namespace HandInput.Engine {
  public class SkeletonHandTracker : IHandTracker {
    private class DistanceComparer : IComparer<Rectangle> {
      private Dictionary<Rectangle, double> costDict;

      /// <summary>
      /// 
      /// </summary>
      /// <param name="skeletonHand">Hand position from skeleton tracking in depth image 
      /// coordinate.</param>
      /// <param name="prevHand"></param>
      public DistanceComparer(Dictionary<Rectangle, double> costDict) {
        this.costDict = costDict;
      }

      int IComparer<Rectangle>.Compare(Rectangle o1, Rectangle o2) {
        double cost1, cost2;
        var ret1 = costDict.TryGetValue(o1, out cost1);
        var ret2 = costDict.TryGetValue(o2, out cost2);
        Debug.Assert(ret1);
        Debug.Assert(ret2);
        return cost1.CompareTo(cost2);
      }
    }
    public static readonly float HandWidth = 0.1f; // m

    private static readonly float TrackingFactor = 1, NontrackingFactor = 1f / 9;
    private static readonly StructuringElementEx Rect5 = new StructuringElementEx(5, 5, 3, 3,
       Emgu.CV.CvEnum.CV_ELEMENT_SHAPE.CV_SHAPE_RECT);


    private static readonly int MorphIter = 1;
    private static readonly int DefaultZDist = 1; // m

    public Image<Gray, Byte> HandMask { get; private set; }
    public Image<Gray, Byte> SkinMask { get; private set; }
    public windows.Point PrevHand { get; private set; }
    /// <summary>
    /// Scaled depth image for the detected hand.
    /// </summary>
    public Image<Gray, Byte> HandImage { get; private set; }
    /// <summary>
    /// Final box enclosing the hand.
    /// </summary>
    public MCvBox2D HandBox { get; private set; }
    public List<Rectangle> HandCandidates { get; private set; }

    private int width, height;
    private Image<Gray, Byte> alignedImg, playerMask;
    private IntPtr storage = CvInvoke.cvCreateMemStorage(0);
    private IntPtr contourPtr = new IntPtr();
    private ColorDepthMapper mapper;
    private SkinDetector skinDetetor;

    public SkeletonHandTracker(int width, int height, byte[] kinectParams) {
      this.width = width;
      this.height = height;
      playerMask = new Image<Gray, Byte>(width, height);
      HandMask = new Image<Gray, Byte>(width, height);
      alignedImg = new Image<Gray, Byte>(width, height);
      HandImage = new Image<Gray, Byte>(width, height);

      mapper = new ColorDepthMapper(kinectParams, Parameters.ColorImageFormat,
                                    Parameters.DepthImageFormat);
      skinDetetor = new SkinDetector(width, height);
      HandBox = new MCvBox2D();
      HandCandidates = new List<Rectangle>();
    }

    /// <summary>
    /// Detects the position of the hand nearest to the skeleton joint of the hand.
    /// </summary>
    /// <param name="depthData"></param>
    /// <returns></returns>
    public TrackingResult Update(short[] depthData, byte[] colorPixelData, Skeleton skeleton) {
      if (skeleton != null) {
        var skeHandJoint = SkeletonUtil.GetJoint(skeleton, JointType.HandRight);

        var playerMask = CreatePlayerImage(depthData);

        if (colorPixelData != null) {
          SkinMask = skinDetetor.DetectSkin(colorPixelData);
          AlignColorImage(depthData, SkinMask);
          CvInvoke.cvAnd(playerMask.Ptr, alignedImg.Ptr, HandMask.Ptr, IntPtr.Zero);
          CvInvoke.cvDilate(HandMask.Ptr, HandMask.Ptr, Rect5, MorphIter);
          CvInvoke.cvDilate(HandMask.Ptr, HandMask.Ptr, IntPtr.Zero, MorphIter);
        }

        FindContours(skeHandJoint);
        RankCandidates(HandCandidates, skeHandJoint, PrevHand, depthData);

        if (HandCandidates.Count > 0) {
          var shoulderCenterJoint = SkeletonUtil.GetJoint(skeleton, JointType.ShoulderCenter);
          var detectSkeHandJointPos = FindHand(depthData, HandCandidates.First());
          var relPos = SkeletonUtil.Sub(detectSkeHandJointPos, shoulderCenterJoint.Position);
          return new TrackingResult(new Some<Vector3D>(relPos), HandImage,
            new Some<Rectangle>(
              new Rectangle((int)(HandBox.center.X - HandBox.size.Width / 2),
              (int)(HandBox.center.Y - HandBox.size.Height / 2), (int)(HandBox.size.Width),
              (int)HandBox.size.Height)));
        }
      }

      return new TrackingResult();
    }

    private Image<Gray, Byte> CreatePlayerImage(short[] depthFrame) {
      CvInvoke.cvZero(playerMask.Ptr);
      var data = playerMask.Data;

      for (int r = 0; r < height; r++)
        for (int c = 0; c < width; c++) {
          var index = r * width + c;
          short pixel = depthFrame[index];
          int playerIndex = DepthUtil.RawToPlayerIndex(pixel);
          if (playerIndex > 0)
            data[r, c, 0] = 255;
        }

      return playerMask;
    }

    /// <summary>
    /// Finds the contours with minimum perimeters.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="hand">Can be null.</param>
    private void FindContours(Joint hand) {
      // Non-zero pixels are treated as 1s. Source image content is modifield.
      CvInvoke.cvFindContours(HandMask.Ptr, storage, ref contourPtr, StructSize.MCvContour,
        RETR_TYPE.CV_RETR_EXTERNAL, CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, new Point(0, 0));
      var contour = new Seq<Point>(contourPtr, null);

      float z = DefaultZDist;
      if (hand != null)
        z = hand.Position.Z;

      HandCandidates.Clear();
      double perimThresh = DepthUtil.GetDepthImageLength(width, HandWidth, z) * 2;
      for (; contour != null && contour.Ptr.ToInt32() != 0; contour = contour.HNext) {
        var perim = CvInvoke.cvContourPerimeter(contour.Ptr);
        if (perim > perimThresh) {
          HandMask.Draw(contour, new Gray(255), -1);
          HandCandidates.Add(contour.BoundingRectangle);
        }
      }
    }

    private void AlignColorImage(short[] depthFrame, Image<Gray, Byte> colorImage) {
      CvInvoke.cvZero(alignedImg.Ptr);

      var data = colorImage.Data;
      var alignedData = alignedImg.Data;
      for (int r = 0; r < height; r++)
        for (int c = 0; c < width; c++) {
          var depthPixel = depthFrame[r * width + c];
          var depth = DepthUtil.RawToDepth(depthPixel);
          var cp = mapper.MapDepthPointToColorPoint(c, r, depth);
          if (cp.X >= 0 && cp.X < width && cp.Y >= 0 && cp.Y < height)
            alignedData[r, c, 0] = data[cp.Y, cp.X, 0];
        }
      CvInvoke.cvMorphologyEx(alignedImg.Ptr, alignedImg.Ptr, IntPtr.Zero, IntPtr.Zero,
                              CV_MORPH_OP.CV_MOP_CLOSE, 1);
    }

    private SkeletonPoint FindHand(short[] depthData, Rectangle rect) {
      CvInvoke.cvZero(HandImage.Ptr);
      var handImageData = HandImage.Data;
      var handMaskData = HandMask.Data;
      var playerMaskData = playerMask.Data;

      var maxDepth = 0;
      var minDepth = Int32.MaxValue;
      for (int y = rect.Top; y < rect.Top + rect.Height && y < height; y++)
        for (int x = rect.Left; x < rect.Left + rect.Width && x < width; x++) {
          if (y > 0 && x > 0 && handMaskData[y, x, 0] > 0 &&
              playerMaskData[y, x, 0] > 0) {
            var depth = DepthUtil.RawToDepth(depthData[y * width + x]);
            maxDepth = Math.Max(maxDepth, depth);
            if (depth < minDepth)
              minDepth = depth;
          }
        }

      var scale = (float)255 / (maxDepth - minDepth);
      for (int y = rect.Top; y < rect.Top + rect.Height && y < height; y++)
        for (int x = rect.Left; x < rect.Left + rect.Width && x < width; x++) {
          if (y > 0 && x > 0 && playerMaskData[y, x, 0] > 0 &&
              handMaskData[y, x, 0] > 0) {
            var depth = DepthUtil.RawToDepth(depthData[y * width + x]);
            handImageData[y, x, 0] = (byte)((maxDepth - depth) * scale);
          }
        }

      var connectedComp = new MCvConnectedComp();
      var shiftedBox = new MCvBox2D();
      CvInvoke.cvCamShift(HandImage.Ptr, rect, new MCvTermCriteria(0.0), out connectedComp,
                          out shiftedBox);

      PrevHand = new windows.Point(HandBox.center.X, HandBox.center.Y);
      HandBox = shiftedBox;
      var newRect = shiftedBox.MinAreaRect();
      var aveDepth = 0.0;
      var count = 0;
      for (int y = newRect.Top; y < newRect.Top + newRect.Height && y < height; y++)
        for (int x = newRect.Left; x < newRect.Left + newRect.Width && x < width; x++) {
          if (x > 0 && y > 0 && playerMaskData[y, x, 0] > 0 &&
              handMaskData[y, x, 0] > 0) {
            var depth = DepthUtil.RawToDepth(depthData[y * width + x]);
            aveDepth += depth;
            count++;
          }
        }

      aveDepth /= count;
      var shiftedCenterX = Math.Max(0, shiftedBox.center.X);
      shiftedCenterX = Math.Min(shiftedCenterX, width);
      var shiftedCenterY = Math.Max(0, shiftedBox.center.Y);
      shiftedCenterY = Math.Min(shiftedCenterY, height);
      return mapper.MapDepthPointToSkeletonPoint((int)shiftedCenterX,
          (int)shiftedCenterY, (int)aveDepth);
    }

    private void RankCandidates(List<Rectangle> contours, Joint skeHandJoint,
        windows.Point prevHand, short[] depthData) {
      var costDict = new Dictionary<Rectangle, double>();
      var skeHandPoint = mapper.MapSkeletonPointToDepthPoint(skeHandJoint.Position);
      var skeDistFactor = skeHandJoint.TrackingState == JointTrackingState.Tracked ?
                          TrackingFactor : NontrackingFactor;
      foreach (var contour in contours) {
        var cost = CostMetric(contour, skeHandPoint, prevHand, depthData, skeDistFactor);
        costDict.Add(contour, cost);
      }
      contours.Sort(new DistanceComparer(costDict));
    }

    private double CostMetric(Rectangle rect, DepthImagePoint skeHand, windows.Point prevHand,
          short[] depthData, float skeDistFactor) {
      var center = rect.Center();
      var cost = GeometryUtil.Distance2(center.X, center.Y, skeHand.X, skeHand.Y) * skeDistFactor;
      if (prevHand != null && (prevHand.X != 0 || prevHand.Y != 0)) {
        cost += GeometryUtil.Distance2(center.X, center.Y, prevHand.X, prevHand.Y);
      }
      return cost;
    }
  }
}
