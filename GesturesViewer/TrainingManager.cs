using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.ComponentModel;

using Common.Logging;

namespace GesturesViewer {
  class TrainingManager : INotifyPropertyChanged {
    private static readonly int WaitTime = 3000; //ms
    private static readonly int NumRepitions = 3;
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

    private String[] gestures;
    private String status;
    private Timer timer = new Timer(WaitTime);
    private Int32 counter = 0, repCounter = 1;

    public TrainingManager() {
      gestures = GesturesViewer.Properties.Resources.Gestures.Split(new char[] { '\r', '\n' },
          StringSplitOptions.RemoveEmptyEntries);
      timer.Elapsed += new ElapsedEventHandler(OnTimeEvent);
    }

    public void Start() {
      timer.Enabled = true;
      Status = "Starting...";
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
          repCounter = 1;
          counter++;
        } else {
          repCounter++;
        }
      } else {
        timer.Enabled = false;
        Status = "Done";
      }
    }

  }
}
