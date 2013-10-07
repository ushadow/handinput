using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Common.Logging;

namespace HandInput.Util {
  public class ParallelProcessor {
    private static readonly ILog Log = LogManager.GetLogger(typeof(ParallelProcessor));

    /// <summary>
    /// The initial state is nonsignaled, and the processor will wait until Set
    /// method is called.
    /// </summary>
    private ManualResetEvent doneEvent = new ManualResetEvent(false);
    private int numberOfThreadsNotCompleted = 0;

    public void Spawn(Action action) {
      Interlocked.Increment(ref numberOfThreadsNotCompleted);
      ThreadPool.QueueUserWorkItem(DoWork, action);
    }

    public void WaitAll() {
      if (numberOfThreadsNotCompleted > 0) {
        doneEvent.WaitOne();
        doneEvent.Reset(); // Sets the state of the event to nonsignaled.
      }
    }

    /// <summary>
    /// Work done in a thread.
    /// </summary>
    /// <param name="action"></param>
    private void DoWork(Object action) {
      try {
        ((Action)action)();
      } catch (Exception ex) {
        Log.Error(ex.InnerException.Message);
      } finally {
        if (Interlocked.Decrement(ref numberOfThreadsNotCompleted) == 0)
          doneEvent.Set();
      }
    }
  }
}
