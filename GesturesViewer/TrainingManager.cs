using System;
using System.Linq;
using System.Timers;
using System.ComponentModel;

using Common.Logging;
using System.Collections.Generic;

namespace HandInput.GesturesViewer {

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
    public static readonly String KinectDataRegex = @"KinectData_(\d+).bin";

    static readonly int GestureWaitTime = 3000; //ms
    static readonly int StartWaitTime = 8000;
    static readonly int StartRepCount = 1;
    static readonly int DefaultNumRepitions = 3;
    static readonly ILog Log = LogManager.GetCurrentClassLogger();

    public int NumRepitions { get; private set; }
    
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

    String status;
    Timer timer = new Timer(1000);
    Int32 counter, repCounter;
    Boolean started = false;

    public TrainingManager() {
      var gestures = Properties.Resources.Gestures.Split(new char[] { '\r', '\n' },
          StringSplitOptions.RemoveEmptyEntries);
      foreach (var s in gestures) {
        gestureList.Add(s, null);
        selectedItems.Add(s, null);
      }
      NumRepitions = DefaultNumRepitions;
    }

    /// <summary>
    /// Starts gesture training recording procedure.
    /// </summary>
    public void Start() {
      counter = 0;
      repCounter = StartRepCount;
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

      timer.Interval = GestureWaitTime;
      if (counter < selectedItems.Count()) {
        var gesture = selectedItems.ElementAt(counter).Key;
        Status = String.Format("{0} #{1}", gesture, repCounter);
        if (repCounter == NumRepitions) {
          repCounter = StartRepCount;
          counter++;
        } else {
          repCounter++;
        }
        TrainingEvent(this, new TrainingEventArgs(TrainingEventType.StartGesture, gesture));
      } else {
        timer.Enabled = false;
        Status = "Done";
        TrainingEvent(this, new TrainingEventArgs(TrainingEventType.End));
      }
    }

  }
}
