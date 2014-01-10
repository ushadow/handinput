using System;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Kinect;
using HandInput.Util;

namespace HandInput.UtilTest {
  [TestClass]
  public class CoordinateConverterTests {
    CoordinateConverter converter;
    [TestInitialize()]
    public void Initialize() {
      var kinectParams = HandInputParams.GetKinectParams(
            Properties.Resources.ColorToDepthRelationalParameters);
      converter = new CoordinateConverter(new CoordinateMapper(kinectParams),
          HandInputParams.ColorImageFormat, HandInputParams.DepthImageFormat);
    }

    [TestMethod]
    public void TestMapDepthPointToColorRect() {
      var colorPoint = converter.MapDepthPointToColorPoint(230, 600, 1000);
      Console.WriteLine(String.Format("X: {0}, Y: {1}", colorPoint.X, colorPoint.Y));
      var rect = new Rectangle(-1, -1, -5, -6);
      Console.WriteLine(rect);
    }
  }
}
