using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace GesturesViewer {
  public class ColorConverter : IValueConverter {
    static readonly Brush[] brushes = new Brush[] { Brushes.Blue, Brushes.Orange };
    
    int count = 0;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      var brush = Brushes.Black;

      if (value == null)
        return brush;

      var s = value.ToString().ToLower();
      switch (s) {
        case "stop":
        case "done":
        case "starting...":
          return Brushes.Red;
        default:
          return brushes[(count++) % brushes.Count()] ;
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      return Brushes.Black;
    }
  }
}
