using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Drawing;
using System.IO;
using window = System.Windows;

using Newtonsoft.Json;

using handinput;

using HandInput.Util;
using System.Windows.Media.Media3D;

namespace HandInput.Engine {
  public class GestureRecognitionEngine {

    MProcessor processor;
    bool reset = true;
    String modelFile;

    public GestureRecognitionEngine(String modelFile) {
      this.modelFile = modelFile;
      processor = new MProcessor(HandInputParams.FeatureImageWidth,
          HandInputParams.FeatureImageWidth, modelFile);
    }

    public int GetSampleRate() {
      return processor.KinectSampleRate();
    }

    public String Update(TrackingResult result, bool visualize = false) {
      String gesture = "";
      if (result.RightHandRelPos.IsSome && result.DepthBoundingBoxes.Count > 0) {
        var pos = result.RightHandRelPos.Value;
        var image = result.DepthImage;
        var skin = result.ColorImage;
        image.ROI = result.DepthBoundingBoxes.Last();
        gesture = processor.Update((float)pos.X, (float)pos.Y, (float)pos.Z, image.Ptr,
                                    skin.Ptr, visualize);
        image.ROI = Rectangle.Empty;
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