using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Kinect.Toolbox.Record;
using Kinect.Toolbox;

namespace HandInput.GesturesViewer {
  public class DepthStreamManager : Notifier, IStreamManager {
    public short[] PixelData { get; private set; }
    byte[] depthFrame32;

    public WriteableBitmap Bitmap { get; private set; }

    /// <summary>
    /// Creates a new PixelData array.
    /// </summary>
    /// <param name="frame"></param>
    public void Update(ReplayDepthImageFrame frame) {
      PixelData = new short[frame.PixelDataLength];
      frame.CopyPixelDataTo(PixelData);

      if (depthFrame32 == null) {
        depthFrame32 = new byte[frame.Width * frame.Height * 4];
      }

      if (Bitmap == null) {
        Bitmap = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgra32, null);
      }

      ConvertDepthFrame(PixelData);

      int stride = Bitmap.PixelWidth * Bitmap.Format.BitsPerPixel / 8;
      Int32Rect dirtyRect = new Int32Rect(0, 0, Bitmap.PixelWidth, Bitmap.PixelHeight);
      Bitmap.WritePixels(dirtyRect, depthFrame32, stride, 0);

      RaisePropertyChanged(() => Bitmap);
    }

    void ConvertDepthFrame(short[] depthFrame16) {
      for (int i16 = 0, i32 = 0; i16 < depthFrame16.Length && i32 < depthFrame32.Length; i16++, i32 += 4) {
        int user = depthFrame16[i16] & 0x07;
        int realDepth = (depthFrame16[i16] >> 3);

        byte intensity = 0;
        if (realDepth > 0)
          intensity = (byte)(255 - (255 * realDepth / 0x1fff));

        depthFrame32[i32] = 0;
        depthFrame32[i32 + 1] = 0;
        depthFrame32[i32 + 2] = 0;
        depthFrame32[i32 + 3] = 255;

        switch (user) {
          case 0: // no one
            depthFrame32[i32] = (byte)(intensity / 2);
            depthFrame32[i32 + 1] = (byte)(intensity / 2);
            depthFrame32[i32 + 2] = (byte)(intensity / 2);
            break;
          case 1:
            depthFrame32[i32] = intensity;
            break;
          case 2:
            depthFrame32[i32 + 1] = intensity;
            break;
          case 3:
            depthFrame32[i32 + 2] = intensity;
            break;
          case 4:
            depthFrame32[i32] = intensity;
            depthFrame32[i32 + 1] = intensity;
            break;
          case 5:
            depthFrame32[i32] = intensity;
            depthFrame32[i32 + 2] = intensity;
            break;
          case 6:
            depthFrame32[i32 + 1] = intensity;
            depthFrame32[i32 + 2] = intensity;
            break;
          case 7:
            depthFrame32[i32] = intensity;
            depthFrame32[i32 + 1] = intensity;
            depthFrame32[i32 + 2] = intensity;
            break;
        }
      }
    }
  }
}
