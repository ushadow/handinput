using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using windows = System.Windows;
using System.Windows.Media.Media3D;

using Microsoft.Kinect;
using System.Drawing;
using Emgu.CV.Structure;

namespace HandInput.Util {
  /// <summary>
  /// Utilitiy functions related to the Skeleton class in Microsoft Kinect.
  /// </summary>
  public static class SkeletonUtil {
    /// <summary>
    /// Finds the first tracked skeleton from all the skeletons. The possible 
    /// SkeletonTrackingStates are Tracked, PositionOnly and NonTracked.
    /// </summary>
    /// <param name="skeletons">An array of all skeletons.</param>
    /// <returns>The index of the first tracked skeleton.</returns>
    public static int FirstTrackedSkeletonIndex(Skeleton[] skeletons) {
      if (skeletons == null)
        return -1;

      for (int i = 0; i < skeletons.Length; i++) {
        if (skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
          return i;
      }

      return -1;
    }

    public static Skeleton FirstTrackedSkeleton(Skeleton[] skeletons) {
      var index = FirstTrackedSkeletonIndex(skeletons);
      if (index >= 0)
        return skeletons.ElementAt(index);
      return null;
    }

    public static Joint GetJoint(Skeleton s, JointType jointType) {
      return s.Joints.Where<Joint>(x => x.JointType.Equals(jointType)).First();
    }

    /// <summary>
    /// Computes the head circle in the depth image.
    /// </summary>
    /// <param name="skeleton"></param>
    /// <param name="mapper"></param>
    /// <returns></returns>
    public static FaceModel GetFaceModel(Skeleton skeleton, CoordinateConverter mapper) {
      var headJointPos = GetJoint(skeleton, JointType.Head).Position;
      var shoulderCenterJoint = GetJoint(skeleton, JointType.ShoulderCenter);
      var headDepthPt = mapper.MapSkeletonPointToDepthPoint(headJointPos);
      var shoulderDepthPt = mapper.MapSkeletonPointToDepthPoint(shoulderCenterJoint.Position);
      var radius = Math.Abs(shoulderDepthPt.Y - headDepthPt.Y) / 2;
      return new FaceModel(new Point(headDepthPt.X, headDepthPt.Y), radius, headJointPos.Z);
    }

    /// <summary>
    /// Average squared distance between the corresponding tracked joints of two skeletons.
    /// </summary>
    /// <param name="s1">One skeleton.</param>
    /// <param name="s2">Another skeleton.</param>
    /// <returns>Average squared distance between the corresponding tracked joints.</returns>
    public static float SkeletonDistance2(Skeleton s1, Skeleton s2) {
      float d2 = 0;
      int count = 0;
      foreach (Joint joint1 in s1.Joints) {
        if (joint1.TrackingState == JointTrackingState.Tracked) {
          var joint2 = s2.Joints[joint1.JointType];
          if (joint2.TrackingState == JointTrackingState.Tracked) {
            d2 += Distance2(joint1.Position, joint2.Position);
            count++;
          }
        }
      }

      return d2 / count;
    }

    public static float Distance(SkeletonPoint p1, SkeletonPoint p2) {
      return (float)Math.Sqrt(Distance2(p1, p2));
    }

    /// <summary>
    /// Creates a formated string for a SkeletonPoint.
    /// </summary>
    /// <param name="sp"></param>
    /// <returns></returns>
    public static String ToFormatedString(this SkeletonPoint sp) {
      return String.Format("({0}, {1}, {2})", sp.X, sp.Y, sp.Z);
    }

    /// <summary>
    /// Subtract the coordinate of sp2 from sp1.
    /// </summary>
    /// <param name="sp1"></param>
    /// <param name="sp2"></param>
    public static Vector3D Sub(SkeletonPoint sp1, SkeletonPoint sp2) {
      return new Vector3D(sp1.X - sp2.X, sp1.Y - sp2.Y, sp1.Z - sp2.Z);
    }

    public static Vector3D RelativePosToShoulder(Rectangle rect, byte[, ,] depthData, int width,
      int height, Skeleton skeleton, CoordinateConverter mapper) {
      var shoulderCenterJoint = SkeletonUtil.GetJoint(skeleton, JointType.ShoulderCenter);

      var aveDepth = 0.0;
      var count = 0;
      for (int y = rect.Top; y < rect.Top + rect.Height && y < height; y++)
        for (int x = rect.Left; x < rect.Left + rect.Width && x < width; x++) {
          if (x > 0 && y > 0) {
            aveDepth += depthData[y, x, 0];
            count++;
          }
        }

      var depth = PlayerDetector.ToWorldDepth(aveDepth / count);
      var center = rect.Center();
      var centerX = Math.Max(0, center.X);
      centerX = Math.Min(centerX, width);
      var centerY = Math.Max(0, center.Y);
      centerY = Math.Min(centerY, height);
      var salientPoint = mapper.MapDepthPointToSkeletonPoint((int)centerX, (int)centerY, depth);
      var relPos = SkeletonUtil.Sub(salientPoint, shoulderCenterJoint.Position);
      return relPos;
    }

    private static float Distance2(SkeletonPoint p1, SkeletonPoint p2) {
      return (p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y) + (p1.Z - p2.Z) *
          (p1.Z - p2.Z);
    }
  }
}