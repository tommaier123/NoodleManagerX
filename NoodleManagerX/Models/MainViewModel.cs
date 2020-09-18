using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia;
using Avalonia.Input;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Native;
using Avalonia.Input.Raw;

namespace NoodleManagerX.Models
{
    class MainViewModel : ReactiveObject
    {
        //dotnet publish -c Release -f netcoreapp3.1 -r win-x64 --self-contained true /p:PublishSingleFile=true
        //dotnet publish -c Release -f netcoreapp3.1 -r linux-x64 --self-contained true /p:PublishSingleFile=true
        //dotnet publish -c Release -f netcoreapp3.1 -r osx-x64 --self-contained true /p:PublishSingleFile=true


        [Reactive] public string Name { get; set; } = "Name";
        [Reactive] public string Greeting { get; set; } = "Greetings";

        public ReactiveCommand<Unit, Unit> NameCommand { get; set; }
        public ReactiveCommand<Unit, Unit> MinimizeCommand { get; set; }
        public ReactiveCommand<Unit, Unit> NormalCommand { get; set; }
        public ReactiveCommand<Unit, Unit> MaximizeCommand { get; set; }
        public ReactiveCommand<Unit, Unit> CloseCommand { get; set; }




        private MouseDevice mouse;
        private bool lastleftclick = false;
        private Point lastclickposition;

        public MainViewModel()
        {
            NameCommand = ReactiveCommand.Create((Action<Unit>)(x => Greeting = "Greetings " + Name));

            MinimizeCommand = ReactiveCommand.Create((Action<Unit>)(x =>
            {
                if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow.WindowState = WindowState.Minimized;
                }
            }));

            NormalCommand = ReactiveCommand.Create((Action<Unit>)(x =>
            {
                if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow.WindowState = WindowState.Normal;
                }
            }));

            MaximizeCommand = ReactiveCommand.Create((Action<Unit>)(x =>
            {
                if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow.WindowState = WindowState.Maximized;
                }
            }));

            CloseCommand = ReactiveCommand.Create((Action<Unit>)(x =>
            {
                if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow.Close();
                }
            }));


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
                        bool onwindow = mouseonclient.X >= 0 && mouseonclient.Y >= 0;

                        if (!lastleftclick && leftclick && onwindow)//rising edge
                        {
                            lastleftclick = true;
                            lastclickposition = mouseonclient;
                        }
                        else if (lastleftclick && leftclick && lastclickposition != null)//hold
                        {
                            PixelPoint p = new PixelPoint(mouse.Position.X - (int)lastclickposition.X, mouse.Position.Y - (int)lastclickposition.Y);
                            desktop.MainWindow.Position = p;
                        }
                        else//falling edge
                        {
                            lastleftclick = false;
                        }
                    }
                }
            });
        }
    }
}
