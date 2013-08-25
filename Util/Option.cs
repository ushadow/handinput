using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HandInput.Util
{
  /// <summary>
  /// Option type, because C# doesn't have it built-in
  /// http://litemedia.info/option-type-implementation-in-csharp
  /// Original author: Mikael Lundin, Retrieved on Apr 14, 2012
  /// </summary>

  // Used as return type from method
  public abstract class Option<T>
  {
    // Could contain the value if Some, but not if None
    public abstract T Value { get; }

    public abstract bool IsSome { get; }

    public abstract bool IsNone { get; }
  }

  public sealed class Some<T> : Option<T>
  {
    private T value;
    public Some(T value)
    {
      // Setting Some to null, nullifies the purpose of Some/None
      if (value == null)
      {
        throw new System.ArgumentNullException("value", "Some value was null, use None instead");
      }

      this.value = value;
    }

    public override T Value { get { return value; } }

    public override bool IsSome { get { return true; } }

    public override bool IsNone { get { return false; } }
  }

  public sealed class None<T> : Option<T>
  {
    public override T Value
    {
      get { throw new System.NotSupportedException("There is no value"); }
    }

    public override bool IsSome { get { return false; } }

    public override bool IsNone { get { return true; } }
  }

}