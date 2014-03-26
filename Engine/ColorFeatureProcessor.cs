using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using HandInput.Util;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

using Common.Logging;

namespace HandInput.Engine {
  /// <summary>
  /// Output unormailized features: relative position and image patch data without further processing.
  /// </summary>
  public class ColorFeatureProcessor : IFeatureProcessor {
    static readonly int FeatureImageWidth = HandInputParams.FeatureImageWidth;
    static readonly ILog Log = LogManager.GetCurrentClassLogger();

    public int MotionFeatureLength {
      get {
        return 3;
      }
    }

    public int DescriptorLength { get { return 0; } }

    public ColorFeatureProcessor(float sampleRate = 1) {
    }

    /// <summary>
    /// Creates a new feature array.
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public Option<Array> Compute(TrackingResult result) {
      if (result.RightHandRelPos.IsSome) {
        var colorBb = result.ColorBoundingBoxes.Last();
        var descriptorLen = colorBb.Width * colorBb.Height;
        var feature = new float[MotionFeatureLength + descriptorLen + 2];
        var relPos = result.RightHandRelPos.Value;
        feature[0] = (float)relPos.X;
        feature[1] = (float)relPos.Y;
        feature[2] = (float)relPos.Z;
        feature[3] = colorBb.Width;
        feature[4] = colorBb.Height;
        AddImageFeature(result.ColorImage, colorBb, feature, MotionFeatureLength + 2);
        return new Some<Array>(feature);
      }
      return new None<Array>();
    }

    void AddImageFeature(Image<Gray, Byte> image, Rectangle bb, float[] dst, int dstIndex) {
      var data = image.Data;
     
      var k = 0;
      // Copy bytes.
      for (int i = bb.Top; i < bb.Top + bb.Height; i++)
        for (int j = bb.Left; j < bb.Left + bb.Width; j++, k++) {
          dst[dstIndex + k] = data[i, j, 0];
        }
    }
  }
}
