using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.GPU;

using Emgu.CV.Util;
using Microsoft.Kinect;

namespace HandInput.Util {
  /// <summary>
  /// A detector for player and player skin.
  /// </summary>
  public class PlayerDetector {
    static readonly StructuringElementEx StrucElem3 = new StructuringElementEx(3, 3, 2, 2,
        CV_ELEMENT_SHAPE.CV_SHAPE_RECT);
    static readonly int CvOpenIter = 1;
    static readonly int ContourApproxLevel = 2;

    public Image<Gray, Byte> PlayerMask { get; private set; }
    public Image<Gray, Byte> ColorPlayerMask { get; private set; }

    /// <summary>
    /// Scaled depth value image for the player. Non-player and non-skin pixels are 0.
    /// </summary>
    public Image<Gray, Byte> DepthImage { get; private set; }
    public Image<Gray, Byte> DepthSkinMask;
    /// <summary>
    /// Gray scale skin image.
    /// </summary>
    public Image<Gray, Byte> SkinImage {
      get { return dataBuffer.Peek<Image<Gray, Byte>>(typeof(Image<Gray, Byte>)); }
    }

    int width, height;
    MemStorage mem = new MemStorage();
    ISkinDetector skinDetector;
    Image<Gray, Byte> tempPlayerMask;
    CoordinateConverter mapper;
    DataBuffer dataBuffer;

    public static int ToWorldDepth(double depth) {
      return (int)(HandInputParams.MaxDepth - depth * HandInputParams.MaxDepth / 255);
    }

    public PlayerDetector(int width, int height, CoordinateConverter mapper, int bufferSize = 1) {
      this.width = width;
      this.height = height;
      this.mapper = mapper;
      PlayerMask = new Image<Gray, Byte>(width, height);
      ColorPlayerMask = new Image<Gray, Byte>(width, height);
      DepthImage = new Image<Gray, Byte>(width, height);
      dataBuffer = new DataBuffer(bufferSize);
    }

    /// <summary>
    /// Updates the player mask and the depth image.
    /// </summary>
    /// <param name="depthFrame"></param>
    /// <returns></returns>
    public void FilterPlayer(short[] depthFrame, byte[] colorPixelData) {
      UpdatePlayerDepthImage(depthFrame, PlayerMask.Data, null, Rectangle.Empty);
    }

    public void FilterPlayerContourSkin(short[] depthFrame, byte[] colorFrame) {
      if (skinDetector == null)
        skinDetector = new SkinDetector(width, height);

      if (DepthSkinMask == null)
        DepthSkinMask = new Image<Gray, Byte>(width, height);

      UpdatePlayerMask(depthFrame);
      var contour = FindPlayerContour(PlayerMask);

      var skinMask = skinDetector.DetectSkin(colorFrame);
      ImageUtil.AlignImageColorToDepth(skinMask, DepthSkinMask, depthFrame, mapper);
      CvInvoke.cvAnd(skinDetector.SkinImage.Ptr, ColorPlayerMask.Ptr, skinDetector.SkinImage.Ptr, 
                     IntPtr.Zero);
      dataBuffer.EnqueueAndCopy(skinDetector.SkinImage);

      UpdatePlayerDepthImage(depthFrame, contour, DepthSkinMask.Data);
    }

    public void SmoothSkin(Rectangle roi) {
      var skin = SkinImage;
      skin.ROI = roi;
      CvInvoke.cvMorphologyEx(skin.Ptr, skin.Ptr, IntPtr.Zero, StrucElem3, CV_MORPH_OP.CV_MOP_CLOSE, 1);
      skin.ROI = Rectangle.Empty;
    }

    /// <summary>
    /// Updates the player and skin masks without fitering out the player and the skin region.
    /// </summary>
    /// <param name="depthFrame"></param>
    /// <param name="colorPixelData"></param>
    /// <param name="mapper"></param>
    public void UpdateMasks(short[] depthFrame, byte[] colorPixelData) {
      UpdateMasks(depthFrame, colorPixelData, Rectangle.Empty, false, false);
    }

    /// <summary>
    /// Updates both skin mask and player mask.
    /// </summary>
    /// <param name="depthFrame"></param>
    /// <param name="colorPixelData"></param>
    /// <param name="mapper"></param>
    /// <param name="roi">ROI in depth image.</param>
    public void UpdateMasks(short[] depthFrame, byte[] colorPixelData,
        Rectangle roi, bool filterPlayer = false, bool filterSkin = false) {
      if (skinDetector == null)
        skinDetector = new SkinDetector(width, height);

      if (DepthSkinMask == null)
        DepthSkinMask = new Image<Gray, Byte>(width, height);

      var colorSkinMask = skinDetector.DetectSkin(colorPixelData, roi);
      ImageUtil.AlignImageColorToDepth(colorSkinMask, DepthSkinMask, depthFrame, mapper);
      dataBuffer.EnqueueAndCopy(skinDetector.SkinImage);

      UpdatePlayerMask(depthFrame);
      byte[, ,] playerMask = null;
      byte[, ,] skinMask = null;
      if (filterPlayer)
        playerMask = PlayerMask.Data;
      if (filterSkin)
        skinMask = DepthSkinMask.Data;
      UpdatePlayerDepthImage(depthFrame, playerMask, skinMask, roi);
    }

