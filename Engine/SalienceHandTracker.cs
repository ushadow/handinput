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

using handinput;

namespace HandInput.Engine {
  public class SalienceHandTracker : IHandTracker {
    public static readonly float HandWidth = 0.095f; // m
    static readonly int NumBin = 256;
    static readonly int DefaultZDist = 1; // m
    readonly ILog Log = LogManager.GetCurrentClassLogger();
    static readonly StructuringElementEx StructuringElement = new StructuringElementEx(3, 3, 1, 1,
       Emgu.CV.CvEnum.CV_ELEMENT_SHAPE.CV_SHAPE_RECT);
    static readonly int CamShiftIter = 2;
    static readonly int NumTrackedHands = 2;

    public Image<Gray, Single> SaliencyProb { get; private set; }
    public Image<Gray, Byte> TempMask { get; private set; }
    public List<Rectangle> PrevBoundingBoxes { get; private set; }
    // Smoothed player depth image.
    public Image<Gray, Byte> SmoothedDepth { get; private set; }
    public Image<Gray, Single> TemporalSmoothed { get; private set; }
    public Image<Gray, Byte> Diff0 { get; private set; }
    public Image<Gray, Byte> DiffMask1 { get; private set; }
    public Image<Gray, Byte> DiffMask0 { get; private set; }
    public FaceModel FaceModel { get; private set; }

    float[] diffCumulativeDist, depthCumulativeDist;
    PlayerDetector playerDetector;
    int t = 0;
    int width, height;
    IntPtr storage = CvInvoke.cvCreateMemStorage(0);
    IntPtr contourPtr = new IntPtr();
    MCvConnectedComp connectedComp = new MCvConnectedComp();
    MCvBox2D shiftedBox = new MCvBox2D();
    CoordinateConverter mapper;
    MHandTracker handTracker;
    Image<Gray, Byte> prevSmoothedDepth;

    /// <summary>
    /// Creates a detector based on salience.
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="kinectParamsBinary">Kinect parameters in binary.</param>
    public SalienceHandTracker(int width, int height, Byte[] kinectParams) {
      mapper = new CoordinateConverter(kinectParams, HandInputParams.ColorImageFormat,
                                    HandInputParams.DepthImageFormat);
      Init(width, height);
    }

    public SalienceHandTracker(int width, int height, CoordinateMapper mapper) {
      this.mapper = new CoordinateConverter(mapper, HandInputParams.ColorImageFormat,
                                         HandInputParams.DepthImageFormat);
      Init(width, height);
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
    public TrackingResult Update(short[] depthFrame, byte[] colorFrame, Skeleton skeleton) {
      t++;

      Option<Vector3D> relPos = new None<Vector3D>();

      if (skeleton == null || depthFrame == null)
        return new TrackingResult();

      playerDetector.FilterPlayerContourSkin(depthFrame, colorFrame);
      var depthImage = playerDetector.DepthImage;
      // Median smoothing cannot be in place.
      CvInvoke.cvSmooth(depthImage.Ptr, SmoothedDepth.Ptr, SMOOTH_TYPE.CV_MEDIAN, 5, 5,
                        0, 0);

      if (t > 1) {
        CvInvoke.cvAbsDiff(SmoothedDepth.Ptr, prevSmoothedDepth.Ptr, Diff0.Ptr);
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
          PrevBoundingBoxes = FindBestBoundingBox(depthFrame, skeleton);
          if (PrevBoundingBoxes.LastOrDefault().Width > 0)
            relPos = new Some<Vector3D>(SkeletonUtil.RelativePosToShoulder(PrevBoundingBoxes.Last(),
                SmoothedDepth.Data, width, height, skeleton, mapper));
        }
      }
      SmoothedDepth.CopyTo(prevSmoothedDepth);
      List<Rectangle> colorBBs = new List<Rectangle>();
      foreach (var bb in PrevBoundingBoxes) {
        var colorBox = mapper.MapDepthRectToColorRect(bb, depthFrame, width, height);
        colorBBs.Add(colorBox);
      }
      return new TrackingResult(relPos, SmoothedDepth, PrevBoundingBoxes, playerDetector.SkinImage,
                                colorBBs);
    }

