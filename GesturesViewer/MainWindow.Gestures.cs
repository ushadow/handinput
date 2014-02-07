using System;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Configuration;
using System.Diagnostics;

namespace HandInput.GesturesViewer {
  // Manages gesture recording.
  partial class MainWindow {

    static readonly int BatchIndex = 1;
    static readonly String Pid = ConfigurationManager.AppSettings["pid"];
    static readonly String DataDir = ConfigurationManager.AppSettings["data_dir"];
    static readonly String OutputDir = Path.Combine(DataDir,
            ConfigurationManager.AppSettings["processor_output_dir"]);
    static readonly String OfflineProcessorExe = Path.GetFullPath(ConfigurationManager.AppSettings[
        "offline_processor"]);

    StreamWriter sw;

    void recordGesture_Click(object sender, RoutedEventArgs e) {
      RecordGesture();
    }

    void RecordGesture() {
      StartKinect();

      var time = String.Format("{0:yyyy-MM-dd_HH-mm}", DateTime.Now);
      var dir = Path.Combine(DataDir, Pid, time);
      Directory.CreateDirectory(dir);
      var fileName = Path.Combine(dir, String.Format(TrainingManager.KinectDataPattern, BatchIndex));
      var gtFile = Path.Combine(dir, String.Format(TrainingManager.KinectGTDPattern, BatchIndex));
      sw = new StreamWriter(File.Create(gtFile));
      DirectRecord(fileName);

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
          break;
        case TrainingEventType.End:
          sw.Close();
          StopRecord();
          trainingManager.Status = "Start processing";
          ExecuteOfflineProcessor();
          trainingManager.Status = "Done processing";
          break;
        case TrainingEventType.StartGesture:
          sw.WriteLine("{0} {1} {2}", TrainingEventType.StartGesture.ToString(), e.Gesture,
              depthFrameNumber);
          break;
      }
    }

    void ExecuteOfflineProcessor() {
      var argsPrefix = String.Format("-i={0} -o={1} -k ", DataDir, OutputDir);
      var args = argsPrefix + "--data=standing --tracker=simple --processor=hog";
      ExecuteCommand(OfflineProcessorExe, args);
      args = argsPrefix + "-t=gt";
      ExecuteCommand(OfflineProcessorExe, args);
      Log.Info("Finished offline processing.");
    }

    void ExecuteCommand(String command, String args) {
      Log.InfoFormat("Executing {0} {1}", command, args);
      var processInfo = new ProcessStartInfo(command, args);
      processInfo.CreateNoWindow = true;
      processInfo.UseShellExecute = false;
      processInfo.RedirectStandardError = true;
      processInfo.RedirectStandardOutput = true;
      var process = Process.Start(processInfo);
      process.WaitForExit();

      var output = process.StandardOutput.ReadToEnd();
      var error = process.StandardError.ReadToEnd();

      var exitCode = process.ExitCode;

      Log.DebugFormat("output>>{0}", String.IsNullOrEmpty(output) ? "(none)" : output);
      Log.DebugFormat("error>>{0}", String.IsNullOrEmpty(error) ? "(none)" : error);
      Log.DebugFormat("exit code = {0}", exitCode.ToString());
      process.Close();
    }
  }
}
