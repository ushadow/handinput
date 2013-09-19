using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.IO;
using System.Windows.Input;
using System.Threading.Tasks.Dataflow;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Media;

using Microsoft.Kinect;
using Microsoft.Win32;

using Kinect.Toolbox;
using Kinect.Toolbox.Record;
using Kinect.Toolbox.Voice;

using Common.Logging;

using HandInput.Engine;
using HandInput.Util;

namespace GesturesViewer {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow {
    static readonly ILog Log = LogManager.GetCurrentClassLogger();
    static readonly ColorImageFormat ColorImageFormat = ColorImageFormat.RgbResolution640x480Fps30;
    static readonly DepthImageFormat DepthImageFormat = DepthImageFormat.Resolution640x480Fps30;
    static readonly int DepthWidth = 640, DepthHeight = 480;

    readonly ColorStreamManager colorManager = new ColorStreamManager();
    readonly DepthDisplayManager depthManager = new DepthDisplayManager();
    readonly TrainingManager trainingManager = new TrainingManager();
    readonly ContextTracker contextTracker = new ContextTracker();


    KinectSensor kinectSensor;

    AudioStreamManager audioManager;
    SkeletonDisplayManager skeletonDisplayManager;
    EyeTracker eyeTracker;
    bool displayDepth = false;

    KinectRecorder recorder;
    KinectAllFramesReplay replay;

    BindableNUICamera nuiCamera;

    int depthFrameNumber;
    BlockingCollection<KinectDataPacket> buffer = new BlockingCollection<KinectDataPacket>();
    Thread handTrackerThread;
    SaliencyDetector handTracker;
    FPSCounter fpsCounter = new FPSCounter();

    public MainWindow() {
      InitializeComponent();
    }

    void Kinects_StatusChanged(object sender, StatusChangedEventArgs e) {
      switch (e.Status) {
        case KinectStatus.Connected:
          if (kinectSensor == null) {
            kinectSensor = e.Sensor;
            Initialize();
          }
          break;
        case KinectStatus.Disconnected:
          if (kinectSensor == e.Sensor) {
            Clean();
            MessageBox.Show("Kinect was disconnected");
          }
          break;
        case KinectStatus.NotReady:
          break;
        case KinectStatus.NotPowered:
          if (kinectSensor == e.Sensor) {
            Clean();
            MessageBox.Show("Kinect is no more powered");
          }
          break;
        default:
          MessageBox.Show("Unhandled Status: " + e.Status);
          break;
      }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) {
      this.Activate();
      try {
        //listen to any status change for Kinects
        KinectSensor.KinectSensors.StatusChanged += Kinects_StatusChanged;

        //loop through all the Kinects attached to this PC, and start the first that is connected without an error.
        foreach (KinectSensor kinect in KinectSensor.KinectSensors) {
          if (kinect.Status == KinectStatus.Connected) {
            kinectSensor = kinect;
            break;
          }
        }

        if (KinectSensor.KinectSensors.Count == 0)
          MessageBox.Show("No Kinect found");
        else
          Initialize();

      } catch (Exception ex) {
        MessageBox.Show(ex.Message);
      }
    }

    private void Initialize() {
      if (kinectSensor == null)
        return;

      audioManager = new AudioStreamManager(kinectSensor.AudioSource);
      audioBeamAngle.DataContext = audioManager;

      kinectSensor.ColorStream.Enable(ColorImageFormat);

      kinectSensor.DepthStream.Enable(DepthImageFormat);

      kinectSensor.SkeletonStream.Enable(new TransformSmoothParameters {
        Smoothing = 0.5f,
        Correction = 0.5f,
        Prediction = 0.5f,
        JitterRadius = 0.05f,
        MaxDeviationRadius = 0.04f
      });

      kinectSensor.AllFramesReady += kinectRuntime_AllFrameReady;

      skeletonDisplayManager = new SkeletonDisplayManager(kinectSensor, kinectCanvas);

      kinectSensor.Start();

      nuiCamera = new BindableNUICamera(kinectSensor);

      elevationSlider.DataContext = nuiCamera;

      kinectDisplay.DataContext = colorManager;
      handTrackerThread = new Thread(new ThreadStart(StartHandTracker));
      handTrackerThread.Start();
    }

    void StartHandTracker() {
      handTracker = new SaliencyDetector(DepthWidth, DepthHeight, kinectSensor.CoordinateMapper);
      while (kinectSensor != null && kinectSensor.IsRunning) {
        var data = buffer.Take();
        handTracker.detect(data.DepthData, data.ColorData, data.Skeleton);
        fpsCounter.ComputeFPS();
      }
    }

    void UpdateDisplay() {
      gesturesCanvas.Children.Clear();
      if (handTracker.PrevBoundingBox.Width > 0) {
        VisualUtil.DrawRectangle(gesturesCanvas, handTracker.PrevBoundingBox, Brushes.Red);
      }
      depthManager.Update(handTracker.SmoothedDepth.Bytes, DepthWidth, DepthHeight);
    }

