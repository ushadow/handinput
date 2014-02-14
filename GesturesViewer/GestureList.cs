using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GesturesViewer {
  public class GestureList {

    IList<String> allGestures = new List<String>();

    public GestureList(IList<String> gestures, int nRepetitions) {
      foreach (var s in gestures) {
        for (int i = 0; i < nRepetitions; i++)
          allGestures.Add(s);
      }
    }

    public IEnumerator<String> GetOrderedList() {
      return allGestures.GetEnumerator();
    }

    public IEnumerator<String> GetRandomList() {
      // Use time-dependent default seed.
      Random rnd = new Random();

      return allGestures.OrderBy(x => rnd.Next()).GetEnumerator();
    }
  }
}
