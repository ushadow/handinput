using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HandInput.Util;

namespace HandInput.Engine {
  public interface IFeatureProcessor {
    int MotionFeatureLength { get; }
    int DescriptorLength { get; }
    Option<Array> Compute(TrackingResult result);
  }
}
