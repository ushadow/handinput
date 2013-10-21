using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using handinput;
using System.Drawing;

namespace HandInput.Engine {
  public class RecognitionEngine {

    MProcessor processor = new MProcessor(Parameters.FeatureImageWidth, 
        Parameters.FeatureImageWidth, Parameters.ModelFile);
    bool reset = true;

    public void Update(TrackingResult result) {
      if (result.RelPos.IsSome && result.BoundingBox.IsSome) {
        var pos = result.RelPos.Value;
        var image = result.SmoothedDepth;
        image.ROI = result.BoundingBox.Value;
        processor.Update((float)pos.X, (float)pos.Y, (float)pos.Z, image.Ptr);
        image.ROI = Rectangle.Empty;
        reset = false;
      } else {
        if (!reset) {
          reset = true;
          processor.Reset();
        }
      }
    }
  }
}