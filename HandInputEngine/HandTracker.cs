using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;

using HandInput.Util;

namespace HandInput.HandInputEngine {
  class HandTracker {
    private Skeleton[] skeletonData;
    private CoordinateMapper coordMapper;
    public HandTracker(CoordinateMapper coordMapper) {
      this.coordMapper = coordMapper;
    }

    public Option<HandInputEvent> Update(SkeletonFrame sf) {
      if (sf != null) {
        if (skeletonData == null || skeletonData.Length != sf.SkeletonArrayLength) {
          skeletonData = new Skeleton[sf.SkeletonArrayLength];
        }

        sf.CopySkeletonDataTo(skeletonData);
        var trackedIndex = SkeletonUtil.FirstTrackedSkeletonIndex(skeletonData);
        if (trackedIndex >= 0) {
          var skeleton = skeletonData[trackedIndex];
          var handRight = SkeletonUtil.GetJoint(skeleton, JointType.HandRight);
          var handLeft = SkeletonUtil.GetJoint(skeleton, JointType.HandLeft);

          var handRightPos = coordMapper.MapSkeletonPointToDepthPoint(handRight.Position, 
              DepthImageFormat.Resolution640x480Fps30);
          var handLeftPos = coordMapper.MapSkeletonPointToDepthPoint(handLeft.Position,
              DepthImageFormat.Resolution640x480Fps30);
          return new Some<HandInputEvent>(new HandInputEvent(handLeftPos, handRightPos));
        }
      }
      return new None<HandInputEvent>();
    }
  }
}
