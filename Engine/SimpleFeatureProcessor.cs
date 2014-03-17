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
  /// Output raw features: relative position and image patch data without further processing.
  /// </summary>
  public class SimpleFeatureProcessor : IFeatureProcessor {
    static readonly int FeatureImageWidth = HandInputParams.FeatureImageWidth;
    static readonly int BytesPerPixel = 4;
    static readonly ILog Log = LogManager.GetCurrentClassLogger();

    public int MotionFeatureLength {
      get {
        return 3;
      }
    }

    public int DescriptorLength {
      get {
        return FeatureImageWidth * FeatureImageWidth * 2;
      }
    }

    Image<Gray, Single> floatImage = new Image<Gray, Single>(FeatureImageWidth, FeatureImageWidth);

    public SimpleFeatureProcessor(float sampleRate = 1) {

    }

    /// <summary>
    /// Creates a new feature array.
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public Option<Array> Compute(TrackingResult result) {
      if (result.RightHandRelPos.IsSome) {
        var feature = new float[MotionFeatureLength + DescriptorLength];
        var relPos = result.RightHandRelPos.Value;
        feature[0] = (float)relPos.X;
        feature[1] = (float)relPos.Y;
        feature[2] = (float)relPos.Z;
        AddImageFeature(result.ColorImage, result.ColorBoundingBoxes.Last(), feature, 
                        MotionFeatureLength);
        AddImageFeature(result.DepthImage, result.DepthBoundingBoxes.Last(), feature,
                        MotionFeatureLength + FeatureImageWidth * FeatureImageWidth);
        return new Some<Array>(feature);
      }
      return new None<Array>();
    }

    void AddImageFeature(Image<Gray, Byte> image, Rectangle bb, float[] dst, int dstIndex) {
      image.ROI = bb;
      floatImage.ConvertFrom(image);
      image.ROI = Rectangle.Empty;
      // Copy bytes.
      System.Buffer.BlockCopy(floatImage.Bytes, 0, dst, dstIndex * BytesPerPixel, 
                              floatImage.Bytes.Length);
    }
  }
}
