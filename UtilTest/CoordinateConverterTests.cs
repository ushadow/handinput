using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Kinect;
using HandInput.Util;
 
namespace HandInput.UtilTest {
  [TestClass]
  public class CoordinateConverterTests {
    CoordinateConverter converter;
    [TestInitialize()]
    public void Initialize() {
      var mapper = new CoordinateMapper(Properties.Resources.ColorToDepthRelationalParameters);
      converter = new CoordinateConverter(mapper, HandInputParams.ColorImageFormat, 
                                          HandInputParams.DepthImageFormat);
    }
    
    [TestMethod]
    public void TestMapDepthPointToColorRect() {
      var colorPoint = converter.MapDepthPointToColorPoint(230, 480, 500);
      Console.WriteLine(colorPoint);
      Assert.AreEqual(colorPoint.X, 0);
    }
  }
}
