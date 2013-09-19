using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Kinect.Toolbox.Record;
using System.IO;

namespace GesturesViewer {
  partial class MainWindow {
    static readonly int FPS = 30;
    DispatcherTimer timer;

    private void Replay(Stream recordStream) {
      replay = new KinectAllFramesReplay(recordStream);
      frameSlider.Maximum = replay.FrameCount;
      frameSlider.Value = 0;
      timer = new DispatcherTimer();
      timer.Interval = new TimeSpan(0, 0, 0, 0, (1000 / FPS));
      timer.Tick += new EventHandler(OnTimerTick);
      Start();
    }

    private void OnTimerTick(object sender, EventArgs e) {
      frameSlider.Value += 1;
    }

    private void frameSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) 
    {
      int index = (int)e.NewValue;
      var frame = replay.FrameAt(index);
      if (frame != null)
        ReplayFrame(frame.DepthImageFrame, frame.ColorImageFrame, frame.SkeletonFrame);
      else
        timer.Stop();
    }

    private void ReplayFrame(ReplayDepthImageFrame df, ReplayColorImageFrame cf, 
        ReplaySkeletonFrame sf) {
      if (df != null)
        statusTextBox.Text = df.FrameNumber.ToString();
      UpdateDepthFrame(df);
      UpdateColorFrame(cf);
      UpdateSkeletonFrame(sf);
    }

    private void Pause() {
      if (timer != null)
        timer.Stop();
    }

    private void Start() {
      if (timer != null)
        timer.Start();
    }

  }
}