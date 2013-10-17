using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using handinput;

namespace HandInput.Engine {
  public class RecognitionEngine {

    MProcessor processor = new MProcessor(Parameters.FeatureImageWidth, 
        Parameters.FeatureImageWidth, Parameters.ModelFile);
    public void Update(TrackingResult result) {

    }
  }
}