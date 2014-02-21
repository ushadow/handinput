using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.IO;
using System.Configuration;
using System.Text.RegularExpressions;

using Kinect.Toolbox.Record;
using Kinect.Toolbox;

using Microsoft.Kinect;
using Microsoft.Win32;
using kinect = Microsoft.Kinect.Toolkit.FaceTracking;

using HandInput.Engine;
using HandInput.Util;

// Replay related interactions.
namespace GesturesViewer {
  partial class MainWindow {
    static readonly int FPS = 50;
    static readonly String IpAddress = ConfigurationManager.AppSettings["ip"];
    static readonly int Port = int.Parse(ConfigurationManager.AppSettings["port"]);

    DispatcherTimer timer;
    GroundTruthDataRelayer gtReplayer;

    float sampleRate = 1;

    void replayButton_Click(object sender, RoutedEventArgs e) {
      OpenFileDialog openFileDialog = new OpenFileDialog {
        Title = "Select filename",
        Filter = "Replay files|*.bin"
      };

      if (openFileDialog.ShowDialog() == true) {
        if (replay != null) {
          replay.Dispose();
        }
        var fullPath = openFileDialog.FileName;
        Stream recordStream = File.OpenRead(fullPath);
        Stream gtStream = null;

        var fileName = Path.GetFileName(fullPath);
        var dir = Path.GetDirectoryName(fullPath);
        var match = Regex.Match(fileName, TrainingManager.KinectDataRegex);
        if (match.Success) {
          var batchIndex = Int32.Parse(match.Groups[1].Value);
          var gtFilePath = Path.Combine(dir,
              String.Format(TrainingManager.KinectGTDPattern, batchIndex));
          gtStream = File.OpenRead(gtFilePath);
        }
        Replay(recordStream, gtStream);
      }
    }

    /// <summary>
    /// Starts new replay.
    /// </summary>
    /// <param name="recordStream"></param>
    void Replay(Stream recordStream, Stream gtStream) {
      // Reset slider.
      frameSlider.Value = 0;

      if (gtStream != null)
        gtReplayer = new GroundTruthDataRelayer(gtStream);

      lock (this) {
        replay = new KinectAllFramesReplay(recordStream);
        frameSlider.Maximum = replay.GetFramesCount();

        handTracker = new SimpleSkeletonHandTracker(HandInputParams.DepthWidth,
            HandInputParams.DepthHeight, replay.GetKinectParams());
      }

      Log.DebugFormat("model file: {0}", ModelFile);
      recogEngine = new GestureRecognitionEngine(ModelFile);
      sampleRate = recogEngine.GetSampleRate();
      timer = new DispatcherTimer();
      timer.Interval = new TimeSpan(0, 0, 0, 0, (1000 / FPS));
      timer.Tick += new EventHandler(OnTimerTick);
      timer.Start();
    }

    void OnTimerTick(object sender, EventArgs e) {
      StepForward();
    }

    void frameSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
      int index = (int)e.NewValue;
      if (replay != null) {
        var frame = replay.FrameAt(index);
        if (frame != null) {
          ReplayFrame(frame.DepthImageFrame, frame.ColorImageFrame, frame.SkeletonFrame);
        } else {
          Log.DebugFormat("Obtained a null frame.");
          StopReplay();
        }
      }
    }

    void ReplayFrame(ReplayDepthImageFrame df, ReplayColorImageFrame cf,
        ReplaySkeletonFrame sf) {
      if (df != null) {
        labelFrameNumber.Content = df.FrameNumber.ToString();
        if (gtReplayer != null) {
          var data = gtReplayer.GetDataFrame(df.FrameNumber);
          if (data != null) {
            UpdateGroundTruthDisplay(data);
          }
        }
      }
      colorManager.Update(cf, !displayDebug);
      depthManager.Update(df);
      UpdateSkeletonDisplay(sf);
      if (handTracker != null && recogEngine != null) {
        var result = handTracker.Update(depthManager.PixelData, colorManager.PixelData,
            SkeletonUtil.FirstTrackedSkeleton(sf.Skeletons));
        var gesture = recogEngine.Update(result, viewHog);

        lock (gestureServer)
          gestureServer.Send(gesture);

        statusTextBox.Text = gesture;
        fpsCounter.LogFPS();
        UpdateDisplay(result);
      }
    }

    void TogglePlay() {
      if (timer != null) {
        if (timer.IsEnabled)
          timer.Stop();
        else
          timer.Start();
      }
    }

    void StepForward() {
      frameSlider.Value += sampleRate;
    }

    /// <summary>
    /// Stops replaying if it is on.
    /// </summary>
    void StopReplay() {
      if (timer != null && timer.IsEnabled)
        timer.Stop();

      lock (this) {
        if (replay != null) {
          replay.Dispose();
          replay = null;
          Log.Debug("Set replay to null.");
        }
      }

      recogEngine = null;
    }

    void UpdateGroundTruthDisplay(GroundTruthData data) {
      labelPhaseVal.Content = data.PhaseLabel;
      labelGestureVal.Content = data.GestureLabel;
    }
  }
}