using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.GPU;

using HandInput.Util;
using Emgu.CV.Util;

namespace HandInput.Engine {
  public class PlayerDetector {
    private static readonly int CvOpenIter = 1;
    private static readonly int ContourApproxLevel = 2;

    public Image<Gray, Byte> PlayerMask { get; private set; }

    /// <summary>
    /// Scaled depth value image for the player. Non-player and non-skin pixels are 0.
    /// </summary>
    public Image<Gray, Byte> DepthImage { get; private set; }
    public Image<Gray, Byte> AlignedSkinMask;

    private int width, height;
    private MemStorage mem = new MemStorage();
    private ISkinDetector skinDetector;

    public static int ToWorldDepth(double depth) {
      return (int)(Parameters.MaxDepth - depth * Parameters.MaxDepth / 255);
    }

    public PlayerDetector(int width, int height) {
      this.width = width;
      this.height = height;
      PlayerMask = new Image<Gray, Byte>(width, height);
      DepthImage = new Image<Gray, Byte>(width, height);
    }

    /// <summary>
    /// Detects the bounding box of the player.
    /// </summary>
    /// <param name="depthFrame"></param>
    /// <returns></returns>
    public void Update(short[] depthFrame, byte[] colorPixelData, ColorDepthMapper mapper) {
      UpdatePlayerMask(depthFrame);
      UpdatePlayerDepthImage(depthFrame, PlayerMask.Data, null);
    }

    /// <summary>
    /// Detects the player and filters out skin color region only.
    /// </summary>
    /// <param name="depthFrame"></param>
    /// <param name="colorPixelData"></param>
    /// <param name="mapper"></param>
    public void UpdateFilterSkin(short[] depthFrame, byte[] colorPixelData,
                                      ColorDepthMapper mapper) {
      if (skinDetector == null)
        skinDetector = new SkinDetectorGpu(width, height);

      if (AlignedSkinMask == null)
        AlignedSkinMask = new Image<Gray, Byte>(width, height);

      var skinMask = skinDetector.DetectSkin(colorPixelData);
      ImageUtil.AlignColorImage(skinMask, AlignedSkinMask, depthFrame, mapper);
      CvInvoke.cvMorphologyEx(AlignedSkinMask.Ptr, AlignedSkinMask.Ptr, IntPtr.Zero, IntPtr.Zero,
                              CV_MORPH_OP.CV_MOP_CLOSE, 1);
      UpdatePlayerMask(depthFrame);
      UpdatePlayerDepthImage(depthFrame, PlayerMask.Data, AlignedSkinMask.Data);
    }

    private void UpdatePlayerMask(short[] depthFrame) {
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
    }

    private Seq<Point> FindPlayerContour() {
      var contourPtr = new IntPtr();
      CvInvoke.cvFindContours(PlayerMask.Ptr, mem.Ptr, ref contourPtr, StructSize.MCvContour,
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

    void UpdatePlayerDepthImage(short[] depthFrame, Byte[, ,] playerMask, Byte[, ,] skinMask) {
      CvInvoke.cvZero(DepthImage.Ptr);
      var data = DepthImage.Data;

      var scale = (float)255 / Parameters.MaxDepth;
      for (int r = 0; r < height; r++)
        for (int c = 0; c < width; c++) {
          var index = r * width + c;
          short pixel = depthFrame[index];
          var depth = DepthUtil.RawToDepth(pixel);
          depth = depth < Parameters.MinDepth ? Parameters.MaxDepth : depth;
          data[r, c, 0] = (byte)(Math.Max(0, Parameters.MaxDepth - depth) * scale);
        }
    }

    bool IsPlayerPixel(Byte[, ,] playerMask, Byte[, ,] skinMask, int x, int y) {
      return (skinMask == null || skinMask[y, x, 0] > 0) && playerMask[y, x, 0] > 0;
    }

    bool IsPlayerPixel(Seq<Point> contour, Byte[, ,] skinMask, int x, int y) {
      return contour.InContour(new Point(x, y)) >= 0 && (skinMask == null || skinMask[y, x, 0] > 0);
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
