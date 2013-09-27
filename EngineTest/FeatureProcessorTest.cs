using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using HandInput.Engine;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

using handinput;

namespace HandInput.EngineTest {
  [TestClass]
  public class FeatureProcessorTest {
    [TestMethod]
    public void TestHogDescriptor() {
      MFeatureProcessor featureProcessor = new MFeatureProcessor(64, 64);
      Image<Bgr, Byte> image = new Image<Bgr, Byte>("../../data/pic3.png");
      Image<Gray, Byte> grayImage = new Image<Gray, Byte>(image.Width, image.Height);
      CvInvoke.cvCvtColor(image, grayImage, COLOR_CONVERSION.CV_BGR2GRAY);
      String windowName = "Test Window";
      CvInvoke.cvNamedWindow(windowName);
      CvInvoke.cvShowImage(windowName, grayImage);
      featureProcessor.Compute(image.Ptr);
      CvInvoke.cvWaitKey(0);
      CvInvoke.cvDestroyWindow(windowName);
    }
  }
}
