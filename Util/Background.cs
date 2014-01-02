using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandInput.Util {
  /// <summary>
  /// A background model. 
  /// </summary>
  public class Background {
    public bool ModelsCreated { get; private set; }

    public int Count { get; private set; }

    /// <summary>
    /// Frame absolute difference accumulator.
    /// </summary>
    Image<Gray, Single> diff;
    Image<Gray, Single> prev;

    /// <summary>
    /// Frame accumulator.
    /// </summary>
    Image<Gray, Single> avg;
    Image<Gray, Single> high, low;

    /// <summary>
    /// Temporary storage.
    /// </summary>
    Image<Gray, Single> tmp1;

    bool first = true;
    Gray avgDiff;
    MCvScalar stdDiff;

    public Background(int width, int height) {
      avg = new Image<Gray, Single>(width, height);
      diff = new Image<Gray, Single>(width, height);
      tmp1 = new Image<Gray, Single>(width, height);
      high = new Image<Gray, Single>(width, height);
      low = new Image<Gray, Single>(width, height);

      ModelsCreated = false;
    }

    /// <summary>
    /// Learns the background statisitics for one more frame.
    /// </summary>
    /// <param name="image">Image to be accumulated for learning background statistics.</param>
    public void AccumulateBackground(Image<Gray, Single> image) {
      if (!first) {
        avg.Acc(image);
        CvInvoke.cvAbsDiff(image.Ptr, prev.Ptr, tmp1.Ptr);
        diff.Acc(tmp1);
        Count++;
      }
      first = false;
      prev = image.Copy();
    }

    /// <summary>
    /// Creates a statistical model of the background if the model is not created.
    /// </summary>
    public void CreateModelsFromStats() {
      if (Count == 0) {
        throw new System.InvalidOperationException("No background image is accumulated.");
      }

      if (ModelsCreated)
        return;

      CvInvoke.cvConvertScale(avg.Ptr, avg.Ptr, 1.0 / Count, 0);
      CvInvoke.cvConvertScale(diff.Ptr, diff.Ptr, 1.0 / Count, 0);
      diff.AvgSdv(out avgDiff, out stdDiff);

      // Makes sure diff is always something.
      CvInvoke.cvAddS(diff.Ptr, new MCvScalar(0.0001), diff.Ptr, IntPtr.Zero);
      SetHighThreshold((float)7.0);
      SetLowThreshold((float)6.0);
      ModelsCreated = true;
    }

    /// <summary>
    /// Segments an input image into foreground and background. 
    /// </summary>
    /// <param name="image">image of which the background needs to be subtracted.</param>
    /// <param name="mask">a value of 255 in the mask means the pixel is foreground, and a 
    /// value of 0 means the corresponding pixel is background.</param>
    public void BackgroundDiff(Image<Gray, Single> image, Image<Gray, Byte> mask) {
      if (!ModelsCreated)
        CreateModelsFromStats();

      CvInvoke.cvInRange(image.Ptr, low.Ptr, high.Ptr, mask.Ptr);
      // Inverts the results.
      CvInvoke.cvSubRS(mask.Ptr, new MCvScalar(255), mask.Ptr, IntPtr.Zero);
    }

    public override String ToString() {
      StringBuilder sb = new StringBuilder();
      sb.Append(String.Format("pixel diff average = {0}, std = {1}", avgDiff, stdDiff.v0));
      return sb.ToString();
    }

    /// <summary>
    /// Sets the high threshold of the background model for each pixel. Any value above that
    /// threashold for a particular pixel is considered foreground.
    /// High threshold = average value + average absolute difference * scale
    /// </summary>
    /// <param name="scale">the factor that multiplies the average frame-to-frame absolue 
    /// difference.</param>
    void SetHighThreshold(float scale) {
      CvInvoke.cvConvertScale(diff.Ptr, tmp1.Ptr, scale, 0);
      CvInvoke.cvAdd(avg.Ptr, tmp1.Ptr, high.Ptr, IntPtr.Zero);
    }

    /// <summary>
    /// Sets the low threshold of the background model for each pixel. Any value below that
    /// threashold for a particular pixel is considered foreground.
    /// Low threshold = average value - average absolute difference * scale
    /// </summary>
    /// <param name="scale">the factor that multiplies the average frame-to-frame absolue 
    /// difference.</param>
    void SetLowThreshold(float scale) {
      CvInvoke.cvConvertScale(diff.Ptr, tmp1.Ptr, scale, 0);
      CvInvoke.cvSub(avg.Ptr, tmp1.Ptr, low.Ptr, IntPtr.Zero);
    }
  }
}