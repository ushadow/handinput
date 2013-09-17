using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using windows = System.Windows;
using System.Windows.Media.Media3D;

using Microsoft.Kinect;

using Emgu.CV.Structure;

namespace HandInput.Util {
  public static class GeometryUtil {
    const double Eps = 1e-9;

    public static double Slope(Point p1, Point p2) {
      if (p2.X != p1.X) {
        return (double)(p2.Y - p1.Y) / (p2.X - p1.X);
      } else {
        return double.MaxValue;
      }
    }

    /// <summary>
    /// Returns the angle in degree of the gradient of the line defined by two points.
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <returns>The angle is between -90 and 90 degrees.</returns>
    public static float GradientInDegree(Point p1, Point p2) {
      float theta = (float)(Math.Atan2(p2.Y - p1.Y, p2.X - p1.X) * 180 / Math.PI);
      if (theta > 90)
        theta -= 180;
      else if (theta < -90)
        theta += 180;
      return theta;
    }

    public static Point Midpoint(Point p1, Point p2) {
      return new Point(p1.X + (p2.X - p1.X) / 2,
                       p1.Y + (p2.Y - p1.Y) / 2);
    }

    public static PointF Midpoint(PointF p1, PointF p2) {
      return new PointF(p1.X + (p2.X - p1.X) / 2,
                       p1.Y + (p2.Y - p1.Y) / 2);
    }

    public static double Distance2(double x1, double y1, double x2, double y2) {
      return (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2);
    }

    /// <summary>
    /// Returns distance between p1 and p2.
    /// </summary>
    public static double Distance(Point p1, Point p2) {
      return Math.Sqrt(Distance2(p1, p2));
    }

    /// <summary>
    /// Returns distance between p1 and p2.
    /// </summary>
    public static double Distance(PointF p1, PointF p2) {
      return Math.Sqrt(Distance2(p1, p2));
    }

    /// <summary>
    /// Squared Euclidean distance of two 2D points.
    /// </summary>
    /// <param name="p1">Point 1.</param>
    /// <param name="p2">Point 2.</param>
    /// <returns>Squared Euclean distance of p1 and p2.</returns>
    public static float Distance2(Point p1, Point p2) {
      return (p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y);
    }

    /// <summary>
    /// Squared Euclidean distance of two 2D points.
    /// </summary>
    /// <param name="p1">Point 1.</param>
    /// <param name="p2">Point 2.</param>
    /// <returns>Squared Euclean distance of p1 and p2.</returns>
    public static float Distance2(PointF p1, PointF p2) {
      return (p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y);
    }

    public static PointF ClosestPointToLine(PointF a, PointF b, PointF p) {
      var d = Math.Abs(LinePointDistance(a, b, p));
      if (d < Eps)
        return p;
      var v = Perp(Vector(a, b));
      var p2 = new PointF(p.X + (float)v.X, p.Y + (float)v.Y);
      return LineIntersection(a, b, p, p2);
    }

    /// <summary>
    /// Returns a new vector which is perpendicular to v.
    /// </summary>
    /// <param name="v">The original vector.</param>
    /// <returns>A new vector which is perpendicular to the input vector.</returns>
    public static windows.Vector Perp(windows.Vector v) {
      return new windows.Vector(-v.Y, v.X);
    }

    /// <summary>
    /// Calculates the signed perpendicular distance from a point to a line. Point on the right
    /// side of the vector (P1 -> P2) will be NEGATIVE.
    /// </summary>
    /// <param name="p1">One end of the line.</param>
    /// <param name="p2">The other end of the line.</param>
    /// <param name="p"></param>
    /// <returns></returns>
    public static float LinePointDistance(PointF p1, PointF p2, PointF p) {
      var v1 = new windows.Vector(p2.X - p1.X, p2.Y - p1.Y);
      var v2 = new windows.Vector(p.X - p1.X, p.Y - p1.Y);
      return (float)(CrossProduct(v1, v2) / v1.Length);
    }

    /// <summary>
    /// Signed length of projection of (a->p) to vector (a->b). 
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="p"></param>
    /// <returns></returns>
    public static float ProjectionDistance(PointF a, PointF b, PointF p) {
      var ab = Vector(a, b);
      var ap = Vector(a, p);
      return (float)(DotProduct(ab, ap) / ab.Length);
    }

