using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using windows = System.Windows;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Windows.Media.Media3D;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.CV.GPU;

using HandInput.Util;
using Microsoft.Kinect;
using Common.Logging;

namespace HandInput.Engine {
  public class SaliencyDetector {
    public static readonly float HandWidth = 0.095f; // m
    private static readonly int NumBin = 256;
    private static readonly int DefaultZDist = 1; // m
    private readonly ILog Log = LogManager.GetCurrentClassLogger();
    private static readonly StructuringElementEx StructuringElement = new StructuringElementEx(3, 3, 1, 1,
       Emgu.CV.CvEnum.CV_ELEMENT_SHAPE.CV_SHAPE_RECT);
    private static readonly int CamShiftIter = 2;

    public Image<Gray, Single> SaliencyProb { get; private set; }
    public Image<Gray, Byte> TempMask { get; private set; }
    public Rectangle PrevBoundingBox { get; private set; }
    public Image<Gray, Byte> SmoothedDepth { get; private set; }
    public Image<Gray, Byte> Diff0 { get; private set; }
    public Image<Gray, Byte> DiffMask1 { get; private set; }
    public Image<Gray, Byte> DiffMask0 { get; private set; }

    private float[] diffCumulativeDist, depthCumulativeDist;
    private PlayerDetector playerDetector;
    private int t = 0;
    private int width, height;
    private IntPtr storage = CvInvoke.cvCreateMemStorage(0);
    private IntPtr contourPtr = new IntPtr();
    private MCvConnectedComp connectedComp = new MCvConnectedComp();
    private MCvBox2D shiftedBox = new MCvBox2D();
    private ColorDepthMapper mapper;

    /// <summary>
    /// Creates a detector based on salience.
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="kinectParamsBinary">Kinect parameters in binary.</param>
    public SaliencyDetector(int width, int height, Byte[] kinectParamsBinary) {
      Init(width, height);

      var bf = new BinaryFormatter();
      var stream = new MemoryStream(kinectParamsBinary);
      var kinectParams = bf.Deserialize(stream) as IEnumerable<byte>;
      stream.Close();
      mapper = new ColorDepthMapper(kinectParams);
    }

