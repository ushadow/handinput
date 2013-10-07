using System;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Configuration;

namespace HandInput.GesturesViewer {
  partial class MainWindow {
    static readonly String DataDir = ConfigurationManager.AppSettings["data-dir"];

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
          var timeSuffix = String.Format("-{0:yyyy-MM-dd_HH-mm}", DateTime.Now);
          var fileName = Path.Combine(DataDir, String.Format("KinectData{0}.bin", timeSuffix));
          var gtFile = Path.Combine(DataDir, String.Format("KinectDataGTD{0}.txt", timeSuffix));
          sw = new StreamWriter(File.Create(gtFile));
          DirectRecord(fileName);
          break;
        case TrainingEventType.End:
          sw.Close();
          StopRecord();
          break;
        case TrainingEventType.StartPreStroke:
          sw.WriteLine("{0} {1} {2}", TrainingEventType.StartPreStroke.ToString(), e.Gesture,
              depthFrameNumber);
          break;
      }
    }
  }
}
