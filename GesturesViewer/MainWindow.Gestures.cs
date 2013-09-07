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
      trainingManager.Start();
      var binding = new Binding("Status");
      binding.Mode = BindingMode.OneWay;
      statusTextBox.DataContext = trainingManager;
      binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
      this.statusTextBox.SetBinding(TextBox.TextProperty, binding);
    }

  }
}
