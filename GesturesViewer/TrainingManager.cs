using System;
using System.Linq;
using System.Timers;
using System.ComponentModel;

using Common.Logging;

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
    static readonly int GestureWaitTime = 3000; //ms
    static readonly int StartWaitTime = 8000;
    static readonly int NumRepitions = 3;
    static readonly int StartRepCount = 1;
    static readonly ILog Log = LogManager.GetCurrentClassLogger();

    public String Status {
      get {
        return status;
      }
      set {
        status = value;
        OnPropertyChanged("Status");
      }
    }
    public event PropertyChangedEventHandler PropertyChanged;
    public event EventHandler<TrainingEventArgs> TrainingEvent;

    String[] gestures;
    String status;
    Timer timer = new Timer(1000);
    Int32 counter = 0, repCounter = StartRepCount;
    Boolean started = false;

    public TrainingManager() {

      gestures = Properties.Resources.Gestures.Split(new char[] { '\r', '\n' },
          StringSplitOptions.RemoveEmptyEntries);
      timer.Elapsed += new ElapsedEventHandler(OnTimeEvent);
    }

    /// <summary>
    /// Starts gesture training recording procedure.
    /// </summary>
    public void Start() {
      Status = "Starting...";
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
      if (counter < gestures.Count()) {
        var gesture = gestures[counter];
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
