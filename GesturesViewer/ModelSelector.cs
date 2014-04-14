using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Text.RegularExpressions;

using Common.Logging;

namespace GesturesViewer {
  class ModelSelector : INotifyPropertyChanged {
    static readonly String ModelFilePattern = "*.mat";
    static readonly String TimeRegex = @"\d{4}-\d{2}-\d{2}_\d{2}-\d{2}.mat$";

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

    String selectedModel, dir;

    public ModelSelector(String dir) {
      this.dir = dir;
      ModelFiles = new List<String>();
      Refresh();
    }

    public void Refresh() {
      var files = Directory.GetFiles(dir, ModelFilePattern);
      ModelFiles.Clear();
      foreach (var f in files) {
        ModelFiles.Add(f);
        if (SelectedModel == null || Regex.IsMatch(f, TimeRegex)) 
          SelectedModel = f;
      }
    }

    void OnPropteryChanged(String prop) {
      if (PropertyChanged != null)
        PropertyChanged(this, new PropertyChangedEventArgs(prop));
    }
  }
}
