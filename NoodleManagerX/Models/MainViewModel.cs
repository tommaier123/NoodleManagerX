using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Net;
using Newtonsoft.Json;
using Avalonia.Threading;
using DynamicData;

namespace NoodleManagerX.Models
{
    class MainViewModel : ReactiveObject
    {
        //dotnet publish -c Release -f netcoreapp3.1 -r win-x64 --self-contained true /p:PublishSingleFile=true
        //dotnet publish -c Release -f netcoreapp3.1 -r linux-x64 --self-contained true /p:PublishSingleFile=true
        //dotnet publish -c Release -f netcoreapp3.1 -r osx-x64 --self-contained true /p:PublishSingleFile=true

        [Reactive] public int selectedTabIndex { get; set; } = 0;

        public ReactiveCommand<Unit, Unit> minimizeCommand { get; set; }
        public ReactiveCommand<Unit, Unit> toggleFullscreenCommand { get; set; }
        public ReactiveCommand<Unit, Unit> closeCommand { get; set; }
        public ReactiveCommand<string, Unit> tabSelectCommand { get; set; }

        public ObservableCollection<Map> maps { get; set; } = new ObservableCollection<Map>();

        private Thread apiMapThread;

        public MainViewModel()
        {
            minimizeCommand = ReactiveCommand.Create(() =>
            {
                if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow.WindowState = WindowState.Minimized;
                }
            });

            toggleFullscreenCommand = ReactiveCommand.Create(() =>
            {
                if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    if (desktop.MainWindow.WindowState != WindowState.Maximized)
                    {
                        desktop.MainWindow.WindowState = WindowState.Maximized;
                    }
                    else if (desktop.MainWindow.WindowState != WindowState.Normal)
                    {
                        desktop.MainWindow.WindowState = WindowState.Normal;
                    }
                }
            });

            closeCommand = ReactiveCommand.Create(() =>
            {
                if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow.Close();
                }
            });

            tabSelectCommand = ReactiveCommand.Create<string>((x =>
            {
                selectedTabIndex = Int32.Parse(x);
            }));

            apiMapThread = new Thread(GetMaps);
            apiMapThread.Start("https://synthriderz.com/api/beatmaps?limit=50&page=1&sort=published_at,DESC");
        }

        public void GetMaps(object url)
        {
            using (WebClient client = new WebClient())
            {
                string res = client.DownloadString(url.ToString());
                MapPage page = JsonConvert.DeserializeObject<MapPage>(res);
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    maps.Clear();
                    maps.Add(page.data);
                });
            }
        }
    }
}
