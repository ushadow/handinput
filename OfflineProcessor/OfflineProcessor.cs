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
    List<dynamic> frameList = new List<dynamic>();
    float sampleRate;
    IFeatureProcessor featureProcessor;
    int bufferSize;

    public OfflineProcessor(String inputFile, String outputFile, Object readLock,
      Object writeLock, Type replayerType, Type handTrackerType, Type featureProcessorType,
      float sampleRate, String gtSensor, int bufferSize) {
      this.inputFile = inputFile;
      this.outputFile = outputFile;
      this.readLock = readLock;
      this.writeLock = writeLock;
      this.handTrackerType = handTrackerType;
      this.featureProcessorType = featureProcessorType;
      this.replayerType = replayerType;
      this.sampleRate = sampleRate;
      this.gtSensor = gtSensor;
      this.bufferSize = bufferSize;
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
      int frameCount = replayer.GetFramesCount(); 
      for (float i = 0; i < frameCount; i += sampleRate) {
        int index = (int)Math.Round(i);
        
        if (index >= frameCount)
          break;
        
        var skeletonFrame = replayer.GetSkeletonFrame(index);
        var depthFrame = replayer.GetDepthFrame(index);
        var colorFrame = replayer.GetColorFrame(index);

        if (handTracker == null) {
          handTracker = (IHandTracker)Activator.CreateInstance(handTrackerType, new Object[] {
            depthFrame.Width, depthFrame.Height, GetKinectParams(), bufferSize});
        }
        if (featureProcessor == null)
          featureProcessor = (IFeatureProcessor)Activator.CreateInstance(
              featureProcessorType, new Object[] {sampleRate});
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
          if (replayerType == typeof(KinectAllFramesReplay)) {
            frameList.Add(depthFrame.GetFrameNumber());
          } else {
            int curIndex = (int) Math.Round(i - sampleRate * (bufferSize - 1));
            frameList.Add(curIndex);
          }
          featureList.Add(feature.Value);
        }
      }
      Log.DebugFormat("Finished processing {0}.", inputFile);
    }

    void ReadFile() {
      Log.DebugFormat("Reading file {0}...", inputFile);
      var recordStream = File.OpenRead(inputFile);
      Log.DebugFormat("Create replayer type: {0}", replayerType);
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

    byte[] GetKinectParams() {
      if (replayerType == typeof(KinectAllFramesReplay))
        return replayer.GetKinectParams();
      return HandInputParams.GetKinectParams(Properties.Resources.ColorToDepthRelationalParameters); 
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
