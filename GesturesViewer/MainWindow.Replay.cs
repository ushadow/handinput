using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.IO;

using Kinect.Toolbox.Record;
using Kinect.Toolbox;
using Microsoft.Kinect;

using HandInput.Engine;
using HandInput.Util;
using Microsoft.Win32;

// Replay related interactions.
namespace HandInput.GesturesViewer {
  partial class MainWindow {
    static readonly int FPS = 30;
    DispatcherTimer timer;

    void replayButton_Click(object sender, RoutedEventArgs e) {
      OpenFileDialog openFileDialog = new OpenFileDialog {
        Title = "Select filename",
        Filter = "Replay files|*.replay"
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

      handTracker = new HandInput.Engine.SaliencyDetector(DepthWidth, DepthHeight,
                                                          replay.CoordinateMapper);
      featureProcessor = new SalientFeatureProcessor();
      timer = new DispatcherTimer();
      timer.Interval = new TimeSpan(0, 0, 0, 0, (1000 / FPS));
      timer.Tick += new EventHandler(OnTimerTick);
      timer.Start();
    }

    void OnTimerTick(object sender, EventArgs e) {
      frameSlider.Value += 1;
    }

    void frameSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
      int index = (int)e.NewValue;
      var frame = replay.FrameAt(index);
      if (frame != null)
        ReplayFrame(frame.DepthImageFrame, frame.ColorImageFrame, frame.SkeletonFrame);
      else {
        timer.Stop();
        featureProcessor = null;
      }
    }

    void ReplayFrame(ReplayDepthImageFrame df, ReplayColorImageFrame cf,
        ReplaySkeletonFrame sf) {
      if (df != null)
        statusTextBox.Text = df.FrameNumber.ToString();
      depthDisplayManager.Update(df);
      colorManager.Update(cf, !displayDepth);
      UpdateSkeletonDisplay(sf);
      var relPos = handTracker.detect(depthDisplayManager.PixelData, colorManager.PixelData,
          SkeletonUtil.FirstTrackedSkeleton(sf.Skeletons));
      featureProcessor.Compute(relPos, handTracker.SmoothedDepth, handTracker.PrevBoundingBox);
      fpsCounter.LogFPS();
      UpdateDisplay();
    }

    void TogglePlay() {
      if (timer != null) {
        if (timer.IsEnabled)
          timer.Stop();
        else
          timer.Start();
      }
    }

    void StopReply() {
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
      if (displayDepth)
        return;

      colorManager.Update(e.ColorImageFrame);
    }

    void replay_SkeletonFrameReady(object sender, ReplaySkeletonFrameReadyEventArgs e) {
      UpdateSkeletonDisplay(e.SkeletonFrame);
    }
  }
}