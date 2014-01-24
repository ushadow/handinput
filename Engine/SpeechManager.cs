using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Microsoft.Speech.Recognition;
using Microsoft.Speech.AudioFormat;
using Microsoft.Kinect;

namespace HandInput.Engine {
  public class SpeechManager {
    static public SpeechManager Create(Stream audioStream, Stream grammar) {
      foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers()) {
        string value;
        recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
        if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) &&
            "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase)) {
          return new SpeechManager(recognizer.Id, audioStream, grammar);
        }
      }

      return null;
    }

    public event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized {
      add { engine.SpeechRecognized += value; }
      remove { engine.SpeechRecognized -= value; }
    }

    SpeechRecognitionEngine engine;

    public void Start() {
      engine.RecognizeAsync(RecognizeMode.Multiple);
    }

    public void Stop() {
      engine.RecognizeAsyncStop();
    }

    SpeechManager(String id, Stream audioStream, Stream grammar) {
      engine = new SpeechRecognitionEngine(id);
      engine.LoadGrammar(new Grammar(grammar));
      engine.SetInputToAudioStream(audioStream,
          new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
    }

  }
}
