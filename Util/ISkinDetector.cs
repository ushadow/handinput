using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Emgu.CV;
using Emgu.CV.Structure;

namespace HandInput.Util {
  public interface ISkinDetector {
    Image<Gray, Byte> DetectSkin(byte[] image);
  }
}
