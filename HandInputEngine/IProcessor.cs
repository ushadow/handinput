using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Media.Media3D;

namespace HandInput.Engine {
  abstract class Processor {
    public abstract void ReadFile();
    public abstract void Process();
    public abstract void WriteToFile();

    protected void Write(StreamWriter sw, Vector3D v) {
      sw.Write("{0},{1},{2},", v.X, v.Y, v.Z);
    }

    protected void Write(StreamWriter sw, Array array) {
      foreach (var e in array) {
        sw.Write("{0},", e);
      }
    }
  }
}
