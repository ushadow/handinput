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
    static readonly int GestureWaitTime = 3000; //ms
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

    public event PropertyChangedEventHandler PropertyChanged;
    public event EventHandler<TrainingEventArgs> TrainingEvent;

    Dictionary<string, object> gestureList = new Dictionary<string, object>();
    Dictionary<string, object> selectedItems = new Dictionary<string, object>();
    IEnumerator<String> gestureEnumerator;

    String status;
    Timer timer = new Timer(1000);
    Boolean started = false, gestureStop = false;

    public TrainingManager() {
      var gestures = Properties.Resources.Gestures.Split(new char[] { '\r', '\n' },
          StringSplitOptions.RemoveEmptyEntries);
      foreach (var s in gestures) {
        gestureList.Add(s, null);
        selectedItems.Add(s, null);
      }
      NumRepitions = DefaultNumRepitions;
      Pid = DefaultPid;
    }

    /// <summary>
    /// Starts gesture training recording procedure.
    /// </summary>
    public void Start() {
      gestureEnumerator = new GestureList(new List<String>(selectedItems.Keys),
                                          NumRepitions).GetOrderedList();
      Status = "Starting...";
      timer.Elapsed += new ElapsedEventHandler(OnTimeEvent);
      timer.Enabled = true;
    }

    private void OnPropertyChanged(String propName) {
      if (PropertyChanged != null) {
        PropertyChanged(this, new PropertyChangedEventArgs(propName));
      }
    }

    private void OnTimeEvent(object source, ElapsedEventArgs e) {
      if (!started) {
        started = true;
        timer.Interval = StartWaitTime;
        TrainingEvent(this, new TrainingEventArgs(TrainingEventType.Start));
        return;
      }

      if (!gestureStop) {
        timer.Interval = GestureWaitTime;
        if (gestureEnumerator.MoveNext()) {
          var gesture = gestureEnumerator.Current;
          Status = String.Format("{0}", gesture);
          TrainingEvent(this, new TrainingEventArgs(TrainingEventType.StartGesture, gesture));
        } else {
          timer.Enabled = false;
          Status = "Done";
          TrainingEvent(this, new TrainingEventArgs(TrainingEventType.End));
        }
      } else {
        timer.Interval = GestureStopWaitTime;
        Status = "Stop";
      }
      gestureStop = !gestureStop;
    }

  }
}
