using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Kinect.Toolbox;
using Microsoft.Kinect;
using System.Windows.Data;
using System.Windows.Controls;

namespace GesturesViewer {
  partial class MainWindow {
    private void recordGesture_Click(object sender, RoutedEventArgs e) {
      RecordGesture();
    }

    private void RecordGesture() {
      trainingManager.TrainingEvent += new TrainingEventHandler(OnTrainingEvent);
      trainingManager.Start();
      var binding = new Binding("Status");
      binding.Mode = BindingMode.OneWay;
      statusTextBox.DataContext = trainingManager;
      binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
      this.statusTextBox.SetBinding(TextBox.TextProperty, binding);
    }

    private void OnTrainingEvent(TrainingManager sender, TrainingEventArgs e) {
      switch (e.Type) {
        case TrainingEventType.Start:
          var fileName = TrainingRecordFile();
          Log.Debug(fileName);
          DirectRecord(fileName);
          break;
        case TrainingEventType.End:
          StopRecord();
          break;
      }
    }

    private String TrainingRecordFile() {
      var dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
      var file = Path.Combine(dir, 
              String.Format("training-{0:dd-MM-yyyy_HH-mm}.replay", DateTime.Now));
      return file;
    }

  }
}
