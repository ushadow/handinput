using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HandInput.GesturesViewer {
  public class GroundTruthData {
    public int RefFrameDepth { get; private set; }

    public String PhaseLabel { get; private set; }
    public String GestureLabel { get; private set; }

    public static GroundTruthData FromTextFileFormat(String line) {
      var tokens = line.Split(' ');
      return new GroundTruthData(tokens[0], tokens[1], Int32.Parse(tokens[2]));
    }

    public GroundTruthData(String phase, String gestureLabel, int refFrameDepth) {
      this.PhaseLabel = phase;
      this.GestureLabel = gestureLabel;
      RefFrameDepth = refFrameDepth;
    }

  }
  public class GroundTruthDataRelayer {
    IDictionary<int, GroundTruthData> dict = new Dictionary<int, GroundTruthData>();

    public GroundTruthDataRelayer(Stream stream) {
      using (var reader = new StreamReader(stream)) {
        while (!reader.EndOfStream) {
          var line = reader.ReadLine();
          var data = GroundTruthData.FromTextFileFormat(line);
          dict[data.RefFrameDepth] = data;
        }
      }
    }

    public GroundTruthData GetDataFrame(int refFrame) {
      GroundTruthData data;
      dict.TryGetValue(refFrame, out data);
      return data;
    }
  }

}
