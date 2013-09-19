using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kinect.Toolbox;
using Kinect.Toolbox.Record;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace GesturesViewer {
  class DepthDisplayManager : Notifier, IStreamManager {
    public short[] PixelData { get; private set; }
    public WriteableBitmap Bitmap { get; private set; }

    public void Update(ReplayDepthImageFrame frame) {
      PixelData = new short[frame.PixelDataLength];
      frame.CopyPixelDataTo(PixelData);
    }

    public void Update(byte[] data, int width, int height) {
      if (Bitmap == null) {
        Bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
      }
      int stride = Bitmap.PixelWidth * Bitmap.Format.BitsPerPixel / 8;
      Int32Rect dirtyRect = new Int32Rect(0, 0, Bitmap.PixelWidth, Bitmap.PixelHeight);
      Bitmap.WritePixels(dirtyRect, data, stride, 0);
      RaisePropertyChanged(() => Bitmap);
    }
  }
}
