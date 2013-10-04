using System;
using System.Threading;

using Common.Logging;
using WebSocket;

namespace HandInput.Engine
{
  public class Program
  {
    private static readonly ILog log = LogManager.GetCurrentClassLogger();

    public static void Main(String[] args)
    {
      Console.Out.WriteLine("Press ENTER to exit.");
      Console.In.ReadLine();
    }
  }
}
