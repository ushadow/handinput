using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Media.Media3D;

using Kinect.Toolbox;
using Kinect.Toolbox.Record;

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

    public void Process() {
      lock (readLock) {
        ReadFile();
      }

      ProcessFeature();

      lock (writeLock) {
        WriteToFile();
      }
    }

    private void ProcessFeature() {

    }

    private void ReadFile() {
      var recordStream = File.OpenRead(inputFile);
      var replay = new KinectAllFramesReplay(recordStream);
    }

    private void WriteToFile() {

    }



    private void Write(StreamWriter sw, Vector3D v) {
      sw.Write("{0},{1},{2},", v.X, v.Y, v.Z);
    }

    private void Write(StreamWriter sw, Array array) {
      foreach (var e in array) {
        sw.Write("{0},", e);
      }
    }
  }
}
