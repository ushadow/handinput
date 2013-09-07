using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;

namespace HandInput.HandInputEngine {
  public class HandInputEvent {
    public DepthImagePoint LeftHand { get; private set; }
    public DepthImagePoint RightHand { get; private set; }
    public HandInputEvent(DepthImagePoint leftHand, DepthImagePoint rightHand) {
      LeftHand = leftHand;
      RightHand = rightHand;
    }
  }
}
