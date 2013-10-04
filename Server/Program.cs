using System;
using System.Threading;

using Common.Logging;
using WebSocket;

namespace HandInput.Server {
  public class Program {
    private static readonly ILog log = LogManager.GetCurrentClassLogger();

    public static void Main(String[] args) {
      var server = new HandInputServer("127.0.0.1", 8080);

      server.Start();
      Console.Out.WriteLine("Press ENTER to exit.");
      Console.In.ReadLine();
      server.Stop();
    }
  }
}