    void UpdateDepthFrame(ReplayDepthImageFrame frame) {
      depthManager.Update(frame);
    }

    void kinectRuntime_AllFrameReady(object sender, AllFramesReadyEventArgs e) {
      if (replay != null && !replay.IsFinished)
        return;

      using (var cf = e.OpenColorImageFrame())
      using (var df = e.OpenDepthImageFrame())
      using (var sf = e.OpenSkeletonFrame()) {
        try {
          if (recorder != null) {
            recorder.Record(sf, df, cf);
          }
        } catch (ObjectDisposedException) { }

        if (cf != null)
          UpdateColorFrame(cf);

        if (df != null) {
          depthFrameNumber = df.FrameNumber;
          UpdateDepthFrame(df);
        }

        if (sf != null) {
          UpdateSkeletonFrame(sf);
          if (buffer.Count <= 1)
            buffer.Add(new KinectDataPacket {
              ColorData = colorManager.PixelData,
              DepthData = depthManager.PixelData,
              Skeleton = SkeletonUtil.FirstTrackedSkeleton(sf.GetSkeletons())
            });
        }
      }
      UpdateDisplay();
    }

    /// <summary>
    /// Updates the color pixel data and updates the display if necessary.
    /// </summary>
    /// <param name="frame">frame is not null.</param>
    void UpdateColorFrame(ReplayColorImageFrame frame) {
      colorManager.Update(frame, !displayDepth);
    }

    void UpdateSkeletonFrame(ReplaySkeletonFrame frame) {
      ProcessFrame(frame);
    }

    void ProcessFrame(ReplaySkeletonFrame frame) {
      Dictionary<int, string> stabilities = new Dictionary<int, string>();
      foreach (var skeleton in frame.Skeletons) {
        if (skeleton.TrackingState != SkeletonTrackingState.Tracked)
          continue;

        contextTracker.Add(skeleton.Position.ToVector3(), skeleton.TrackingId);
        stabilities.Add(skeleton.TrackingId,
            contextTracker.IsStableRelativeToCurrentSpeed(skeleton.TrackingId) ? "Stable" : "Non stable");
      }

      try {
        skeletonDisplayManager.Draw(frame.Skeletons, seatedMode.IsChecked == true);
      } catch (Exception) {

      }

      stabilitiesList.ItemsSource = stabilities;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
      Clean();
      handTrackerThread.Abort();
      handTrackerThread.Join();
    }

    private void Clean() {
      if (audioManager != null) {
        audioManager.Dispose();
        audioManager = null;
      }

      if (recorder != null) {
        recorder.Stop();
        recorder = null;
      }

      if (eyeTracker != null) {
        eyeTracker.Dispose();
        eyeTracker = null;
      }

      if (kinectSensor != null) {
        kinectSensor.AllFramesReady -= kinectRuntime_AllFrameReady;
        kinectSensor.Stop();
        kinectSensor = null;
      }
    }

    private void replayButton_Click(object sender, RoutedEventArgs e) {
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

    void replay_AllFramesReady(object sender, ReplayAllFramesReadyEventArgs e) {
      ReplayFrame(e.DepthImageFrame, e.ColorImageFrame, e.SkeletonFrame);
    }

    void replay_ColorImageFrameReady(object sender, ReplayColorImageFrameReadyEventArgs e) {
      if (displayDepth)
        return;

      colorManager.Update(e.ColorImageFrame);
    }

    void replay_SkeletonFrameReady(object sender, ReplaySkeletonFrameReadyEventArgs e) {
      ProcessFrame(e.SkeletonFrame);
    }

    private void Button_Click(object sender, RoutedEventArgs e) {
      displayDepth = !displayDepth;

      if (displayDepth) {
        viewButton.Content = "View Color";
        kinectDisplay.DataContext = depthManager;
      } else {
        viewButton.Content = "View Depth";
        kinectDisplay.DataContext = colorManager;
      }
    }

    private void nearMode_Checked_1(object sender, RoutedEventArgs e) {
      if (kinectSensor == null)
        return;

      kinectSensor.DepthStream.Range = DepthRange.Near;
      kinectSensor.SkeletonStream.EnableTrackingInNearRange = true;
    }

    private void nearMode_Unchecked_1(object sender, RoutedEventArgs e) {
      if (kinectSensor == null)
        return;

      kinectSensor.DepthStream.Range = DepthRange.Default;
      kinectSensor.SkeletonStream.EnableTrackingInNearRange = false;
    }

    private void seatedMode_Checked_1(object sender, RoutedEventArgs e) {
      if (kinectSensor == null)
        return;

      kinectSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
    }

    private void seatedMode_Unchecked_1(object sender, RoutedEventArgs e) {
      if (kinectSensor == null)
        return;

      kinectSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
      switch (e.Key) {
        case Key.Space:
          RecordGesture();
          break;
        case Key.P:
          Pause();
          break;
        case Key.S:
          Start();
          break;
        default:
          break;
      }
    }
  }
}