    /// <summary>
    /// Line segement (a, b) to point p distance.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="p"></param>
    /// <returns></returns>
    public static double LineSegPointDistance(Point a, Point b, Point p) {
      return (ProjectionDistance(a, b, p) > 0 && ProjectionDistance(b, a, p) > 0) ?
          Math.Abs(LinePointDistance(a, b, p)) :
          Math.Min(Vector(p, a).Length, Vector(p, b).Length);
    }

    /// <summary>
    /// Returns a new <c>Vector</c> from a to b.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static windows.Vector Vector(PointF a, PointF b) {
      return new windows.Vector(b.X - a.X, b.Y - a.Y);
    }

    /// <summary>
    /// Returns the scalar value of the cross product of two vectors.
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <returns></returns>
    public static float CrossProduct(windows.Vector v1, windows.Vector v2) {
      return (float)(v1.X * v2.Y - v1.Y * v2.X);
    }

    public static float DotProduct(windows.Vector v1, windows.Vector v2) {
      return (float)(v1.X * v2.X + v1.Y * v2.Y);
    }

    /// <summary>
    /// Angle between two vectors in radians.
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <returns></returns>
    public static float Angle(windows.Vector v1, windows.Vector v2) {
      if (v1.Length == 0 || v2.Length == 0)
        throw new ArgumentException("Vector lenght cannot be 0.");
      return (float)Math.Acos(DotProduct(v1, v2) / (v1.Length * v2.Length));
    }

    /// <summary>
    /// Returns true iff p lies somewhere between p1 and p2 on x-coordinate
    /// </summary>
    public static bool BetweenX(Point p, Point p1, Point p2) {
      return ((p1.X <= p.X && p.X <= p2.X) || (p2.X <= p.X && p.X <= p1.X));
    }

    public static int Sign(double n) {
      return n > Eps ? 1 : (n < -Eps ? -1 : 0);
    }

    /// <summary>
    /// Returns true if segment (a1, a2) intersects with segment (b1, b2) including end points.
    /// </summary>
    /// <param name="a1"></param>
    /// <param name="a2"></param>
    /// <param name="b1"></param>
    /// <param name="b2"></param>
    /// <returns></returns>
    public static bool SegmentsIntersect(Point a1, Point a2, Point b1, Point b2) {
      var a1a2 = new windows.Vector(a2.X - a1.X, a2.Y - a1.Y);
      var a1b1 = new windows.Vector(b1.X - a1.X, b1.Y - a1.Y);
      var a1b2 = new windows.Vector(b2.X - a1.X, b2.Y - a1.Y);
      var b1b2 = new windows.Vector(b2.X - b1.X, b2.Y - b1.Y);
      var b1a1 = new windows.Vector(a1.X - b1.X, a1.Y - b1.Y);
      var b1a2 = new windows.Vector(a2.X - b1.X, a2.Y - b1.Y);
      return Sign(CrossProduct(a1a2, a1b1)) * Sign(CrossProduct(a1a2, a1b2)) <= 0 &&
          Sign(CrossProduct(b1b2, b1a1)) * Sign(CrossProduct(b1b2, b1a2)) <= 0;
    }

    public static bool SegmentsIntersect(PointF a1, PointF a2, PointF b1, PointF b2) {
      var a1a2 = new windows.Vector(a2.X - a1.X, a2.Y - a1.Y);
      var a1b1 = new windows.Vector(b1.X - a1.X, b1.Y - a1.Y);
      var a1b2 = new windows.Vector(b2.X - a1.X, b2.Y - a1.Y);
      var b1b2 = new windows.Vector(b2.X - b1.X, b2.Y - b1.Y);
      var b1a1 = new windows.Vector(a1.X - b1.X, a1.Y - b1.Y);
      var b1a2 = new windows.Vector(a2.X - b1.X, a2.Y - b1.Y);
      return Sign(CrossProduct(a1a2, a1b1)) * Sign(CrossProduct(a1a2, a1b2)) <= 0 &&
          Sign(CrossProduct(b1b2, b1a1)) * Sign(CrossProduct(b1b2, b1a2)) <= 0;
    }

