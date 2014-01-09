using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Aramis.Packages.KinectSDK.Services.Recorder;

using Common.Logging;

namespace HandInput.OfflineProcessor {
  public static class Extensions {
    static readonly ILog Log = LogManager.GetCurrentClassLogger();

    public static Byte[] GetKinectParams(this KinectReplay player) {
      Log.Debug("GetKinectParams called.");
      var bf = new BinaryFormatter();
      var stream = new MemoryStream(Properties.Resources.ColorToDepthRelationalParameters);
      IEnumerable<byte> kinectParams = bf.Deserialize(stream) as IEnumerable<byte>;
      stream.Close();
      return kinectParams.ToArray<byte>();
    }
  }
}