    public SaliencyDetector(int width, int height, CoordinateMapper mapper) {
      Init(width, height);
      this.mapper = new ColorDepthMapper(mapper);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="detphFrame">raw detph data.</param>
    /// <param name="skeleton">skeleton object from Kinect SDK. The skeleton coordinate space has 
    /// the x-axis pointing rightwards, the y-axis pointing upwards, and the z-axis poining outwards
    /// relative to the image.</param>
    /// <returns>The position of the best bounding box relative to the shoulder joint in skeleton 
    /// coordinates if the bounding box is valid. Otherwise returns None.</returns>
    public Option<Vector3D> detect(short[] depthFrame, byte[] colorPixelData, Skeleton skeleton) {
      t++;

      Option<Vector3D> res = new None<Vector3D>();

      if (skeleton == null || depthFrame == null)
        return res;

      playerDetector.detectFilterSkin(depthFrame, colorPixelData, mapper);
      var playerDepthImage = playerDetector.PlayerDepthImage;
      CvInvoke.cvSmooth(playerDepthImage.Ptr, SmoothedDepth.Ptr, SMOOTH_TYPE.CV_MEDIAN, 5, 5,
                        0, 0);

      if (t > 1) {
        CvInvoke.cvAbsDiff(SmoothedDepth.Ptr, Diff0.Ptr, Diff0.Ptr);
        //CvInvoke.cvErode(Diff0.Ptr, Diff0.Ptr, StructuringElement.Ptr, 1);
        DiffMask0.CopyTo(DiffMask1);
        CvInvoke.cvThreshold(Diff0.Ptr, DiffMask0.Ptr, 2, 255, THRESH.CV_THRESH_BINARY);

        if (t > 2) {
          // Makes diffMask1 the motion mask at t - 1.
          CvInvoke.cvAnd(DiffMask0.Ptr, DiffMask1.Ptr, DiffMask1.Ptr, IntPtr.Zero);
          // Makes diffMask1 the motion mask at t - 0. 
          CvInvoke.cvXor(DiffMask0.Ptr, DiffMask1.Ptr, DiffMask1.Ptr, IntPtr.Zero);
          CvInvoke.cvMorphologyEx(DiffMask1.Ptr, DiffMask1.Ptr, IntPtr.Zero, IntPtr.Zero,
                                  CV_MORPH_OP.CV_MOP_OPEN, 1);
          ComputeCumulativeDist(Diff0, diffCumulativeDist);
          ComputeCumulativeDist(SmoothedDepth, depthCumulativeDist);
          CvInvoke.cvZero(SaliencyProb);
          var diffMaskData = DiffMask1.Data;
          var diffData = Diff0.Data;
          var depthData = SmoothedDepth.Data;
          var probData = SaliencyProb.Data;
          for (int i = 0; i < DiffMask1.Height; i++)
            for (int j = 0; j < DiffMask1.Width; j++) {
              if (diffMaskData[i, j, 0] > 0) {
                var diffBin = diffData[i, j, 0];
                var depthBin = depthData[i, j, 0];
                probData[i, j, 0] = diffCumulativeDist[diffBin] * depthCumulativeDist[depthBin];
              }
            }
          var skeHandJoint = SkeletonUtil.GetJoint(skeleton, JointType.HandRight);
          PrevBoundingBox = FindBestBoundingBox(skeHandJoint);
          if (PrevBoundingBox.Width > 0)
            res = new Some<Vector3D>(RelativePosToShoulder(PrevBoundingBox, skeleton));
        }
      }
      SmoothedDepth.CopyTo(Diff0);
      return res;
    }

    private void Init(int width, int height) {
      this.width = width;
      this.height = height;

      // Diff at t - 0;
      DiffMask0 = new Image<Gray, Byte>(width, height);
      DiffMask1 = new Image<Gray, Byte>(width, height);
      SaliencyProb = new Image<Gray, Single>(width, height);
      Diff0 = new Image<Gray, Byte>(width, height);
      SmoothedDepth = new Image<Gray, Byte>(width, height);
      TempMask = new Image<Gray, Byte>(width, height);

      diffCumulativeDist = new float[NumBin];
      depthCumulativeDist = new float[NumBin];
      playerDetector = new PlayerDetector(width, height);
    }

    private Vector3D RelativePosToShoulder(Rectangle rect, Skeleton skeleton) {
      var shoulderCenterJoint = SkeletonUtil.GetJoint(skeleton, JointType.ShoulderCenter);

      var aveDepth = 0.0;
      var count = 0;
      var depthData = SmoothedDepth.Data;
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="hist"></param>
    /// <param name="cumulativeDist"></param>
    private void ComputeCumulativeDist(Image<Gray, Byte> image, float[] cumulativeDist) {
      Array.Clear(cumulativeDist, 0, cumulativeDist.Length);
      var data = image.Data;

      for (int i = 0; i < height; i++)
        for (int j = 0; j < width; j++) {
          var bin = data[i, j, 0];
          if (bin > 0) {
            cumulativeDist[bin]++;
          }
        }

      for (int i = 1; i < cumulativeDist.Length; i++) {
        cumulativeDist[i] += cumulativeDist[i - 1];
      }

      if (cumulativeDist[cumulativeDist.Length - 1] > 0) {
        for (int i = 0; i < cumulativeDist.Length; i++) {
          cumulativeDist[i] /= cumulativeDist[cumulativeDist.Length - 1];
        }
      }
    }

    /// <summary>
    /// If no bounding box is found, the previous bounding box is returned.
    /// </summary>
    /// <returns></returns>
    private Rectangle FindBestBoundingBox(Joint hand) {
      CvInvoke.cvConvert(SaliencyProb.Ptr, TempMask.Ptr);
      // Non-zero pixels are treated as 1s. Source image content is modifield.
      CvInvoke.cvFindContours(TempMask.Ptr, storage, ref contourPtr, StructSize.MCvContour,
        RETR_TYPE.CV_RETR_EXTERNAL, CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, new Point(0, 0));
      var contour = new Seq<Point>(contourPtr, null);

      float bestScore = 0;
      Seq<Point> bestContour = null;
      Rectangle bestBoundingBox = PrevBoundingBox;

      float z = DefaultZDist;
      if (hand != null)
        z = hand.Position.Z;
      double perimThresh = DepthUtil.GetDepthImageLength(width, HandWidth, z) * 2;

      for (; contour != null && contour.Ptr.ToInt32() != 0; contour = contour.HNext) {
        var perim = CvInvoke.cvContourPerimeter(contour.Ptr);
        if (perim > perimThresh) {
          var rect = contour.BoundingRectangle;
          var score = ContourScore(rect);
          if (score > bestScore) {
            bestScore = score;
            bestContour = contour;
          }
        }
      }

      if (bestContour != null) {
        bestBoundingBox = bestContour.BoundingRectangle;
        CvInvoke.cvCamShift(SmoothedDepth.Ptr, bestBoundingBox, new MCvTermCriteria(CamShiftIter),
            out connectedComp, out shiftedBox);
        bestBoundingBox = shiftedBox.MinAreaRect();
      }

      if (bestBoundingBox.Width > 0) {
        SmoothedDepth.CopyTo(TempMask);
        TempMask.ROI = bestBoundingBox;
        CvInvoke.cvFindContours(TempMask.Ptr, storage, ref contourPtr, StructSize.MCvContour,
            RETR_TYPE.CV_RETR_EXTERNAL, CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
            new Point(0, 0));
        contour = new Seq<Point>(contourPtr, null);

        Seq<Point> largestContour = null;
        var maxPerim = perimThresh;
        for (; contour != null && contour.Ptr.ToInt32() != 0; contour = contour.HNext) {
          var perim = CvInvoke.cvContourPerimeter(contour.Ptr);
          if (perim > maxPerim) {
            maxPerim = perim;
            largestContour = contour;
          }
        }

        CvInvoke.cvZero(TempMask.Ptr);
        if (largestContour != null)
          TempMask.Draw(largestContour, new Gray(255), -1);
        FilterImage(SmoothedDepth, TempMask);
        TempMask.ROI = new Rectangle(0, 0, width, height);
      }

      return bestBoundingBox;
    }

    private void FilterImage(Image<Gray, Byte> image, Image<Gray, Byte> mask) {
      var data = image.Data;
      var maskData = mask.Data;
      var rect = mask.ROI;
      for (int i = rect.Top + 1; i < rect.Top + rect.Height - 1 && i < height; i++)
        for (int j = rect.Left + 1; j < rect.Left + rect.Width - 1 && j < width; j++) {
          if (i > 0 && j > 0 && maskData[i, j, 0] != 255)
            data[i, j, 0] = 0;
        }
    }

    private float AveDepth(Image<Gray, Byte> image) {
      var data = image.Data;
      float sum = 0;
      int count = 0;
      for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++) {
          var depth = data[y, x, 0];
          if (depth > 0) {
            sum += depth;
            count++;
          }
        }
      return sum / count;
    }

    private float ContourScore(Rectangle rect) {
      float sum = 0;
      float depthSum = 0;
      int count = 0;
      var data = SaliencyProb.Data;
      var depthData = SmoothedDepth.Data;
      for (int y = rect.Top; y < rect.Top + rect.Height; y++)
        for (int x = rect.Left; x < rect.Left + rect.Width; x++) {
          if (y >= 0 && y < height && x >= 0 && x < width) {
            sum += data[y, x, 0];
            depthSum += depthData[y, x, 0];
            count++;
          }
        }
      return sum / count;
    }

    private Rectangle InitSearchWindow(Image<Gray, Single> probImage) {
      float m00 = 0, m10 = 0, m01 = 0;
      var data = probImage.Data;
      for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++) {
          var prob = data[y, x, 0];
          m00 += prob;
          m10 += prob * x;
          m01 += prob * y;
        }
      if (m00 <= 0)
        return new Rectangle();
      var xc = m10 / m00;
      var yc = m01 / m00;
      var windowWidth = 2 * Math.Sqrt(m00);
      return new Rectangle(Math.Max(0, (int)(xc - windowWidth / 2)),
          Math.Max(0, (int)(yc - windowWidth / 2)), (int)windowWidth,
          (int)windowWidth);
    }

  }
}
