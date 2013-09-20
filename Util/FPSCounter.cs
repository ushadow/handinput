using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

using Common.Logging;

namespace HandInput.Util {
  public class FPSCounter {
    static readonly ILog Log = LogManager.GetCurrentClassLogger();
    static readonly int MaxCount = 30;

    Stopwatch stopwatch = new Stopwatch();
    int frameCount = 0;

    public FPSCounter() {
      stopwatch.Start();
    }

    public void LogFPS() {
      frameCount += 1;
      if (frameCount == MaxCount) {
        var fps = frameCount * 1000 / stopwatch.ElapsedMilliseconds;
        Log.InfoFormat("FPS = {0}", fps);
        frameCount = 0;
        stopwatch.Restart();
      }
    }

  }
}
