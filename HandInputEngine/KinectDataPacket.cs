using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;

namespace HandInput.Engine {
  public struct KinectDataPacket {
    public short[] DepthData;
    public byte[] ColorData;
    public Skeleton Skeleton;
  }
}
