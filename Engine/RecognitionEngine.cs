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

namespace HandInput.Engine {
  public class RecognitionEngine {

    static String ToJson(String gestureJson, Option<window.Point> rightHandPos) {
      StringWriter sw = new StringWriter();
      JsonTextWriter writer = new JsonTextWriter(sw);

      writer.WriteStartObject();
      writer.WritePropertyName("gestureEvent");
      writer.WriteValue(gestureJson);

      if (rightHandPos.IsSome) {
        writer.WritePropertyName("rightHandPos");

        var pos = rightHandPos.Value;
        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteValue((int)pos.X);
        writer.WritePropertyName("y");
        writer.WriteValue((int)pos.Y);
        writer.WriteEndObject();

      }

      writer.WriteEndObject();
      return sw.ToString();
    }

    MProcessor processor;
    bool reset = true;
    String modelFile;

    public RecognitionEngine(String modelFile) {
      this.modelFile = modelFile;
      processor = new MProcessor(HandInputParams.FeatureImageWidth,
          HandInputParams.FeatureImageWidth, modelFile);
    }

    public int GetSampleRate() {
      return processor.KinectSampleRate();
    }

    public String Update(TrackingResult result, bool visualize = false) {
      String gesture = "";
      if (result.RelPos.IsSome && result.DepthBoundingBoxes.Count > 0) {
        var pos = result.RelPos.Value;
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
      return ToJson(gesture, result.RightHandAbsPos);
    }

  }
}