    void UpdatePlayerMask(short[] depthFrame) {
      CvInvoke.cvZero(PlayerMask.Ptr);
      var data = PlayerMask.Data;

      for (int r = 0; r < height; r++)
        for (int c = 0; c < width; c++) {
          var index = r * width + c;
          short pixel = depthFrame[index];
          int playerIndex = DepthUtil.RawToPlayerIndex(pixel);
          if (playerIndex > 0)
            data[r, c, 0] = 255;
        }

      CvInvoke.cvMorphologyEx(PlayerMask.Ptr, PlayerMask.Ptr,
          IntPtr.Zero, IntPtr.Zero, CV_MORPH_OP.CV_MOP_OPEN, CvOpenIter);

      ImageUtil.AlignImageDepthToColor(PlayerMask, ColorPlayerMask, depthFrame, mapper);
    }

    Seq<Point> FindPlayerContour(Image<Gray, Byte> mask) {
      var contourPtr = new IntPtr();
      if (tempPlayerMask == null)
        tempPlayerMask = new Image<Gray, Byte>(width, height);

      CvInvoke.cvCopy(mask.Ptr, tempPlayerMask.Ptr, IntPtr.Zero);
      CvInvoke.cvFindContours(tempPlayerMask.Ptr, mem.Ptr, ref contourPtr, StructSize.MCvContour,
          RETR_TYPE.CV_RETR_EXTERNAL, CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, new Point(0, 0));
      var contour = new Seq<Point>(contourPtr, null);

      double maxPerim = 0;
      Seq<Point> largestContour = null;

      for (; contour != null && contour.Ptr.ToInt32() != 0; contour = contour.HNext) {
        var perim = CvInvoke.cvContourPerimeter(contour.Ptr);
        if (perim > maxPerim) {
          maxPerim = perim;
          largestContour = contour;
        }
      }
      var polyPtr = largestContour.ApproxPoly(ContourApproxLevel, mem);
      return polyPtr;
    }

    void UpdatePlayerDepthImage(short[] depthFrame, Byte[, ,] playerMask, Byte[, ,] skinMask,
        Rectangle roi) {
      // Clear depth image.
      CvInvoke.cvZero(DepthImage.Ptr);
      var data = DepthImage.Data;

      var scale = (float)255 / HandInputParams.MaxDepth;

      var roiWidth = width;
      var roiHeight = height;
      if (!roi.IsEmpty) {
        roiWidth = roi.Width;
        roiHeight = roi.Height;
      }
      for (int r = roi.Top; r < roi.Top + roiHeight; r++)
        for (int c = roi.Left; c < roi.Left + roiWidth; c++) {
          var index = r * width + c;
          short pixel = depthFrame[index];
          var depth = DepthUtil.RawToDepth(pixel);
          if (IsFilteredPixel(playerMask, skinMask, c, r, depth))
            data[r, c, 0] = (byte)(Math.Max(0, HandInputParams.MaxDepth - depth) * scale);
        }
    }

    /// <summary>
    /// Updates player depth image using the player contour.
    /// </summary>
    /// <param name="depthFrame"></param>
    /// <param name="contour"></param>
    /// <param name="skinMask"></param>
    void UpdatePlayerDepthImage(short[] depthFrame, Seq<Point> contour, Byte[, ,] skinMask) {
      CvInvoke.cvZero(DepthImage.Ptr);
      var data = DepthImage.Data;

      var scale = (float)255 / HandInputParams.MaxDepth;
      for (int r = 0; r < height; r++)
        for (int c = 0; c < width; c++) {
          var index = r * width + c;
          short pixel = depthFrame[index];
          var depth = DepthUtil.RawToDepth(pixel);
          if (IsPlayerPixel(contour, skinMask, c, r, depth)) {
            data[r, c, 0] = (byte)(Math.Max(0, HandInputParams.MaxDepth - depth) * scale);
          }
        }
    }

    bool IsFilteredPixel(Byte[, ,] playerMask, Byte[, ,] skinMask, int x, int y, int depth) {
      return (skinMask == null || skinMask[y, x, 0] > 0) &&
             (playerMask == null || playerMask[y, x, 0] > 0) &&
             depth > HandInputParams.MinDepth;
    }

    bool IsPlayerPixel(Seq<Point> contour, Byte[, ,] skinMask, int x, int y, int depth) {
      return contour.InContour(new Point(x, y)) >= 0 &&
             (skinMask == null || skinMask[y, x, 0] > 0) &&
             depth > HandInputParams.MinDepth;
    }

    private Rectangle FindBoundingBox() {
      var data = PlayerMask.Data;
      int minX = width, maxX = 0, minY = height, maxY = 0;
      for (int r = 0; r < height; r++)
        for (int c = 0; c < width; c++) {
          if (data[r, c, 0] != 0) {
            minX = Math.Min(minX, c);
            maxX = Math.Max(maxX, c);
            minY = Math.Min(minY, r);
            maxY = Math.Max(maxY, r);
          }
        }
      return new Rectangle(minX, minY, maxX - minX, maxY - minY);
    }

  }
}
