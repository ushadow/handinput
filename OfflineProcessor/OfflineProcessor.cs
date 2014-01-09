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
using System.Runtime.Serialization.Formatters.Binary;

namespace HandInput.OfflineProcessor {
  class OfflineProcessor {
    static readonly ILog Log = LogManager.GetCurrentClassLogger();

    String inputFile, outputFile, gtSensor;
    Object readLock, writeLock;
    dynamic replayer;
    Type replayerType, featureProcessorType, handTrackerType;
    IList<Array> featureList = new List<Array>();
    IList<Int32> frameList = new List<Int32>();
    float sampleRate;
    IFeatureProcessor featureProcessor;

    public OfflineProcessor(String inputFile, String outputFile, Object readLock,
      Object writeLock, Type replayerType, Type handTrackerType, Type featureProcessorType,
      float sampleRate, String gtSensor) {
      this.inputFile = inputFile;
      this.outputFile = outputFile;
      this.readLock = readLock;
      this.writeLock = writeLock;
      this.handTrackerType = handTrackerType;
      this.featureProcessorType = featureProcessorType;
      this.replayerType = replayerType;
      this.sampleRate = sampleRate;
      this.gtSensor = gtSensor;
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

    void ProcessFeature() {
      IHandTracker handTracker = null;
      Int16[] depthPixelData = null;
      Byte[] colorPixelData = null;

      Log.DebugFormat("Start processing {0}...", inputFile);
      for (float i = 0; i < replayer.GetFramesCount(); i += sampleRate) {
        int index = (int)Math.Round(i);
        var skeletonFrame = replayer.GetSkeletonFrame(index);
        var depthFrame = replayer.GetDepthFrame(index);
        var colorFrame = replayer.GetColorFrame(index);

        if (handTracker == null) {
          handTracker = (IHandTracker)Activator.CreateInstance(handTrackerType, new Object[] {
            depthFrame.Width, depthFrame.Height, GetKinectParams()});
        }
        if (featureProcessor == null)
          featureProcessor = (IFeatureProcessor)Activator.CreateInstance(
              featureProcessorType);
        if (depthPixelData == null)
          depthPixelData = new Int16[depthFrame.PixelDataLength];
        if (colorPixelData == null)
          colorPixelData = new Byte[colorFrame.PixelDataLength];

        depthFrame.CopyPixelDataTo(depthPixelData);
        colorFrame.CopyPixelDataTo(colorPixelData);
        var skeleton = SkeletonUtil.FirstTrackedSkeleton(skeletonFrame.Skeletons);
        var result = handTracker.Update(depthPixelData, colorPixelData, skeleton);
        Option<Array> feature = featureProcessor.Compute(result);
        if (feature.IsSome) {
          if (replayerType == typeof(KinectAllFramesReplay))
            frameList.Add(depthFrame.FrameNumber);
          else
            frameList.Add(index);
          featureList.Add(feature.Value);
        }
      }
      Log.DebugFormat("Finished processing {0}.", inputFile);
    }

    void ReadFile() {
      Log.DebugFormat("Reading file {0}...", inputFile);
      var recordStream = File.OpenRead(inputFile);
      replayer = Activator.CreateInstance(replayerType, new Object[] { recordStream });
    }

    void WriteToFile() {
      using (var file = new StreamWriter(File.Create(outputFile))) {
        file.WriteLine("# frame_id, motion_feature_len, {0}, descriptor_len, {1}, " +
            "sample_rate, {2}, gt_sensor, {3}", featureProcessor.MotionFeatureLength,
            featureProcessor.DescriptorLength, sampleRate, gtSensor);
        for (int i = 0; i < frameList.Count; i++) {
          file.Write("{0},", frameList.ElementAt(i));
          Write(file, featureList.ElementAt(i));
          file.WriteLine();
        }
      }
    }

    Byte[] GetKinectParams() {
      if (replayerType == typeof(KinectAllFramesReplay))
        return replayer.GetKinectParams();
      var bf = new BinaryFormatter();
      var stream = new MemoryStream(Properties.Resources.ColorToDepthRelationalParameters);
      IEnumerable<byte> kinectParams = bf.Deserialize(stream) as IEnumerable<byte>;
      stream.Close();
      return kinectParams.ToArray<byte>();
    }

    void Write(StreamWriter sw, Vector3D v) {
      sw.Write("{0},{1},{2},", v.X, v.Y, v.Z);
    }

    void Write(StreamWriter sw, Array array) {
      foreach (var e in array) {
        sw.Write("{0},", e);
      }
    }
  }
}
