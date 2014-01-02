using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

using Common.Logging;

namespace HandInput.Util {
  public class SensorDataAnalyzer {
    static readonly ILog Log = LogManager.GetCurrentClassLogger(); 
    Background depthBg, colorBg; 
    Single[, ,] depthData, colorData;
    int count = 0, width, height;
    Image<Gray, Single> grayImage;

    public SensorDataAnalyzer(int width, int height) {
      this.width = width;
      this.height = height;
      depthBg = new Background(width, height);
      colorBg = new Background(width, height);
      depthData = new Single[height, width, 1];
      colorData = new Single[height, width, 3];
      grayImage = new Image<Gray, Single>(width, height);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="depthRaw">not null</param>
    /// <param name="colorRaw">not null</param>
    public void Update(short[] depthRaw, byte[] colorRaw) {
      if (count < 100) {
        depthBg.AccumulateBackground(ImageUtil.CreateCVImage(depthRaw, depthData, width, height));
        var bgrImage = ImageUtil.CreateCVImage(colorRaw, colorData, width, height);
        CvInvoke.cvCvtColor(bgrImage.Ptr, grayImage.Ptr, COLOR_CONVERSION.CV_BGR2GRAY);
        colorBg.AccumulateBackground(grayImage);
        count++;
      } else if (!depthBg.ModelsCreated) {
        depthBg.CreateModelsFromStats();
        colorBg.CreateModelsFromStats();
        Log.DebugFormat("depth: {0}", depthBg);
        Log.DebugFormat("color: {0}", colorBg);
      }
    }
  }
}
