using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.IO;
using System.Configuration;

using Kinect.Toolbox.Record;
using Kinect.Toolbox;

using Microsoft.Kinect;
using Microsoft.Win32;
using kinect = Microsoft.Kinect.Toolkit.FaceTracking;

using HandInput.Engine;
using HandInput.Util;

// Replay related interactions.
namespace HandInput.GesturesViewer {
  partial class MainWindow {
    static readonly int FPS = 30;
    static readonly int SampleRate = int.Parse(ConfigurationManager.AppSettings["sample_rate"]);
    DispatcherTimer timer;

    void replayButton_Click(object sender, RoutedEventArgs e) {
      OpenFileDialog openFileDialog = new OpenFileDialog {
        Title = "Select filename",
        Filter = "Replay files|*.bin"
      };

      if (openFileDialog.ShowDialog() == true) {
        if (replay != null) {
          replay.AllFramesReady -= replay_AllFramesReady;
          replay.Stop();
        }
        Stream recordStream = File.OpenRead(openFileDialog.FileName);

        Replay(recordStream);
      }
    }

    /// <summary>
    /// Starts new replay.
    /// </summary>
    /// <param name="recordStream"></param>
    void Replay(Stream recordStream) {
      CancelTracking();
      replay = new KinectAllFramesReplay(recordStream);
      frameSlider.Maximum = replay.FrameCount;
      frameSlider.Value = 0;

      handTracker = new SimpleSkeletonHandTracker(HandInputParams.DepthWidth, 
          HandInputParams.DepthHeight, replay.KinectParams);
      recogEngine = new RecognitionEngine(ModelFile);
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
          timer.Stop();
          recogEngine = null;
          replay = null;
        }
      }
    }

    void ReplayFrame(ReplayDepthImageFrame df, ReplayColorImageFrame cf,
        ReplaySkeletonFrame sf) {
      if (df != null)
        statusTextBox.Text = df.FrameNumber.ToString();
      colorManager.Update(cf, !displayDebug);
      depthManager.Update(df);
      UpdateSkeletonDisplay(sf);
      if (handTracker != null && recogEngine != null) {
        var result = handTracker.Update(depthManager.PixelData, colorManager.PixelData,
            SkeletonUtil.FirstTrackedSkeleton(sf.Skeletons));
        recogEngine.Update(result, true);
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

    void StopPlay() {
      if (timer != null && timer.IsEnabled) {
        timer.Stop();
      }
    }

    void StepForward() {
      frameSlider.Value += SampleRate;
    }

    void StopReplay() {
      if (timer != null && timer.IsEnabled)
        timer.Stop();

      if (replay != null) {
        replay.Dispose();
        replay = null;
      }
    }

    void replay_AllFramesReady(object sender, ReplayAllFramesReadyEventArgs e) {
      ReplayFrame(e.DepthImageFrame, e.ColorImageFrame, e.SkeletonFrame);
    }

    void replay_ColorImageFrameReady(object sender, ReplayColorImageFrameReadyEventArgs e) {
      if (displayDebug)
        return;

      colorManager.Update(e.ColorImageFrame);
    }

    void replay_SkeletonFrameReady(object sender, ReplaySkeletonFrameReadyEventArgs e) {
      UpdateSkeletonDisplay(e.SkeletonFrame);
    }
  }
}