using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Media;
using System.Configuration;
using System.Windows.Threading;
using System.Linq;
using drawing = System.Drawing;

using Microsoft.Kinect;

using Kinect.Toolbox;
using Kinect.Toolbox.Record;

using Common.Logging;

using HandInput.Engine;
using HandInput.Util;


namespace HandInput.GesturesViewer {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow {
    enum DisplayOption { DEPTH, COLOR };

    static readonly ILog Log = LogManager.GetCurrentClassLogger();
    static readonly String ModelFile = ConfigurationManager.AppSettings["model_file"];

    readonly ColorStreamManager colorManager = new ColorStreamManager();
    readonly DepthStreamManager depthManager = new DepthStreamManager();
    readonly DebugDisplayManager debugDisplayManager = new DebugDisplayManager(
        HandInputParams.DepthWidth, HandInputParams.DepthHeight);
    readonly TrainingManager trainingManager = new TrainingManager();
    readonly ContextTracker contextTracker = new ContextTracker();

    KinectSensor kinectSensor;

    AudioStreamManager audioManager;
    SkeletonDisplayManager skeletonDisplayManager;
    bool displayDebug = false;
    DisplayOption displayOption = DisplayOption.DEPTH;

    KinectRecorder recorder;
    KinectAllFramesReplay replay;

    int depthFrameNumber;
    BlockingCollection<KinectDataPacket> buffer = new BlockingCollection<KinectDataPacket>();
    CancellationTokenSource cancellationTokenSource;
    IHandTracker handTracker;
    RecognitionEngine recogEngine;
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

    void Window_Loaded(object sender, RoutedEventArgs e) {
      this.Activate();
      try {
        // listen to any status change for Kinects
        KinectSensor.KinectSensors.StatusChanged += Kinects_StatusChanged;

        if (KinectSensor.KinectSensors.Count == 0) {
          MessageBox.Show("No Kinect found");
          return;
        }

        // loop through all the Kinects attached to this PC, and start the first that is connected 
        // without an error.
        foreach (KinectSensor kinect in KinectSensor.KinectSensors) {
          if (kinect.Status == KinectStatus.Connected) {
            kinectSensor = kinect;
            break;
          }
        }

        Initialize();

      } catch (Exception ex) {
        MessageBox.Show(ex.Message);
      }
    }

    void Initialize() {
      if (kinectSensor == null)
        return;

      Log.InfoFormat("Color stream nominal focal length in pixel = {0}",
          kinectSensor.ColorStream.NominalFocalLengthInPixels);
      Log.InfoFormat("Depth stream nominal focal length in pixel = {0}",
          kinectSensor.DepthStream.NominalFocalLengthInPixels);

      audioManager = new AudioStreamManager(kinectSensor.AudioSource);
      audioBeamAngle.DataContext = audioManager;

      kinectSensor.ColorStream.Enable(HandInputParams.ColorImageFormat);

      kinectSensor.DepthStream.Enable(HandInputParams.DepthImageFormat);

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

      kinectDisplay.DataContext = colorManager;
      maskDispay.DataContext = debugDisplayManager;
      depthDisplay.DataContext = depthManager;

      HandInputParams.ColorFocalLength = kinectSensor.ColorStream.NominalFocalLengthInPixels;
      HandInputParams.DepthFocalLength = kinectSensor.DepthStream.NominalFocalLengthInPixels;
      //faceTracker = new kinect.FaceTracker(kinectSensor);
    }

    void StartTracking() {
      cancellationTokenSource = new CancellationTokenSource();
      StopReplay();
      var token = cancellationTokenSource.Token;
      Task.Factory.StartNew(() => HandTrackingTask(token), token);
    }

    void HandTrackingTask(CancellationToken token) {
      Log.Debug("Start tracking");
      handTracker = new SimpleSkeletonHandTracker(HandInputParams.DepthWidth,
          HandInputParams.DepthHeight, kinectSensor.CoordinateMapper);
      recogEngine = new RecognitionEngine(ModelFile);
      while (kinectSensor != null && kinectSensor.IsRunning && !token.IsCancellationRequested) {
        var data = buffer.Take();
        var result = handTracker.Update(data.DepthData, data.ColorData, data.Skeleton);
        int gestureIndex = recogEngine.Update(result);
        Dispatcher.Invoke(DispatcherPriority.Normal, new Action<TrackingResult>(UpdateDisplay),
          result);
        if (gestureIndex >= 1 && gestureIndex <= 2)
          Dispatcher.Invoke(DispatcherPriority.Normal, new Action<String>(SetStatus),
              Gestures[gestureIndex]);
        fpsCounter.LogFPS();
      }
    }

    void SetStatus(String status) {
      statusTextBox.AppendText(status + '\n');
      statusTextBox.ScrollToEnd();
    }

    void CancelTracking() {
      if (cancellationTokenSource != null)
        cancellationTokenSource.Cancel();
    }

    void UpdateDisplay(TrackingResult result) {
      colorCanvas.Children.Clear();
      depthCanvas.Children.Clear();
      if (result.ColorBoundingBoxes.Count > 0) {
        VisualUtil.DrawRectangle(colorCanvas, result.ColorBoundingBoxes.Last(), Brushes.Red,
            (float)colorCanvas.ActualWidth / HandInputParams.ColorWidth);
        VisualUtil.DrawRectangle(depthCanvas, result.DepthBoundingBoxes.Last(), Brushes.Red,
                    (float)depthCanvas.ActualWidth / HandInputParams.DepthWidth);
      }
      if (handTracker != null) {
        if (handTracker is SalienceHandTracker)
          UpdateSalienceHandTrackerDisplay();
        else if (handTracker is StipHandTracker)
          UpdateStipHandTrackerDisplay();
        else if (handTracker is SimpleSkeletonHandTracker) {
          UpdateSimpleHandTrackerDisplay();
        }
      }
      if (displayDebug) {
        if (displayOption == DisplayOption.DEPTH && result.DepthImage != null)
          debugDisplayManager.UpdateBitmap(result.DepthImage.Bytes);
        if (displayOption == DisplayOption.COLOR && result.ColorImage != null)
          debugDisplayManager.UpdateBitmap(result.ColorImage.Bytes);
      }
    }

