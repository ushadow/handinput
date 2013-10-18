using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandInput.Engine {
  static class Parameters {
    public static int FeatureImageWidth {
      get {
        return featureImageWidth;
      }

      set {
        featureImageWidth = value;
      }
    }

    public static String ModelFile {
      get {
        return modelFile;
      }

      set {
        modelFile = value;
      }
    }

    // Default values;
    static int featureImageWidth = 64;
    static String modelFile = "G:\\salience\\model.mat";
  }
}
