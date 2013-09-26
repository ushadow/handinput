using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using HandInput.Engine;
using Emgu.CV;
using Emgu.CV.Structure;

using handinput;

namespace HandInput.EngineTest {
  [TestClass]
  public class FeatureProcessorTest {
    [TestMethod]
    public void TestHogDescriptor() {
      MFeatureProcessor featureProcessor = new MFeatureProcessor(64, 64);
      Image<Gray, Double> image = new Image<Gray, Double>(64, 64);
      featureProcessor.Compute(image.Ptr);
    }
  }
}
