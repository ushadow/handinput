using System;
using System.Collections.Generic;

namespace HandInput.Util {
  public class DataBuffer {
    Dictionary<Type, Queue<Object>> queueDict = new Dictionary<Type, Queue<Object>>();

    int capacity;
    public DataBuffer(int capacity) {
      this.capacity = capacity;
    }

    /// <summary>
    /// Put the item to the end of the queue. If the queue is full, dequeue the first item.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="item"></param>
    public void Enqueue<T>(T item) {
      Queue<Object> queue;
      Type t = item.GetType();
      var found = queueDict.TryGetValue(t, out queue);
      if (!found) {
        queue = new Queue<Object>();
        queueDict.Add(t, queue);
      }
      if (queue.Count >= capacity)
        queue.Dequeue();
      queue.Enqueue(item);
    }

    /// <summary>
    /// Returns the item at the beginning of a queue without removing it. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t"></param>
    /// <returns></returns>
    public T Peek<T>(Type t) {
      Queue<Object> queue;
      var found = queueDict.TryGetValue(t, out queue);
      if (found) {
        return (T) queue.Peek();
      }
      return default(T);
    }
  }
}
