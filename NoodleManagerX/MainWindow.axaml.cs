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
        public static BrushConverter brushConverter = new BrushConverter();

        private const string tabActiveColor = "#f91c85";
        private const string tabInactiveColor = "#aa49e0";
        private const string difficultyActiveColor = "#ffffff";
        private const string difficultyInactiveColor = "#888888";
        public static Brush tabActiveBrush;
        public static Brush tabInactiveBrush;
        public static Brush difficultyActiveBrush;
        public static Brush difficultyInactiveBrush;

        private Grid blackBar;
        public MouseDevice mouse;
        private bool lastleftclick = false;
        private bool lastHandled = false;
        private Point lastclickposition;

        public MainWindow()
        {
            s_instance = this;

            tabActiveBrush = (Brush)brushConverter.ConvertFromString(tabActiveColor);
            tabInactiveBrush = (Brush)brushConverter.ConvertFromString(tabInactiveColor);
            difficultyActiveBrush = (Brush)brushConverter.ConvertFromString(difficultyActiveColor);
            difficultyInactiveBrush = (Brush)brushConverter.ConvertFromString(difficultyInactiveColor);

            this.DataContext = new MainViewModel();

            InitializeComponent();

            blackBar = this.FindControl<Grid>("BlackBar");

            TextBox searchBox = this.FindControl<TextBox>("SearchBox");
            searchBox.KeyDown += SearchBoxKeyEvent;

#if DEBUG
            this.AttachDevTools();
#endif

            Application.Current.InputManager.Process.Subscribe(x =>
            {
                if (mouse == null)
                {
                    mouse = (MouseDevice)x.Device;
                }
                if (x is RawPointerEventArgs rawpointerevent)
                {
                    bool leftclick = rawpointerevent.InputModifiers == RawInputModifiers.LeftMouseButton;

                    Point mouseonclient = this.PointToClient(mouse.Position);

                    if (!lastleftclick && leftclick && blackBar.IsPointerOver)//rising edge
                    {
                        lastleftclick = true;
                        lastclickposition = mouseonclient;
                        lastHandled = x.Handled;
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
    }

    public class EqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
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
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return (value.ToString() == parameter.ToString()) ? MainWindow.tabActiveBrush : MainWindow.tabInactiveBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DifficultyColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return false;
            }

            int index = Int32.Parse(parameter.ToString());
            string[] difficulties = (string[])value;
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
            return (present) ? MainWindow.difficultyActiveBrush : MainWindow.difficultyInactiveBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DifficultyPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            string[] paths = ((string)parameter).Split("|");
            if (paths.Length > 1)
            {
                return ((bool)value) ? paths[0] : paths[1];
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
            if (parameter == null)
            {
                throw new ArgumentNullException();
            }

            if (values == null || values.Count != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(values));
            }

            string[] paths = ((string)parameter).Split("|");

            if (paths.Count() > 4)
            {
                return parameter;
            }

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
            if (value == null)
            {
                return false;
            }
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
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            string[] par = parameter.ToString().Split("|");
            if (par.Length != 2)
            {
                throw new ArgumentNullException(nameof(value));
            }
            return ((bool)value) ? (Brush)MainWindow.brushConverter.ConvertFromString(par[0]) : (Brush)MainWindow.brushConverter.ConvertFromString(par[1]);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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
}
