using System;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Configuration;

namespace HandInput.GesturesViewer {
  // Manages gesture recording.
  partial class MainWindow {
    static readonly String DataDir = ConfigurationManager.AppSettings["data_dir"];
    static readonly String Pid = ConfigurationManager.AppSettings["pid"];
    static readonly String[] Gestures = ConfigurationManager.AppSettings["gestures"].Split(',');

    StreamWriter sw;

    void recordGesture_Click(object sender, RoutedEventArgs e) {
      RecordGesture();
    }

    void RecordGesture() {
      trainingManager.TrainingEvent += OnTrainingEvent;
      trainingManager.Start();
      var binding = new Binding("Status");
      binding.Mode = BindingMode.OneWay;
      statusTextBox.DataContext = trainingManager;
      binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
      this.statusTextBox.SetBinding(TextBox.TextProperty, binding);
    }

    void OnTrainingEvent(Object sender, TrainingEventArgs e) {
      switch (e.Type) {
        case TrainingEventType.Start:
          var time = String.Format("{0:yyyy-MM-dd_HH-mm}", DateTime.Now);
          var dir = Path.Combine(DataDir, Pid, time);
          Directory.CreateDirectory(dir);
          var fileName = Path.Combine(dir, "KinectData_1.bin");
          var gtFile = Path.Combine(dir, "KinectDataGTD_1.txt");
          sw = new StreamWriter(File.Create(gtFile));
          DirectRecord(fileName);
          break;
        case TrainingEventType.End:
          sw.Close();
          StopRecord();
          break;
        case TrainingEventType.StartGesture:
          sw.WriteLine("{0} {1} {2}", TrainingEventType.StartGesture.ToString(), e.Gesture,
              depthFrameNumber);
          break;
      }
    }
  }
}
