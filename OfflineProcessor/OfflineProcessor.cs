using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Media.Media3D;

using Kinect.Toolbox;
using Kinect.Toolbox.Record;

using HandInput.Engine;
using HandInput.Util;

using Common.Logging;

namespace HandInput.OfflineProcessor {
  class OfflineProcessor {
    static readonly ILog Log = LogManager.GetCurrentClassLogger();

    String inputFile, outputFile;
    Object readLock, writeLock;
    KinectAllFramesReplay replayer;
    Type featureProcessorType, handTrackerType;
    IList<Single[]> featureList = new List<Single[]>();
    IList<Int32> frameList = new List<Int32>();
    int sampleRate;
    SalienceFeatureProcessor featureProcessor;

    public OfflineProcessor(String inputFile, String outputFile, Object readLock,
      Object writeLock, Type handTrackerType, Type featureProcessorType, int sampleRate) {
      this.inputFile = inputFile;
      this.outputFile = outputFile;
      this.readLock = readLock;
      this.writeLock = writeLock;
      this.handTrackerType = handTrackerType;
      this.featureProcessorType = featureProcessorType;
      this.sampleRate = sampleRate;
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
      IHandTracker handTracker = null;
      Int16[] depthPixelData = null;
      Byte[] colorPixelData = null;

      Log.DebugFormat("Start processing {0}...", inputFile);
      for (int i = 0; i < replayer.FrameCount; i += sampleRate) {
        var allFrames = replayer.FrameAt(i);
        var depthFrame = allFrames.DepthImageFrame;
        var colorFrame = allFrames.ColorImageFrame;

        if (handTracker == null)
          handTracker = (IHandTracker)Activator.CreateInstance(handTrackerType, new Object[] {
            depthFrame.Width, depthFrame.Height, replayer.KinectParams});
        if (featureProcessor == null)
          featureProcessor = (SalienceFeatureProcessor)Activator.CreateInstance(
              featureProcessorType);
        if (depthPixelData == null)
          depthPixelData = new Int16[depthFrame.PixelDataLength];
        if (colorPixelData == null)
          colorPixelData = new Byte[colorFrame.PixelDataLength];

        depthFrame.CopyPixelDataTo(depthPixelData);
        colorFrame.CopyPixelDataTo(colorPixelData);
        var skeleton = SkeletonUtil.FirstTrackedSkeleton(allFrames.SkeletonFrame.Skeletons);
        var result = handTracker.Update(depthPixelData, colorPixelData, skeleton);
        var feature = featureProcessor.Compute(result);
        if (feature.IsSome) {
          frameList.Add(allFrames.FrameNumber);
          featureList.Add(feature.Value);
        }
      }
      Log.DebugFormat("Finished processing {0}.", inputFile);
    }

    private void ReadFile() {
      Log.DebugFormat("Reading file {0}...", inputFile);
      var recordStream = File.OpenRead(inputFile);
      replayer = new KinectAllFramesReplay(recordStream);
    }

    private void WriteToFile() {
      using (var file = new StreamWriter(File.Create(outputFile))) {
        file.WriteLine("# frame_id, feature_length, {0}, descriptor_length, {1}, sample_rate, {2}", 
            featureProcessor.FeatureLength, featureProcessor.DescriptorLength, sampleRate);
        for (int i = 0; i < frameList.Count; i++) {
          file.Write("{0},", frameList.ElementAt(i));
          Write(file, featureList.ElementAt(i));
          file.WriteLine();
        }
      }
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
