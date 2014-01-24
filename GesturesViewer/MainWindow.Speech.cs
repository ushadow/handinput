using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Microsoft.Speech.Recognition;

using HandInput.Engine;

namespace HandInput.GesturesViewer {
  partial class MainWindow {
    static readonly double ConfidenceThreshold = 0.3;

    SpeechManager speechManager;

    void StartSpeechRecognition() {
      using (var memoryStream = new MemoryStream(
          Encoding.ASCII.GetBytes(Properties.Resources.SpeechGrammar))) {
        speechManager = SpeechManager.Create(kinectSensor.AudioSource.Start(), memoryStream);
        if (speechManager != null) {
          speechManager.SpeechRecognized += SpeechRecognized;
          speechManager.Start();
        }
      }
    }

    void SpeechRecognized(object sender, SpeechRecognizedEventArgs e) {
      if (e.Result.Confidence >= ConfidenceThreshold) {
        var s = e.Result.Semantics.Value.ToString();
        speechTextBox.Text = s;
      }
    }

    void StopSpeechRecognition() {
      if (speechManager != null)
        speechManager.Stop();
    }

  }
}
