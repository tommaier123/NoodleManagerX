using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using NoodleManagerX.Models;
using System;
using System.Globalization;
using System.Linq;

namespace NoodleManagerX
{
    public class MainWindow : Window
    {
        private BrushConverter brushConverter = new BrushConverter();

        private const string tabActiveColor = "#f91c85";
        private const string tabInactiveColor = "#aa49e0";
        private const string difficultyActiveColor = "#ffffff";
        private const string difficultyInactiveColor = "#888888";
        private const string downloadActiveColor = "#ffffff";
        private const string downloadInactiveColor = "#888888";
        public static Brush tabActiveBrush;
        public static Brush tabInactiveBrush;
        public static Brush difficultyActiveBrush;
        public static Brush difficultyInactiveBrush;
        public static Brush downloadActiveBrush;
        public static Brush downloadInactiveBrush;

        private Grid blackBar;
        private MouseDevice mouse;
        private bool lastleftclick = false;
        private bool lastHandled = false;
        private Point lastclickposition;

        public MainWindow()
        {
            tabActiveBrush = (Brush)brushConverter.ConvertFromString(tabActiveColor);
            tabInactiveBrush = (Brush)brushConverter.ConvertFromString(tabInactiveColor);
            difficultyActiveBrush = (Brush)brushConverter.ConvertFromString(difficultyActiveColor);
            difficultyInactiveBrush = (Brush)brushConverter.ConvertFromString(difficultyInactiveColor);
            downloadActiveBrush = (Brush)brushConverter.ConvertFromString(downloadActiveColor);
            downloadInactiveBrush = (Brush)brushConverter.ConvertFromString(downloadInactiveColor);

            InitializeComponent();

            this.DataContext = new MainViewModel();
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
                    if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        Point mouseonclient = desktop.MainWindow.PointToClient(mouse.Position);

                        if (!lastleftclick && leftclick && blackBar.IsPointerOver)//rising edge
                        {
                            lastleftclick = true;
                            lastclickposition = mouseonclient;
                            lastHandled = x.Handled;
                        }
                        else if (lastleftclick && leftclick && lastclickposition != null)//hold
                        {
                            if (!lastHandled)
                            {
                                PixelPoint p = new PixelPoint(mouse.Position.X - (int)lastclickposition.X, mouse.Position.Y - (int)lastclickposition.Y);
                                desktop.MainWindow.Position = p;
                            }
                        }
                        else//falling edge
                        {
                            lastleftclick = false;
                            lastHandled = false;
                        }
                    }
                }
            });
        }

        private void SearchBoxKeyEvent(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                ((MainViewModel)this.DataContext).GetMapPage();
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
                throw new ArgumentNullException(nameof(value));
            }
            int index = Int32.Parse(parameter.ToString());
            string[] difficulties = (string[])value;
            bool present = false;

            switch (index)
            {
                case 1:
                    present = difficulties.Contains("Easy");
                    break;
                case 2:
                    present = difficulties.Contains("Normal");
                    break;
                case 3:
                    present = difficulties.Contains("Hard");
                    break;
                case 4:
                    present = difficulties.Contains("Expert");
                    break;
                case 5:
                    present = difficulties.Contains("Master");
                    break;
                case 6:
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

    public class DownloadColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            return (!(bool)value) ? MainWindow.downloadActiveBrush : MainWindow.downloadInactiveBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
