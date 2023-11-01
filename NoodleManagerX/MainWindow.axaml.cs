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
    public partial class MainWindow : Window
    {
        public static MainWindow s_instance;
        public static BrushConverter BrushConverter = new BrushConverter();

        private Grid blackBar;
        private bool lastleftclick = false;
        private bool lastHandled = false;
        private PixelPoint lastposition;

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
            //Application.Current.InputManager.Process.Subscribe(x =>
            //{
            //    if (x is RawPointerEventArgs rawpointerevent)
            //    {
            //        MouseDevice mouse = (MouseDevice)x.Device;
            //        bool leftclick = rawpointerevent.InputModifiers == RawInputModifiers.LeftMouseButton;

            //        //Point mouseonclient = this.PointToClient(mouse.Position);

            //        if (!lastleftclick && leftclick && blackBar.IsPointerOver)//rising edge
            //        {
            //            lastleftclick = true;
            //            lastposition = mouse.Position;
            //            lastHandled = x.Handled;
            //            mouse = (MouseDevice)x.Device;
            //        }
            //        else if (lastleftclick && leftclick)//hold
            //        {
            //            if (!lastHandled)
            //            {
            //                PixelPoint p = new PixelPoint(this.Position.X + mouse.Position.X - lastposition.X, this.Position.Y + mouse.Position.Y - (int)lastposition.Y);
            //                lastposition = mouse.Position;
            //                this.Position = p;
            //            }
            //        }
            //        else//falling edge
            //        {
            //            lastleftclick = false;
            //            lastHandled = false;
            //        }
            //    }
            //});
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
}
