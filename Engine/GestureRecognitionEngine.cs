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
  public class GestureRecognitionEngine : IDisposable {

    MProcessor processor;
    bool reset = true;

    public GestureRecognitionEngine(String modelFile) {
      Init(modelFile);
    }

    public int GetSampleRate() {
      return processor.KinectSampleRate();
    }

    /// <summary>
    /// Synchronized method.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="visualize"></param>
    /// <returns></returns>
    public String Update(TrackingResult result, bool visualize = false) {
      String gesture = "";
      if (result.RightHandRelPos.IsSome && result.DepthBoundingBoxes.Count > 0) {
        var pos = result.RightHandRelPos.Value;
        var image = result.DepthImage;
        var skin = result.ColorImage;
        image.ROI = result.DepthBoundingBoxes.Last();
        lock (this) {
          gesture = processor.Update((float)pos.X, (float)pos.Y, (float)pos.Z, image.Ptr,
                                      skin.Ptr, visualize);
          reset = false;
        }
        image.ROI = Rectangle.Empty;
      } else {
        // No hand detected (hand may be out of field of view).
        if (!reset) {
          reset = true;
          lock (this) {
            processor.Reset();
          }
        }
      }
      return gesture;
    }

    /// <summary>
    /// Synchronized method.
    /// </summary>
    /// <param name="modelFile"></param>
    public void ResetModel(String modelFile) {
      Dispose();
      lock (this) {
        Init(modelFile);
      }
    }

    /// <summary>
    /// Synchronized method.
    /// </summary>
    public void Dispose() {
      lock (this) {
        if (processor != null) {
          processor.Dispose();
          processor = null;
        }
      }
    }

    private void Init(String modelFile) {
      processor = new MProcessor(HandInputParams.FeatureImageWidth,
          HandInputParams.FeatureImageWidth, modelFile);
      reset = true;
    }

  }
}