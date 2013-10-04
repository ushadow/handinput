using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Media.Media3D;

namespace HandInput.OfflineProcessor {
  class OfflineProcessor {
    private String inputFile, outputFile;
    private Object readLock, writeLock;

    public OfflineProcessor(String inputFile, String outputFile, Object readLock,
                            Object writeLock) {
      this.inputFile = inputFile;
      this.outputFile = outputFile;
      this.readLock = readLock;
      this.writeLock = writeLock;
    }

    public void ReadFile() {

    }
    public void Process() {

    }
    public void WriteToFile() {
      
    }

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
