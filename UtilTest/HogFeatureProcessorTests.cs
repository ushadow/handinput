using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using HandInput.Engine;

namespace HandInput.Test {
  [TestClass]
  public class HogFeatureProcessorTests {
    [TestMethod]
    public void TestConstructor() {
      HogFeatureProcessor featureProcessor = new HogFeatureProcessor();
    Assert.AreEqual(49 * 9, featureProcessor.DescriptorLength);
    }
  }
}
