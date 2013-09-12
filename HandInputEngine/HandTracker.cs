using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;

using HandInput.Util;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Threading.Tasks.Dataflow;

namespace HandInput.Engine {
  public class HandTracker {
    Skeleton[] skeletonData;
    CoordinateMapper coordMapper;
    byte[, ,] imageStorage;
    int width = 640, height = 480;
    Image<Gray, Byte> gray;
    Image<Gray, Byte> scaled;

    public HandTracker(CoordinateMapper coordMapper) {
      this.coordMapper = coordMapper;
      imageStorage = new byte[height, width, 3];
      gray = new Image<Gray, byte>(width, height);
      scaled = new Image<Gray, byte>(width / 4, height / 4);
    }

    public async void StartAsync(BufferBlock<byte[]> source) {
      while (await source.OutputAvailableAsync()) {
        IList<byte[]> received = new List<byte[]>();
        source.TryReceiveAll(out received);
        Update(null, received.Last());
      }
    }


    public Option<HandInputEvent> Update(SkeletonFrame sf, byte[] cf) {
      if (cf != null) {
        var image = ImageUtil.CreateBgrImage(cf, imageStorage, width, height);
        CvInvoke.cvCvtColor(image, gray, COLOR_CONVERSION.CV_BGR2GRAY);
        CvInvoke.cvResize(gray, scaled, INTER.CV_INTER_CUBIC);
      }

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
