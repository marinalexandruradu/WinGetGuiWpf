using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WinGetGuiWpf
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool CollapseInsteadOfHide { get; set; } = true;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = false;
            if (value is bool)
                flag = (bool)value;

            return flag ? Visibility.Visible :
                   (CollapseInsteadOfHide ? Visibility.Collapsed : Visibility.Hidden);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is Visibility visibility) && visibility == Visibility.Visible;
        }
    }
}
