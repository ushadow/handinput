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
using System.Text;
using System.IO;
using System.Windows.Data;
using System.Windows.Controls;

namespace GesturesViewer {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow {
    enum DisplayOption { DEPTH, COLOR };

    static readonly ILog Log = LogManager.GetCurrentClassLogger();
    static readonly String ModelDir = Path.GetFullPath(
        ConfigurationManager.AppSettings["model_dir"]);
    static readonly int NOptionPerLine = 5;

    readonly ColorStreamManager colorManager = new ColorStreamManager();
    readonly DepthStreamManager depthManager = new DepthStreamManager();
    SkeletonDisplayManager skeletonDisplayManager;
    readonly DebugDisplayManager debugColorDisplayManager = new DebugDisplayManager(
        HandInputParams.ColorWidth, HandInputParams.ColorHeight);
    readonly DebugDisplayManager debugDepthDisplayManager = new DebugDisplayManager(
        HandInputParams.DepthWidth, HandInputParams.DepthHeight);
    readonly TrainingManager trainingManager = new TrainingManager();
    readonly ContextTracker contextTracker = new ContextTracker();
    
    AudioStreamManager audioManager;

    IDictionary<Key, Action> keyActions;

    // Display options.
    bool displayDebug = false;
    DisplayOption displayOption = DisplayOption.DEPTH;
    bool viewHog = false;
    bool viewSkeleton = true;

    KinectSensor kinectSensor;
    KinectRecorder recorder;
    KinectAllFramesReplay replay;
    IHandTracker handTracker;
    GestureRecognitionEngine recogEngine;
    GestureServer inputServer = new GestureServer(IpAddress, Port);

    int depthFrameNumber;
    BlockingCollection<KinectDataPacket> buffer = new BlockingCollection<KinectDataPacket>();
    FPSCounter fpsCounter = new FPSCounter();
    ModelSelector modelSelector = new ModelSelector(ModelDir);

    /// <summary>
    /// Initializes UI.
    /// </summary>
    public MainWindow() {
      InitializeComponent();
      keyActions = new Dictionary<Key, Action>() {
        {Key.Space, RecordGesture}, {Key.D, ToggleDebugDisplayOption}, {Key.H, ToggleViewHog}, 
        {Key.K, ToggleViewSkeleton}, {Key.N, StepForward}, {Key.P, TogglePlay}, 
        {Key.S, StartKinect}, {Key.T, StartTracking}, 
      };
      labelKeys.Content = GetKeyOptionString();

      trainingManager.TrainingEvent += OnTrainingEvent;
      gestureComboBox.DataContext = trainingManager;
      repitionsTextBox.DataContext = trainingManager;
      pidTextBox.DataContext = trainingManager;
      showStopCheckBox.DataContext = trainingManager;

      var binding = new Binding("Status");
      binding.Mode = BindingMode.OneWay;
      binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
      statusTextBox.DataContext = trainingManager;
      statusTextBox.SetBinding(TextBox.TextProperty, binding);

      binding = new Binding("Status");
      binding.Mode = BindingMode.OneWay;
      binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
      binding.Converter = new ColorConverter();
      statusTextBox.SetBinding(TextBox.ForegroundProperty, binding);

      modelComboBox.DataContext = modelSelector;
      modelComboBox.SelectedItem = modelSelector.SelectedModel;

      inputServer.Start();
    }

    void Kinects_StatusChanged(object sender, StatusChangedEventArgs e) {
      switch (e.Status) {
        case KinectStatus.Connected:
          if (kinectSensor == null) {
            kinectSensor = e.Sensor;
            InitializeKinect();
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
        InitializeKinect();
      } catch (Exception ex) {
        MessageBox.Show(ex.Message);
      }
    }

    String GetKeyOptionString() {
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < keyActions.Count; i++) {
        var kvp = keyActions.ElementAt(i);
        sb.AppendFormat("{0}: {1}\t", kvp.Key, kvp.Value.Method.Name);
        if ((i + 1) % NOptionPerLine == 0)
          sb.Append('\n');
      }
      return sb.ToString();
    }

    /// <summary>
    /// Initialize Kinect.
    /// </summary>
    void InitializeKinect() {
      if (kinectSensor == null)
        return;

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
      skeletonDisplayManager = new SkeletonDisplayManager(kinectSensor.CoordinateMapper,
                                                          skeletonCanvas);
      kinectDisplay.DataContext = colorManager;
      maskDispay.DataContext = debugColorDisplayManager;
      depthDisplay.DataContext = depthManager;

      HandInputParams.ColorFocalLength = kinectSensor.ColorStream.NominalFocalLengthInPixels;
      HandInputParams.DepthFocalLength = kinectSensor.DepthStream.NominalFocalLengthInPixels;
      Log.InfoFormat("Color focal length = {0}", HandInputParams.ColorFocalLength);
      Log.InfoFormat("Depth focal length = {0}", HandInputParams.DepthFocalLength);
    }

