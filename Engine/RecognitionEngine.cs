using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Drawing;

using handinput;
using HandInput.Util;

namespace HandInput.Engine {
  public class RecognitionEngine {

    MProcessor processor;
    bool reset = true;
    String modelFile;

    public RecognitionEngine(String modelFile) {
      this.modelFile = modelFile;
      processor = new MProcessor(HandInputParams.FeatureImageWidth, 
          HandInputParams.FeatureImageWidth, modelFile);
    }

    public String Update(TrackingResult result, bool visualize = false) {
      String gesture = "";
      if (result.RelPos.IsSome && result.DepthBoundingBoxes.Count > 0) {
        var pos = result.RelPos.Value;
        var image = result.DepthImage;
        var skin = result.ColorImage;
        image.ROI = result.DepthBoundingBoxes.Last();
        skin.ROI = result.ColorBoundingBoxes.Last();
        gesture = processor.Update((float)pos.X, (float)pos.Y, (float)pos.Z, image.Ptr, 
                                    skin.Ptr, visualize);
        image.ROI = Rectangle.Empty;
        skin.ROI = Rectangle.Empty;
        reset = false;
      } else {
        if (!reset) {
          reset = true;
          processor.Reset();
        }
      }
      return gesture;
    }
  }
}