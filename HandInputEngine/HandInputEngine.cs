using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;

using Common.Logging;

namespace HandInput.HandInputEngine
{
  class HandInputEngine
  {
    private static readonly ILog log = LogManager.GetCurrentClassLogger();

    private KinectSensorChooser sensorChooser = new KinectSensorChooser();
    private HandTracker handTracker = new HandTracker();

    /// <summary>
    /// Creates a new instance of HandInputEngine.
    /// </summary>
    public HandInputEngine()
    {
      sensorChooser.KinectChanged += new EventHandler<KinectChangedEventArgs>(OnKinectSensorChanged);
      sensorChooser.Start();
    }

    public void Start()
    {
      while (true);
    }

    private void OnKinectSensorChanged(object sender, KinectChangedEventArgs e)
    {
      // KinectSensorChooser handles stopping the old kinect and starting the new kinect.
      var newSensor = e.NewSensor;
      if (newSensor == null)
      {
        log.InfoFormat("The new sensor is null.");    
      }

      newSensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(OnAllFramesReady);
    }

    private void OnAllFramesReady(Object sender, AllFramesReadyEventArgs e)
    {
      using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
      {
        handTracker.Update(skeletonFrame);
      }
    }
  }


}
