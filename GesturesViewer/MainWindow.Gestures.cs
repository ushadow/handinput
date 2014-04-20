using System;
using System.IO;
using System.Windows;
using System.Configuration;
using System.Diagnostics;
using System.Threading;

namespace GesturesViewer {
  // Manages gesture recording.
  partial class MainWindow {

    static readonly String TimeFormat = "{0:yyyy-MM-dd_HH-mm}";
    static readonly int BatchIndex = 1;
    static readonly String OfflineProcessorExe = Path.GetFullPath(ConfigurationManager.AppSettings[
        "offline_processor"]);
    static readonly String MatlabExe = "matlab";

    StreamWriter sw;
    bool processFeature = !String.IsNullOrEmpty(
        ConfigurationManager.AppSettings["process_feature"]);
    String dataDir, outputDir;

    void InitDataDir() {
      var rootDir = Directory.GetDirectoryRoot(AppDomain.CurrentDomain.BaseDirectory);
      dataDir = Path.Combine(rootDir,  ConfigurationManager.AppSettings["data_dir"]);
      outputDir = Path.Combine(dataDir, ConfigurationManager.AppSettings["processor_output_dir"]);
    }
      
    void recordGesture_Click(object sender, RoutedEventArgs e) {
      RecordGesture();
    }

    void RecordGesture() {
      StopTracking();

      if (!IsKinectRunning())
        StartKinect();

      var time = String.Format(TimeFormat, DateTime.Now);
      var dir = Path.Combine(dataDir, trainingManager.Pid, time);
      Directory.CreateDirectory(dir);
      var fileName = Path.Combine(dir, String.Format(TrainingManager.KinectDataPattern, BatchIndex));
      var gtFile = Path.Combine(dir, String.Format(TrainingManager.KinectGTDPattern, BatchIndex));
      sw = new StreamWriter(File.Create(gtFile));
      DirectRecord(fileName);
      trainingManager.Start();
    }

    void OnTrainingEvent(Object sender, TrainingEventArgs e) {
      switch (e.Type) {
        case TrainingEventType.Start:
          break;
        case TrainingEventType.End:
          sw.Close();
          StopRecord();
          if (processFeature) {
            trainingManager.Status = "Start processing";
            ExecuteOfflineProcessor();
            trainingManager.Status = "Done processing";
          }
          break;
        case TrainingEventType.StartGesture:
          sw.WriteLine("{0} {1} {2}", TrainingEventType.StartGesture.ToString(), e.Gesture,
              depthFrameNumber);
          break;
      }
    }

    void ExecuteOfflineProcessor() {
      var argsPrefix = String.Format("-i={0} -o={1} -k ", dataDir, outputDir);
      var args = argsPrefix + "--data=standing --tracker=simple --processor=hog";
      ExecuteCommand(OfflineProcessorExe, args);
      args = argsPrefix + "-t=gt";
      ExecuteCommand(OfflineProcessorExe, args);
      Log.Info("Finished offline processing.");
      CopyGestureDefFile(outputDir);
    }

    void ExecuteCommand(String command, String args, bool redirectError = true,
        bool redirectOutput = true) {
      Log.InfoFormat("Executing {0} {1}", command, args);
      var processInfo = new ProcessStartInfo(command, args);
      processInfo.CreateNoWindow = true;
      processInfo.UseShellExecute = false;
      processInfo.RedirectStandardError = redirectError;
      processInfo.RedirectStandardOutput = redirectOutput;
      var process = Process.Start(processInfo);
      process.WaitForExit();

      if (redirectOutput) {
        var output = process.StandardOutput.ReadToEnd();
        Log.DebugFormat("output>>{0}", String.IsNullOrEmpty(output) ? "(none)" : output);
      }

      if (redirectError) {
        var error = process.StandardError.ReadToEnd();
        Log.DebugFormat("error>>{0}", String.IsNullOrEmpty(error) ? "(none)" : error);
      }

      var exitCode = process.ExitCode;
      Log.DebugFormat("exit code = {0}", exitCode.ToString());
      process.Close();
    }

    void trainButton_Click(object sender, RoutedEventArgs e) {
      TrainModel();
    }

    void TrainModel() {
      var time = String.Format(TimeFormat, DateTime.Now);
      var fileName = time + ".mat";
      var path = Path.Combine(ModelDir, fileName);
      var args = String.Format("-nodisplay -nosplash -nodesktop -r \"train('{0}', '{1}'); pause(1); exit;\"",
          outputDir, path);
      ExecuteCommand(MatlabExe, args, false, false);
      RefreshModel();
    }

    void RefreshModel() {
      modelSelector.Refresh();
      modelComboBox.SelectedItem = modelSelector.SelectedModel;
    }

    void CopyGestureDefFile(String outputDir) {
      var fileName = Path.GetFileName(TrainingManager.GestureDefFile);
      var outputFile = Path.Combine(outputDir, fileName);
      File.Copy(TrainingManager.GestureDefFile, outputFile, true);
      Log.InfoFormat("Copied file {0} to {1}", TrainingManager.GestureDefFile, outputFile);
    }
  }
}
