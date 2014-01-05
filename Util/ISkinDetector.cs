using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using Emgu.CV;
using Emgu.CV.Structure;

namespace HandInput.Util {
  public interface ISkinDetector {
    Image<Gray, Byte> SkinImage { get; }
    Image<Gray, Byte> DetectSkin(byte[] image);
    Image<Gray, Byte> DetectSkin(byte[] image, Rectangle roi);
    Rectangle Smooth(Rectangle roi);
  }
}