    void Init(int width, int height) {
      this.width = width;
      this.height = height;

      handTracker = new MHandTracker(width, height);

      // Diff at t - 0;
      DiffMask0 = new Image<Gray, Byte>(width, height);
      DiffMask1 = new Image<Gray, Byte>(width, height);
      SaliencyProb = new Image<Gray, Single>(width, height);
      Diff0 = new Image<Gray, Byte>(width, height);
      SmoothedDepth = new Image<Gray, Byte>(width, height);
      prevSmoothedDepth = new Image<Gray, Byte>(width, height);
      TempMask = new Image<Gray, Byte>(width, height);
      TemporalSmoothed = new Image<Gray, Single>(width, height);

      diffCumulativeDist = new float[NumBin];
      depthCumulativeDist = new float[NumBin];
      playerDetector = new PlayerDetector(width, height, mapper);
      PrevBoundingBoxes = new List<Rectangle>();
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
    /// If no bounding box is found, returns the last bounding box.
    /// </summary>
    /// <returns></returns>
    List<Rectangle> FindBestBoundingBox(short[] depthFrame, Skeleton skeleton) {
      CvInvoke.cvConvert(SaliencyProb.Ptr, TempMask.Ptr);
      // Non-zero pixels are treated as 1s. Source image content is modifield.
      CvInvoke.cvFindContours(TempMask.Ptr, storage, ref contourPtr, StructSize.MCvContour,
        RETR_TYPE.CV_RETR_EXTERNAL, CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, new Point(0, 0));
      var contour = new Seq<Point>(contourPtr, null);

      SortedList<float, Seq<Point>> bestContours = new SortedList<float, Seq<Point>>();
      List<Rectangle> bestBoundingBoxes = PrevBoundingBoxes;

      float z = DefaultZDist;
      var hand = SkeletonUtil.GetJoint(skeleton, JointType.HandRight);
      if (hand != null)
        z = hand.Position.Z;
      double perimThresh = DepthUtil.GetDepthImageLength(width, HandWidth, z) * 2;

      FaceModel = SkeletonUtil.GetFaceModel(skeleton, mapper);

      for (; contour != null && contour.Ptr.ToInt32() != 0; contour = contour.HNext) {
        var perim = CvInvoke.cvContourPerimeter(contour.Ptr);
        if (perim > perimThresh) {
          var rect = contour.BoundingRectangle;
          var score = ContourScore(rect);
          var center = rect.Center();
          int x = (int)center.X;
          int y = (int)center.Y;
          var depth = DepthUtil.RawToDepth(depthFrame[y * width + x]);
          if (!FaceModel.IsPartOfFace(x, y, depth) &&
              (bestContours.Count < NumTrackedHands || score > bestContours.ElementAt(0).Key)) {
            bestContours.Add(score, contour);
            if (bestContours.Count > NumTrackedHands)
              bestContours.RemoveAt(0);
          }
        }
      }

      if (bestContours.Count > 0) {
        bestBoundingBoxes.Clear();
        SmoothedDepth.CopyTo(TempMask);
        foreach (var c in bestContours.Values) {
          var rect = c.BoundingRectangle;
          CvInvoke.cvCamShift(SmoothedDepth.Ptr, rect, new MCvTermCriteria(CamShiftIter),
              out connectedComp, out shiftedBox);
          var bestBoundingBox = shiftedBox.MinAreaRect();
          bestBoundingBoxes.Add(bestBoundingBox);
          if (bestBoundingBox.Width > 0) {
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
            TempMask.ROI = Rectangle.Empty;
          }

        }
      }

      return bestBoundingBoxes;
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
      int count = 0;
      var data = SaliencyProb.Data;
      for (int y = rect.Top; y < rect.Top + rect.Height; y++)
        for (int x = rect.Left; x < rect.Left + rect.Width; x++) {
          if (y >= 0 && y < height && x >= 0 && x < width) {
            sum += data[y, x, 0];
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
