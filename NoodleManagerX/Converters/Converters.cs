using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Converters
{
    public class EqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            string val = value.ToString();
            var tmp = parameter.ToString().Split("-").Where(x => x == val);

            return tmp.Count() > 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TabHilightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            return (value.ToString() == parameter.ToString()) ? MainWindow.GetBrush("TabActiveColor") : MainWindow.GetBrush("TabInactiveColor");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class DifficultyColorConverter : IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null || values == null || values.Count != 2) throw new ArgumentNullException();

            // Some items don't have difficulties...
            if (values[0] == null || values[1] == null)
            {
                return MainWindow.GetBrush("DifficultyInactiveDownloadedColor");
            }

            if (values[0].GetType() == typeof(Avalonia.UnsetValueType) || values[1].GetType() == typeof(Avalonia.UnsetValueType))
            {
                return MainWindow.GetBrush("DifficultyInactiveDownloadedColor");
            }

            int index = Int32.Parse(parameter.ToString());
            string[] difficulties = (string[])values[0];
            bool present = false;

            switch (index)
            {
                case 0:
                    present = difficulties.Contains("Easy");
                    break;
                case 1:
                    present = difficulties.Contains("Normal");
                    break;
                case 2:
                    present = difficulties.Contains("Hard");
                    break;
                case 3:
                    present = difficulties.Contains("Expert");
                    break;
                case 4:
                    present = difficulties.Contains("Master");
                    break;
                case 5:
                    present = difficulties.Contains("Custom");
                    break;
            }

            if (present)
            {
                if ((bool)values[1])
                {
                    return MainWindow.GetBrush("DifficultyActiveDownloadedColor");
                }
                else
                {
                    return MainWindow.GetBrush("DifficultyActiveNotDownloadedColor");
                }
            }
            else
            {
                if ((bool)values[1])
                {
                    return MainWindow.GetBrush("DifficultyInactiveDownloadedColor");
                }
                else
                {
                    return MainWindow.GetBrush("DifficultyInactiveNotDownloadedColor");
                }
            }
        }

        public object ConvertBack(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) throw new ArgumentNullException();

            string[] paths = ((string)parameter).Split("|");
            if (paths.Length > 1)
            {
                return ((bool)value) ? paths[1] : paths[0];
            }
            else
            {
                return parameter;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TwoParameterPathConverter : IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null || values == null || values.Count != 2) throw new ArgumentNullException();

            string[] paths = ((string)parameter).Split("|");
            if (paths.Length != 4) throw new ArgumentOutOfRangeException(nameof(paths));

            if (values[0].GetType() == typeof(Avalonia.UnsetValueType) || values[1].GetType() == typeof(Avalonia.UnsetValueType))
            {
                return paths[1];
            }

            if ((bool)values[1])//blacklisted
            {
                if ((bool)values[0])//downloaded
                {
                    return paths[3];
                }
                else
                {
                    return paths[2];
                }
            }
            if ((bool)values[0])//downloaded
            {
                return paths[1];
            }
            return paths[0];
        }

        public object ConvertBack(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;
            return !String.IsNullOrEmpty((string)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string par = parameter.ToString();
            if (value == null || String.IsNullOrEmpty(par)) throw new ArgumentNullException();

            string[] colors = ((string)parameter).Split("|");
            if (colors.Length != 2) throw new ArgumentOutOfRangeException(nameof(colors));

            return ((bool)value) ? MainWindow.GetBrush(colors[0]) : MainWindow.GetBrush(colors[1]);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts two boolean values into one of four colors.
    /// The order of the parameter is FF|TF|FT|TT, with TF being the default
    /// </summary>
    public class TwoParameterColorConverter : IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null || values == null || values.Count != 2) throw new ArgumentNullException();

            string[] colors = ((string)parameter).Split("|");
            if (colors.Length != 4) throw new ArgumentOutOfRangeException(nameof(colors));

            if (values[0].GetType() == typeof(Avalonia.UnsetValueType) || values[1].GetType() == typeof(Avalonia.UnsetValueType))
            {
                return MainWindow.GetBrush(colors[1]);
            }

            if ((bool)values[1])//blacklisted
            {
                if ((bool)values[0])//downloaded
                {
                    return MainWindow.GetBrush(colors[3]);
                }
                else
                {
                    return MainWindow.GetBrush(colors[2]);
                }
            }
            if ((bool)values[0])//downloaded
            {
                return MainWindow.GetBrush(colors[1]);
            }
            return MainWindow.GetBrush(colors[0]);
        }

        public object ConvertBack(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NotConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool)) throw new InvalidOperationException("Not bool");

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
