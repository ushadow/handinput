using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.IO;
using System.Windows.Input;

using Microsoft.Kinect;
using Microsoft.Win32;

using Kinect.Toolbox;
using Kinect.Toolbox.Record;
using Kinect.Toolbox.Voice;

using Common.Logging;

namespace GesturesViewer {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow {
    static readonly ILog Log = LogManager.GetCurrentClassLogger();

    KinectSensor kinectSensor;

    readonly ColorStreamManager colorManager = new ColorStreamManager();
    readonly DepthStreamManager depthManager = new DepthStreamManager();
    readonly TrainingManager trainingManager = new TrainingManager();
    readonly ContextTracker contextTracker = new ContextTracker();
    
    AudioStreamManager audioManager;
    SkeletonDisplayManager skeletonDisplayManager;
    EyeTracker eyeTracker;
    bool displayDepth;

    KinectRecorder recorder;
    KinectAllFramesReplay replay;

    BindableNUICamera nuiCamera;

    VoiceCommander voiceCommander;

    Int32 depthFrameNumber;

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

      kinectSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

      kinectSensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);

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

      voiceCommander = new VoiceCommander("record", "stop");
      voiceCommander.OrderDetected += voiceCommander_OrderDetected;

      StartVoiceCommander();

      kinectDisplay.DataContext = colorManager;
    }

    void UpdateDepthFrame(ReplayDepthImageFrame frame) {
      if (!displayDepth)
        return;

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
        
        if (sf != null)
          UpdateSkeletonFrame(sf);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="frame">frame is not null.</param>
    void UpdateColorFrame(ReplayColorImageFrame frame) {
      if (displayDepth)
        return;

      colorManager.Update(frame);
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
    }

    private void Clean() {
      if (audioManager != null) {
        audioManager.Dispose();
        audioManager = null;
      }

      if (voiceCommander != null) {
        voiceCommander.OrderDetected -= voiceCommander_OrderDetected;
        voiceCommander.Stop();
        voiceCommander = null;
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
      OpenFileDialog openFileDialog = new OpenFileDialog { Title = "Select filename", 
          Filter = "Replay files|*.replay" };

      if (openFileDialog.ShowDialog() == true) {
        if (replay != null) {
          replay.AllFramesReady -= replay_AllFramesReady;
          replay.Stop();
        }
        Stream recordStream = File.OpenRead(openFileDialog.FileName);

        replay = new KinectAllFramesReplay(recordStream);

        replay.AllFramesReady += replay_AllFramesReady;

        replay.Start();
      }
    }

    void replay_AllFramesReady(object sender, ReplayAllFramesReadyEventArgs e) {
      var df = e.DepthImageFrame;
      if (df != null)
        statusTextBox.Text = df.FrameNumber.ToString();
      UpdateDepthFrame(df);
      UpdateColorFrame(e.ColorImageFrame);
      UpdateSkeletonFrame(e.SkeletonFrame);
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
        default:
          break;
      }
    }
  }
}
