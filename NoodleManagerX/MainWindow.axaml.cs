using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using NoodleManagerX.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NoodleManagerX
{
    public class MainWindow : Window
    {
        public static MainWindow s_instance;
        public static BrushConverter BrushConverter = new BrushConverter();

        private Grid blackBar;
        private bool lastleftclick = false;
        private bool lastHandled = false;
        private Point lastclickposition;

        public MainWindow()
        {
            s_instance = this;

            this.DataContext = new MainViewModel();

            InitializeComponent();

            blackBar = this.FindControl<Grid>("BlackBar");

            TextBox searchBox = this.FindControl<TextBox>("SearchBox");
            searchBox.KeyDown += SearchBoxKeyEvent;

#if DEBUG
            this.AttachDevTools();
#endif
#pragma warning disable 0618
            Application.Current.InputManager.Process.Subscribe(x =>
            {
                if (x is RawPointerEventArgs rawpointerevent)
                {
                    MouseDevice mouse = (MouseDevice)x.Device;
                    bool leftclick = rawpointerevent.InputModifiers == RawInputModifiers.LeftMouseButton;

                    Point mouseonclient = this.PointToClient(mouse.Position);

                    if (!lastleftclick && leftclick && blackBar.IsPointerOver)//rising edge
                    {
                        lastleftclick = true;
                        lastclickposition = mouseonclient;
                        lastHandled = x.Handled;
                        mouse = (MouseDevice)x.Device;
                    }
                    else if (lastleftclick && leftclick)//hold
                    {
                        if (!lastHandled)
                        {
                            PixelPoint p = new PixelPoint(mouse.Position.X - (int)lastclickposition.X, mouse.Position.Y - (int)lastclickposition.Y);
                            this.Position = p;
                        }
                    }
                    else//falling edge
                    {
                        lastleftclick = false;
                        lastHandled = false;
                    }
                }
            });
#pragma warning disable 0618
        }

        private void SearchBoxKeyEvent(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                ((MainViewModel)this.DataContext).GetPage();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static Brush GetBrush(string colorResource)
        {
            try
            {
                return s_instance.Resources[colorResource] as Brush;
            }
            catch { }

            return (Brush)BrushConverter.ConvertFromString("#ff00ff");
        }
    }

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