    public static PointF Rotate(PointF p, PointF pivot, double theta) {
      var x = Math.Cos(theta) * (p.X - pivot.X) - Math.Sin(theta) * (p.Y - pivot.Y) +
          pivot.X;
      var y = Math.Sin(theta) * (p.X - pivot.X) + Math.Cos(theta) * (p.Y - pivot.Y) +
          pivot.Y;

      return new PointF((float)x, (float)y);
    }

    /// <summary>
    /// Returns the end points of the major and minor axes of an ellipse.
    /// </summary>
    /// <param name="ellipse"></param>
    /// <returns></returns>
    public static PointF[] EllipseAxes(MCvBox2D ellipse) {
      var vertices = ellipse.GetVertices();
      var midPoints = new PointF[vertices.Length];
      for (int i = 0; i < midPoints.Length; i++) {
        var p1 = vertices[i];
        var p2 = vertices[(i + 1) % vertices.Length];
        var mid = new PointF((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
        // Correct the bug in the angle of the ellipse in OpenCV.
        midPoints[i] = Rotate(mid, ellipse.center, Math.PI / 2);
      }

      return midPoints;
    }

    /// <summary>
    /// Extends segment (p1, p2) by certain length. The direction of extension is from p1 to 
    /// p2. If <c>extension</c> is negative, it is equivalent to shortening the segment. The 
    /// length of the segment cannot be zero.
    /// </summary>
    /// <param name="p1">One end of the segment.</param>
    /// <param name="p2">The other end of the segment.</param>
    /// <param name="extension">The length of the extension.</param>
    /// <returns>The new extended point p2.</returns>
    public static PointF Extend(PointF p1, PointF p2, float extension) {
      if (p1.X == p2.X && p1.Y == p2.Y)
        throw new ArgumentException("p1 and p2 cannot be the same.");
      var distance = Distance(p1, p2);
      return new PointF((float)(p2.X + (p2.X - p1.X) * extension / distance),
          (float)(p2.Y + (p2.Y - p1.Y) * extension / distance));
    }

    /// <summary>
    /// Offsets a point in a particular direction by certian length.
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="dir"></param>
    /// <param name="len"></param>
    /// <returns></returns>
    public static Point Offset(Point p1, windows.Vector dir, float len) {
      return new Point((int)(p1.X + dir.X * len / dir.Length),
          (int)(p1.Y + dir.Y * len / dir.Length));
    }

    public static PointF Offset(PointF p1, windows.Vector dir, float len) {
      return new PointF(p1.X + (float)(dir.X * len / dir.Length),
          p1.Y + (float)(dir.Y * len / dir.Length));
    }

    /// <summary>
    /// Point of intersection between line ab and line cd.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <param name="d"></param>
    /// <returns></returns>
    public static PointF LineIntersection(PointF a, PointF b, PointF c, PointF d) {
      var ca = new windows.Vector(a.X - c.X, a.Y - c.Y);
      var cd = new windows.Vector(d.X - c.X, d.Y - c.Y);
      var ab = new windows.Vector(b.X - a.X, b.Y - a.Y);
      var scale = CrossProduct(ca, cd) / CrossProduct(cd, ab);
      return new PointF((float)(a.X + scale * ab.X), (float)(a.Y + scale * ab.Y));
    }

    /// <summary>
    /// Returns true if the angle from vector bc to vector ba is clockwise.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    public static bool IsAngleClockwise(PointF a, PointF b, PointF c) {
      windows.Vector ba = new windows.Vector(a.X - b.X, a.Y - b.Y);
      windows.Vector bc = new windows.Vector(c.X - b.X, c.Y - b.Y);
      return CrossProduct(bc, ba) < 0;
    }

    public static bool IsAngleClockwise(windows.Vector v1, windows.Vector v2) {
      return CrossProduct(v1, v2) < 0;
    }

    public static windows.Point Center(this Rectangle rect) {
      return new windows.Point((rect.Left + rect.Right) / 2.0, (rect.Top + rect.Bottom) / 2.0);
    }
  }
}