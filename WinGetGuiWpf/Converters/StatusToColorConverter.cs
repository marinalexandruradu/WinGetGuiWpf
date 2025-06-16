using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WinGetGuiWpf.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                "Success" => Brushes.LimeGreen,
                "Failed" => Brushes.OrangeRed,
                "Pending" => Brushes.Goldenrod,
                _ => Brushes.Gray
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