    /// <summary>
    /// Starts Kinect if it is not started. This call takes some time.
    /// </summary>
    void StartKinect() {
      if (kinectSensor == null || kinectSensor.IsRunning)
        return;

      if (replay != null) {
        replay.Dispose();
        replay = null;
      }

      kinectSensor.AllFramesReady += kinectRuntime_AllFrameReady;
      kinectSensor.Start();
      StartSpeechRecognition();
    }

    void StartTracking() {
      StopReplay();
      Log.Debug("Start tracking.");
      StartKinect();
      handTracker = new SimpleSkeletonHandTracker(HandInputParams.DepthWidth,
          HandInputParams.DepthHeight, kinectSensor.CoordinateMapper);
      recogEngine = new GestureRecognitionEngine(modelSelector.SelectedModel);
    }

    void SetStatus(String status) {
      statusTextBox.AppendText(status + '\n');
      statusTextBox.ScrollToEnd();
    }

    void UpdateDisplay(TrackingResult result) {
      colorCanvas.Children.Clear();
      depthCanvas.Children.Clear();
      if (result.ColorBoundingBoxes.Count > 0) {
        VisualUtil.DrawRectangle(colorCanvas, result.ColorBoundingBoxes.Last(), Brushes.Red,
            (float)colorCanvas.ActualWidth / HandInputParams.ColorWidth);
      }
      if (result.DepthBoundingBoxes.Count > 0) {
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
        debugDepthDisplayManager.UpdateBitmap(result.DepthImage.Bytes);
        debugColorDisplayManager.UpdateBitmap(result.ColorImage.Bytes);
      }
    }

    void UpdateSimpleHandTrackerDisplay() {
      SimpleSkeletonHandTracker ssht = (SimpleSkeletonHandTracker)handTracker;
      VisualUtil.DrawRectangle(depthCanvas, ssht.InitialHandRect, Brushes.Green,
          (float)depthCanvas.ActualWidth / HandInputParams.DepthWidth);
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
      debugColorDisplayManager.UpdateBitmap(converted.Bytes);
      debugColorDisplayManager.UpdateBitmapMask(sht.SaliencyProb.Data);
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
          if (handTracker != null && recogEngine != null) {
            var result = handTracker.Update(depthManager.PixelData, colorManager.PixelData,
              SkeletonUtil.FirstTrackedSkeleton(sf.GetSkeletons()));
            var gesture = recogEngine.Update(result);
            lock (inputServer)
              inputServer.Send(gesture);
            UpdateDisplay(result);
            textGestureEvent.Text = gesture;
            fpsCounter.LogFPS();
          }
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
        if (viewSkeleton) {
          skeletonDisplayManager.Draw(frame.Skeletons, false,
                                      HandInputParams.ColorImageFormat);
        } else {
          skeletonCanvas.Children.Clear();
        }
      } catch (Exception e) {
        Log.Error(e.Message);
      }

      stabilitiesList.ItemsSource = stabilities;
    }

    void ToggleViewSkeleton() {
      viewSkeleton = !viewSkeleton;
    }

    void ToggleViewHog() {
      viewHog = !viewHog;
    }

    void ToggleDebugDisplayOption() {
      switch (displayOption) {
        case DisplayOption.COLOR:
          displayOption = DisplayOption.DEPTH;
          break;
        case DisplayOption.DEPTH:
          displayOption = DisplayOption.COLOR;
          break;
      }
    }

    void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
      Clean();
    }

    /// <summary>
    /// Cleans up everything.
    /// </summary>
    void Clean() {
      Log.Info("Cleaning.");
      if (audioManager != null) {
        audioManager.Dispose();
        audioManager = null;
      }

      if (recorder != null) {
        recorder.Close();
        recorder = null;
      }

      if (kinectSensor != null && kinectSensor.IsRunning) {
        kinectSensor.AllFramesReady -= kinectRuntime_AllFrameReady;
        kinectSensor.Stop();
      }
      kinectSensor = null;

      StopReplay();
      inputServer.Stop();
    }

    void Button_Click(object sender, RoutedEventArgs e) {
      displayDebug = !displayDebug;

      if (displayDebug) {
        viewButton.Content = "View Color/Depth";
        kinectDisplay.DataContext = debugColorDisplayManager;
        depthDisplay.DataContext = debugDepthDisplayManager;
      } else {
        viewButton.Content = "View Debug";
        kinectDisplay.DataContext = colorManager;
        depthDisplay.DataContext = depthManager;
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
      Action action;
      var found = keyActions.TryGetValue(e.Key, out action);
      if (found) {
        action();
      }
    }

    void depthDisplay_MouseDown(object sender, MouseButtonEventArgs e) {
      var p = e.GetPosition(depthDisplay);
      var x = (int)(p.X * HandInputParams.DepthWidth / depthDisplay.ActualWidth);
      var y = (int)(p.Y * HandInputParams.DepthHeight / depthDisplay.ActualHeight);
      var raw = depthManager.PixelData[y * HandInputParams.DepthWidth + x];
      Log.DebugFormat("depth = {0}", DepthUtil.RawToDepth(raw));
    }
  }
}
