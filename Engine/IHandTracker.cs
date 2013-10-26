using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandInput.Engine {
  public interface IHandTracker {
    TrackingResult Update(short[] depthFrame, byte[] colorFrame, Skeleton skeleton);
  }
}
