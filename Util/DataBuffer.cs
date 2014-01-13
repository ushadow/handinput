using System;
using System.Collections.Generic;
using Emgu.CV;
using Emgu.CV.Structure;

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

    public void EnqueueAndCopy<TColor, TDepth>(Image<TColor, TDepth> item) 
        where TColor : struct, IColor 
        where TDepth : struct {
      Queue<Object> queue;
      Type t = item.GetType();
      var found = queueDict.TryGetValue(t, out queue);
      if (!found) {
        queue = new Queue<Object>();
        queueDict.Add(t, queue);
      }
      Image<TColor, TDepth> copy;
      if (queue.Count >= capacity) {
        copy = (Image<TColor, TDepth>)queue.Dequeue();
        item.CopyTo(copy);
      } else {
        copy = new Image<TColor, TDepth>(item.Width, item.Height);
        item.CopyTo(copy);
      }
      queue.Enqueue(copy);
    }

    /// <summary>
    /// Returns a reference of the item at the beginning of a queue without removing it. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t"></param>
    /// <returns></returns>
    public T Peek<T>(Type t) {
      Queue<Object> queue;
      var found = queueDict.TryGetValue(t, out queue);
      if (found) {
        return (T)queue.Peek();
      }
      return default(T);
    }
  }
}
