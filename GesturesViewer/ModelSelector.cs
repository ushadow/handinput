using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Common.Logging;
using System.ComponentModel;

namespace GesturesViewer {
  class ModelSelector : INotifyPropertyChanged {
    static readonly ILog Log = LogManager.GetCurrentClassLogger();
    static readonly String ModelFilePattern = "model*.mat";
    static readonly String BaseModelName = "model_base.mat";

    public event PropertyChangedEventHandler PropertyChanged;

    public List<String> ModelFiles { get; private set; }
    public String SelectedModel {
      get {
        return selectedModel;
      }
      set {
        selectedModel = value;
        OnPropteryChanged("SelectedModel");
      }
    }

    String selectedModel;

    public ModelSelector(String dir) {
      var files = Directory.GetFiles(dir, ModelFilePattern);
      ModelFiles = new List<string>();
      foreach (var f in files) {
        ModelFiles.Add(f);
        if (!f.Equals(BaseModelName) && SelectedModel == null)
          SelectedModel = f;
      }
      Log.Debug(SelectedModel); 
    }

    void OnPropteryChanged(String prop) {
      if (PropertyChanged != null)
        PropertyChanged(this, new PropertyChangedEventArgs(prop));
    }
  }
}