    void UpdateSimpleHandTrackerDisplay() {
      SimpleSkeletonHandTracker ssht = (SimpleSkeletonHandTracker)handTracker;
      VisualUtil.DrawRectangle(colorCanvas, ssht.InitialHandRect, Brushes.Green,
          (float)colorCanvas.ActualWidth / HandInputParams.ColorWidth);
    }

    void UpdateStipHandTrackerDisplay() {
      StipHandTracker sht = (StipHandTracker)handTracker;
      foreach (drawing.Point p in sht.InterestPoints) {
        VisualUtil.DrawCircle(colorCanvas, new Point(p.X, p.Y), Brushes.Red, 1, 5);
      }
    }

    void UpdateSalienceHandTrackerDisplay() {
      SalienceHandTracker sht = (SalienceHandTracker)handTracker;
      var bb = sht.PrevBoundingBoxes;
      if (bb.Count > 0) {
        VisualUtil.DrawRectangle(colorCanvas, bb.Last(), Brushes.Red);
      }
      var converted = sht.TemporalSmoothed.ConvertScale<Byte>(255, 0);
      debugDisplayManager.UpdateBitmap(converted.Bytes);
      debugDisplayManager.UpdateBitmapMask(sht.SaliencyProb.Data);
    }

    void kinectRuntime_AllFrameReady(object sender, AllFramesReadyEventArgs e) {
      if (replay != null && !replay.IsFinished)
        return;

      using (var cf = e.OpenColorImageFrame())
      using (var df = e.OpenDepthImageFrame())
      using (var sf = e.OpenSkeletonFrame()) {
        try {
          if (recorder != null && sf != null && df != null && cf != null) {
            recorder.Record(sf, df, cf);
          }
        } catch (ObjectDisposedException) { }

        if (cf != null)
          colorManager.Update(cf, !displayDebug);

        if (df != null) {
          depthFrameNumber = df.FrameNumber;
          depthManager.Update(df);
        }

        if (sf != null) {
          UpdateSkeletonDisplay(sf);
          if (buffer.Count <= 1)
            buffer.Add(new KinectDataPacket {
              ColorData = colorManager.PixelData,
              DepthData = depthManager.PixelData,
              Skeleton = SkeletonUtil.FirstTrackedSkeleton(sf.GetSkeletons())
            });
        }
      }
    }

    void UpdateSkeletonDisplay(ReplaySkeletonFrame frame) {
      if (frame.Skeletons == null)
        return;
      Dictionary<int, string> stabilities = new Dictionary<int, string>();
      foreach (var skeleton in frame.Skeletons) {
        if (skeleton.TrackingState != SkeletonTrackingState.Tracked)
          continue;

        contextTracker.Add(skeleton.Position.ToVector3(), skeleton.TrackingId);
        stabilities.Add(skeleton.TrackingId,
            contextTracker.IsStableRelativeToCurrentSpeed(skeleton.TrackingId) ?
                                                          "Stable" : "Non stable");
      }

      try {
        skeletonDisplayManager.Draw(frame.Skeletons, seatedMode.IsChecked == true);
      } catch (Exception e) {
        Log.Error(e.Message);
      }

      stabilitiesList.ItemsSource = stabilities;
    }

    void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
      Clean();
    }

    void Clean() {
      CancelTracking();

      if (audioManager != null) {
        audioManager.Dispose();
        audioManager = null;
      }

      if (recorder != null) {
        recorder.Close();
        recorder = null;
      }

      if (kinectSensor != null) {
        kinectSensor.AllFramesReady -= kinectRuntime_AllFrameReady;
        kinectSensor.Stop();
        kinectSensor = null;
      }
    }

    void Button_Click(object sender, RoutedEventArgs e) {
      displayDebug = !displayDebug;

      if (displayDebug) {
        viewButton.Content = "View Color";
        kinectDisplay.DataContext = debugDisplayManager;
      } else {
        viewButton.Content = "View Debug";
        kinectDisplay.DataContext = colorManager;
      }
    }

    void nearMode_Checked_1(object sender, RoutedEventArgs e) {
      if (kinectSensor == null)
        return;

      kinectSensor.DepthStream.Range = DepthRange.Near;
      kinectSensor.SkeletonStream.EnableTrackingInNearRange = true;
    }

    void nearMode_Unchecked_1(object sender, RoutedEventArgs e) {
      if (kinectSensor == null)
        return;

      kinectSensor.DepthStream.Range = DepthRange.Default;
      kinectSensor.SkeletonStream.EnableTrackingInNearRange = false;
    }

    void seatedMode_Checked_1(object sender, RoutedEventArgs e) {
      if (kinectSensor == null)
        return;

      kinectSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
    }

    void seatedMode_Unchecked_1(object sender, RoutedEventArgs e) {
      if (kinectSensor == null)
        return;

      kinectSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
    }

    void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
      switch (e.Key) {
        case Key.Space:
          RecordGesture();
          break;
        case Key.P:
          TogglePlay();
          break;
        case Key.T:
          StartTracking();
          break;
        case Key.N:
          StepForward();
          break;
        case Key.C:
          displayOption = DisplayOption.COLOR;
          break;
        case Key.D:
          displayOption = DisplayOption.DEPTH;
          break;
        default:
          break;
      }
    }
  }
}
