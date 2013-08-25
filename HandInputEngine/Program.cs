using System;
using System.Threading;

using Common.Logging;

namespace HandInput.HandInputEngine
{
  public class Program
  {
    private static readonly ILog log = LogManager.GetCurrentClassLogger();

    public static void Main(String[] args)
    {
      new HandInputEngine();
      Console.Out.WriteLine("Press ENTER to exit.");
      Console.In.ReadLine();
    }
  }
}
