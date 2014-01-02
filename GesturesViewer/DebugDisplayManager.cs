using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

using Kinect.Toolbox;
using Kinect.Toolbox.Record;

using Common.Logging;

using HandInput.Util;

namespace HandInput.GesturesViewer {
  /// <summary>
  /// Manages the visualization of depth data.
  /// </summary>
  class DebugDisplayManager : Notifier, IStreamManager {
    static readonly ILog Log = LogManager.GetCurrentClassLogger();

    public short[] DepthPixelData { get; private set; }
    public WriteableBitmap Bitmap { get; private set; }
    public WriteableBitmap BitmapMask { get; private set; }

    int width, height;
    byte[] depthFrame, transparentFrame;

    public DebugDisplayManager(int width, int height) {
      this.width = width;
      this.height = height;
      Bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
      BitmapMask = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
    }

    /// <summary>
    /// Updates the depth frame pixel data. Each pixel contains both the player and the depth
    /// information.
    /// </summary>
    /// <param name="frame"></param>
    public void UpdatePixelData(ReplayDepthImageFrame frame) {
      if (DepthPixelData == null)
        DepthPixelData = new short[frame.PixelDataLength];
      frame.CopyPixelDataTo(DepthPixelData);
    }

    public void UpdateBitmap() {
      ConvertDepthFrame(DepthPixelData);
      UpdateBitmap(depthFrame);
    }

    public void UpdateBitmap(byte[] data) {
      UpdateBitmap(data, Bitmap);
    }

    public void UpdateBitmapMask(Single[, ,] data) {
      if (transparentFrame == null) {
        transparentFrame = new byte[width * height * 4];
      }
      ImageUtil.CreateMask(data, transparentFrame, width, true);
      UpdateBitmap(transparentFrame, BitmapMask);
    }

    /// <summary>
    /// Update the bitmap with the given byte array.
    /// </summary>
    /// <param name="data">the length of the data must be equal to this.width * this.height</param>
    public void UpdateBitmap(byte[] data, WriteableBitmap bitmap) {
      var stride = bitmap.PixelWidth * bitmap.Format.BitsPerPixel / 8;
      var rect = new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);
      bitmap.WritePixels(rect, data, stride, 0);
      RaisePropertyChanged(() => bitmap);
    }

    void ConvertDepthFrame(short[] depthFrame16) {
      if (depthFrame == null)
        depthFrame = new byte[width * height];

      for (int i = 0; i < depthFrame16.Length; i++) {
        int user = depthFrame16[i] & 0x07;
        int realDepth = (depthFrame16[i] >> 3);

        byte intensity = (byte)(255 - (255 * realDepth / 0x1fff));

        depthFrame[i] = intensity;
      }
    }
  }
}
