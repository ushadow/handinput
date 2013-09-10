using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.ComponentModel;

using Common.Logging;

namespace GesturesViewer {

  public enum TrainingEventType {Start, End};
  
  public class TrainingEventArgs {
    public TrainingEventType Type { get; private set; }
    public TrainingEventArgs(TrainingEventType e) {
      Type = e;
    }
  }

  public delegate void TrainingEventHandler(TrainingManager sender, TrainingEventArgs e);

  public class TrainingManager : INotifyPropertyChanged {
    private static readonly int WaitTime = 3000; //ms
    private static readonly int NumRepitions = 3;
    private static readonly int StartRepCount = 1;
    private static readonly ILog Log = LogManager.GetCurrentClassLogger();

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
    public event TrainingEventHandler TrainingEvent;

    private String[] gestures;
    private String status;
    private Timer timer = new Timer(WaitTime);
    private Int32 counter = 0, repCounter = StartRepCount;

    public TrainingManager() {
      
      gestures = Properties.Resources.Gestures.Split(new char[] { '\r', '\n' },
          StringSplitOptions.RemoveEmptyEntries);
      timer.Elapsed += new ElapsedEventHandler(OnTimeEvent);
    }

    /// <summary>
    /// Starts gesture training recording procedure.
    /// </summary>
    public void Start() {
      timer.Enabled = true;
      Status = "Starting...";
      TrainingEvent(this, new TrainingEventArgs(TrainingEventType.Start));
    }

    private void OnPropertyChanged(String propName) {
      if (PropertyChanged != null) {
        PropertyChanged(this, new PropertyChangedEventArgs(propName));
      }
    }

    private void OnTimeEvent(object source, ElapsedEventArgs e) {
      if (counter < gestures.Count()) {
        Status = String.Format("{0} #{1}", gestures[counter], repCounter);
        if (repCounter == NumRepitions) {
          repCounter = StartRepCount;
          counter++;
        } else {
          repCounter++;
        }
      } else {
        TrainingEvent(this, new TrainingEventArgs(TrainingEventType.End));
        timer.Enabled = false;
        Status = "Done";
      }
    }

  }
}
