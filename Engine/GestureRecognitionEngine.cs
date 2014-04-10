using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Windows.Media.Media3D;
using window = System.Windows;

using Newtonsoft.Json;
using Common.Logging;

using handinput;

using HandInput.Util;

namespace HandInput.Engine {
  public class GestureRecognitionEngine : IDisposable {
    private static readonly ILog Log = LogManager.GetCurrentClassLogger();

    String modelFile;
    MProcessor processor;
    bool reset = true;

    public GestureRecognitionEngine(String modelFile) {
      this.modelFile = modelFile;
      Init();
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
      lock (this) {
        Dispose();
        this.modelFile = modelFile;
        Init();
      }
    }

    /// <summary>
    /// Synchronized method.
    /// </summary>
    public void Reset() {
      lock (this) {
        Dispose();
        Init();
      }
    }

    public void Dispose() {
      if (processor != null) {
        processor.Dispose();
        processor = null;
      }
    }

    private void Init() {
      processor = new MProcessor(HandInputParams.FeatureImageWidth,
          HandInputParams.FeatureImageWidth, modelFile);
      reset = true;
      Log.DebugFormat("Initialized with model file: {0}", modelFile);
    }

  }
}