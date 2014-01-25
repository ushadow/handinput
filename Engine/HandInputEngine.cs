using System;

using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;

using Common.Logging;

namespace HandInput.Engine {

  public delegate void HandInputEventHandler(HandInputEngine sender, HandInputEvent e);

  /// <summary>
  /// HandInputEngine provides the main interface to hand tracking and gesture recognition.
  /// </summary>
  public class HandInputEngine {
    private static readonly SkeletonTrackingMode SkeletonTrackingMode = SkeletonTrackingMode.Seated;
    private static readonly Boolean SkeletonEnableTrackingInNearMode = true;

    private static readonly ILog log = LogManager.GetCurrentClassLogger();

    public event HandInputEventHandler HandInputEvent;

    private KinectSensorChooser sensorChooser = new KinectSensorChooser();
    private HandJointTracker handTracker;

    /// <summary>
    /// Creates a new instance of HandInputEngine.
    /// </summary>
    public HandInputEngine() {
      sensorChooser.KinectChanged += new EventHandler<KinectChangedEventArgs>(OnKinectSensorChanged);
    }

    public void Start() { sensorChooser.Start(); }

    public void Stop() { sensorChooser.Stop();  }

    private void OnKinectSensorChanged(object sender, KinectChangedEventArgs e) {
      // KinectSensorChooser handles stopping the old kinect and starting the new kinect.
      var newSensor = e.NewSensor;
      if (newSensor == null) {
        log.InfoFormat("The new sensor is null.");
        return;
      }
      handTracker = new HandJointTracker(newSensor.CoordinateMapper);
      EnsureSkeletonStreamState(newSensor.SkeletonStream);
      newSensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(OnAllFramesReady);
    }

    private void OnAllFramesReady(Object sender, AllFramesReadyEventArgs e) {
      using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame()) {
        var res = handTracker.Update(skeletonFrame, null);
        if (res.IsSome) {
          if (HandInputEvent != null)
            HandInputEvent(this, res.Value);
        }
      }
    }

    private void EnsureSkeletonStreamState(SkeletonStream ss) {
      ss.Enable();
      ss.TrackingMode = SkeletonTrackingMode;
      ss.EnableTrackingInNearRange = SkeletonEnableTrackingInNearMode;
    }
  }
}
