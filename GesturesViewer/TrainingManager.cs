using System;
using System.Linq;
using System.Timers;
using System.ComponentModel;
using System.Configuration;
using System.IO;

using Common.Logging;
using System.Collections.Generic;

namespace GesturesViewer {

  public enum TrainingEventType { Start, End, StartGesture, StopPostStroke };

  public class TrainingEventArgs {
    public TrainingEventType Type { get; private set; }
    public String Gesture { get; private set; }
    public TrainingEventArgs(TrainingEventType e, String gesture = null) {
      Type = e;
      Gesture = gesture;
    }
  }

  public class TrainingManager : INotifyPropertyChanged {
    public static readonly String KinectGTDPattern = "KinectDataGTD_{0}.txt";
    public static readonly String KinectDataPattern = "KinectData_{0}.bin";
    public static readonly String KinectDataRegex = @"KinectData_(\d+).bin";

    static readonly String DefaultPid = ConfigurationManager.AppSettings["pid"];
    static readonly int GestureWaitTimeShort = 2000;
    static readonly int GestureWaitTime = 3000; //ms
    static readonly int GestureMaxWaitTime = 6000;
    static readonly int GestureStopWaitTime = 1000;
    static readonly int StartWaitTime = 8000;
    static readonly int DefaultNumRepitions = 3;
    static readonly ILog Log = LogManager.GetCurrentClassLogger();

    public int NumRepitions { get; private set; }
    public String Pid { get; private set; }

    public String Status {
      get {
        return status;
      }
      set {
        status = value;
        OnPropertyChanged("Status");
      }
    }

    public Dictionary<string, object> Items {
      get {
        return gestureList;
      }
    }

    public Dictionary<string, object> SelectedItems {
      get {
        return selectedItems;
      }
    }

    public bool ShowStop { get; private set; }

    public event PropertyChangedEventHandler PropertyChanged;
    public event EventHandler<TrainingEventArgs> TrainingEvent;

    Dictionary<string, object> gestureList = new Dictionary<string, object>();
    Dictionary<string, object> selectedItems = new Dictionary<string, object>();
    IEnumerator<String> gestureEnumerator;

    String status;
    Timer timer;
    Boolean started = false, gestureStop = false;
    Random rnd = new Random();

    public TrainingManager() {
      var gestures = Properties.Resources.Gestures.Split(new char[] { '\r', '\n' },
          StringSplitOptions.RemoveEmptyEntries);
      foreach (var s in gestures) {
        gestureList.Add(s, null);
        selectedItems.Add(s, null);
      }
      NumRepitions = DefaultNumRepitions;
      Pid = DefaultPid;
      ShowStop = true;
    }

    /// <summary>
    /// Starts gesture training recording procedure.
    /// </summary>
    public void Start() {
      gestureEnumerator = new GestureList(new List<String>(selectedItems.Keys),
                                          NumRepitions).GetRandomList();
      gestureStop = false;
      Status = "Starting...";
      timer = new Timer(1000);
      timer.Elapsed += new ElapsedEventHandler(OnTimeEvent);
      timer.Enabled = true;
    }

    void OnPropertyChanged(String propName) {
      if (PropertyChanged != null) {
        PropertyChanged(this, new PropertyChangedEventArgs(propName));
      }
    }

    void OnTimeEvent(object source, ElapsedEventArgs e) {
      if (!started) {
        started = true;
        timer.Interval = StartWaitTime;
        TrainingEvent(this, new TrainingEventArgs(TrainingEventType.Start));
        return;
      }

      if (!gestureStop) {
        timer.Interval = NextWaitTime();
        if (gestureEnumerator.MoveNext()) {
          var gesture = gestureEnumerator.Current;
          Status = String.Format("{0}", gesture);
          TrainingEvent(this, new TrainingEventArgs(TrainingEventType.StartGesture, gesture));
        } else {
          timer.Enabled = false;
          timer.Dispose();
          Status = "Done";
          TrainingEvent(this, new TrainingEventArgs(TrainingEventType.End));
        }
      } else {
        if (ShowStop) {
          timer.Interval = GestureStopWaitTime;
          Status = "Stop";
        }
      }

      if (ShowStop)
        gestureStop = !gestureStop;
    }

    int NextWaitTime() {
      var minValue = ShowStop ? GestureWaitTime : GestureWaitTimeShort;
      return rnd.Next(minValue, GestureMaxWaitTime);
    }
  }
}
