using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;

using HandInput.Util;

namespace HandInput.HandInputEngine
{
  class HandTracker
  {
    private Skeleton[] skeletonData;

    public void Update(SkeletonFrame sf)
    {
      if (sf != null)
      {
        if (skeletonData == null || skeletonData.Length != sf.SkeletonArrayLength)
        {
          skeletonData = new Skeleton[sf.SkeletonArrayLength];
        }

        sf.CopySkeletonDataTo(skeletonData);
        var trackedIndex = SkeletonUtil.FirstTrackedSkeletonIndex(skeletonData);
        if (trackedIndex >= 0)
        {
          var skeleton = skeletonData[trackedIndex];
        }

      }
    }
  }
}
