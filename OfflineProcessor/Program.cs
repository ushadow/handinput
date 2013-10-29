using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;

using Common.Logging;
using NDesk.Options;

using HandInput.Util;
using HandInput.Engine;

namespace HandInput.OfflineProcessor {
  class Program {
    private static readonly ILog Log = LogManager.GetCurrentClassLogger();

    private static readonly String KinectPattern = "KinectData_*.bin";
    private static readonly String GTPattern = "{0}DataGTD_*.txt";
    private static readonly String KinectRegex = @"KinectData_(\d+).bin";
    private static readonly String GTRegex = @"{0}DataGTD_(\d+).txt";
    private static readonly String PidRegex = @"PID-0*([1-9]+\d*)$";
    private static readonly String IndexRegex = @"(\d+)-?(\d+)?";
    private static readonly String Ext = "csv";

    private static readonly Int32 StartPid = 1, NumPids = 50;
    private static readonly Int32 StartBatch = 1, NumBatches = 30;

    /// <summary>
    /// Default options.
    /// </summary>
    private static int nSession = 4;
    private static int sampleRate = 1;
    private static String type = "fe";
    private static String sessionToProcess = null;
    private static String featureType = "kinectxsens";
    private static String gtSensor = "Kinect";

    private static ParallelProcessor pp = new ParallelProcessor();
    private static Object readLock = new Object();
    private static Object writeLock = new Object();
    private static IEnumerable<Int32> pidList, batchList;

    public static void Main(string[] args) {
      String inputFolder = null;
      String outputFolder = null;
      bool showHelp = false;
      pidList = Enumerable.Range(StartPid, NumPids);
      batchList = Enumerable.Range(StartBatch, NumBatches);

      var p = new OptionSet() {
        { "i=", "the input {FOLDER} of the data set", v => inputFolder = v },
        { "o=", "the output {FOLDER} of the processed data", v => outputFolder = v },
        { "t|type=", String.Format("type of the operation: gt|fe. Default is {0}.", type), 
            v => type = v },
        { "p=", "the {PID} of the data set to process. Can be a single number or a " +
            "range like 1-17. Default is all.", v => pidList = ParseIndex(v) },
        { "b=", "the {BATCH NUMBER(S)} to process. Can be a single number or a range like 1-8." +  
            " Default is all.", v => batchList = ParseIndex(v) },
        { "h|help", "show this message and exit", v => showHelp = v != null },
        { "s=", "{SESSION} name to be processed", v => sessionToProcess = v },
        { "ns=", "{NUMBER OF SESSIONS} to be processed. Default is 4.", 
            v => nSession = Int32.Parse(v) },
        { "f=", "{FEATURE TYPE} to process", v => featureType = v},
        { "gs=", String.Format("{{SENSOR}} for ground truth. Default is {0}.", gtSensor), 
            v => gtSensor = v},
        { "sample=", String.Format("{{SAMPLE RATE}}. Default is {0}.", sampleRate), 
            v => sampleRate = Int32.Parse(v) }
      };

      try {
        p.Parse(args);
      } catch (OptionException oe) {
        Console.WriteLine(oe.Message);
        return;
      }

      if (showHelp) {
        p.WriteOptionDescriptions(Console.Out);
        return;
      }

      var stopWatch = new Stopwatch();
      stopWatch.Start();
      ProcessPids(inputFolder, outputFolder);
      stopWatch.Stop();
      var ts = stopWatch.Elapsed;
      var elapsedTime = String.Format("{0:00}:{1:00}", ts.Hours, ts.Minutes);
      Log.InfoFormat("Run tinme = {0}", elapsedTime);
    }

    private static IEnumerable<Int32> ParseIndex(String index) {
      var match = Regex.Match(index, IndexRegex);

      if (match.Success) {
        var startNdx = Int32.Parse(match.Groups[1].Value);
        var endNdx = startNdx;
        if (match.Groups.Count > 2 && match.Groups[2].Length > 0)
          endNdx = Int32.Parse(match.Groups[2].Value);
        return Enumerable.Range(startNdx, endNdx - startNdx + 1);
      } else {
        throw new OptionException("Cannot parse index option.", index);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="inputFolder">Main database folder.</param>
    /// <param name="outputFolder"></param>
    private static void ProcessPids(String inputFolder, String outputFolder) {
      Log.DebugFormat("Process PIDs in the range: {0} - {1}", pidList.First(), pidList.Last());
      var dirs = Directory.GetDirectories(inputFolder);
      foreach (var dir in dirs) {
        var dirInfo = new DirectoryInfo(dir);
        var match = Regex.Match(dirInfo.Name, PidRegex);
        if (match.Success) {
          var pid = Int32.Parse(match.Groups[1].Value);
          if (pidList.Contains(pid)) {
            var outputPidDir = Path.Combine(outputFolder, dirInfo.Name);
            ProcessSessions(dir, outputPidDir);
          }
        }
      }
      if (type.Equals("fe"))
        pp.WaitAll();
    }

    private static void ProcessSessions(String inputFolder, String outputFolder) {
      String[] sessionDirs = null;

      if (sessionToProcess != null) {
        sessionDirs = new String[] { sessionToProcess };
      } else {
        sessionDirs = Directory.GetDirectories(inputFolder);
      }
      foreach (var dir in sessionDirs.Take(nSession)) {
        var dirInfo = new DirectoryInfo(dir);
        var inputSession = Path.Combine(inputFolder, dirInfo.Name);
        var outputSession = Path.Combine(outputFolder, dirInfo.Name);
        Directory.CreateDirectory(outputSession);
        ProcessBatches(inputSession, outputSession);
      }
    }

    /// <summary>
    /// Process all the batch data in one session.
    /// </summary>
    /// <param name="inputSessionFolder"></param>
    /// <param name="outputSessionFolder"></param>
    private static void ProcessBatches(String inputSessionFolder, String outputSessionFolder) {
      Log.DebugFormat("Process session {0}:", inputSessionFolder);

      var inputPattern = KinectPattern;
      var regex = KinectRegex;
      if (type.Equals("gt")) {
        inputPattern = String.Format(GTPattern, gtSensor);
        regex = String.Format(GTRegex, gtSensor);
      }

      String[] filePaths = Directory.GetFiles(inputSessionFolder, inputPattern);

      foreach (var inFile in filePaths) {
        var fileInfo = new FileInfo(inFile);
        var name = fileInfo.Name;

        var match = Regex.Match(name, regex);
        if (match.Success) {
          int batchNum = Int32.Parse(match.Groups[1].Value);
          if (batchList.Contains(batchNum)) {
            if (type.Equals("gt")) {
              var outputFile = Path.Combine(outputSessionFolder, name);
              File.Copy(inFile, outputFile, true);
            } else {
              var outFile = Path.Combine(outputSessionFolder, Path.ChangeExtension(name, Ext));
              OfflineProcessor proc = new OfflineProcessor(inFile, outFile, readLock, writeLock,
                typeof(SimpleSkeletonHandTracker), typeof(SalienceFeatureProcessor), sampleRate);
              try {
                pp.Spawn(proc.Process);
              } catch (Exception ex) {
                Log.Error(ex.Message);
              }
            }
          }
        }
      }
    }
  }
}
