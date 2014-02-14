using System.IO;
using System.Windows;
using Kinect.Toolbox.Record;
using Microsoft.Win32;

using Microsoft.Kinect;
using HandInput.Util;

namespace GesturesViewer {
  partial class MainWindow {
    private void recordOption_Click(object sender, RoutedEventArgs e) {
      if (recorder != null) {
        StopRecord();
        recordOption.Content = "Record";
        return;
      }

      SaveFileDialog saveFileDialog = new SaveFileDialog { Title = "Select filename", 
          Filter = "Replay files|*.bin" };

      if (saveFileDialog.ShowDialog() == true) {
        DirectRecord(saveFileDialog.FileName);
      }
      recordOption.Content = "Stop Recording";
    }

    void DirectRecord(string targetFileName) {
      Stream recordStream = File.Create(targetFileName);
      recorder = new KinectRecorder(KinectRecordOptions.Skeletons | KinectRecordOptions.Color |
          KinectRecordOptions.Depth, kinectSensor.CoordinateMapper,
          HandInputParams.ColorFocalLength, HandInputParams.DepthFocalLength, recordStream);
    }

    void StopRecord() {
      if (recorder != null) {
        recorder.Close();
        recorder = null;
        return;
      }
    }
  }
}
