using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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

        public const int pagecount = 10;
        public const int pagesize = 5;

        [Reactive] public int selectedTabIndex { get; set; } = 0;
        [Reactive] public int currentPage { get; set; } = 1;
        [Reactive] public int numberOfPages { get; set; }

        public ReactiveCommand<Unit, Unit> minimizeCommand { get; set; }
        public ReactiveCommand<Unit, Unit> toggleFullscreenCommand { get; set; }
        public ReactiveCommand<Unit, Unit> closeCommand { get; set; }
        public ReactiveCommand<string, Unit> tabSelectCommand { get; set; }
        public ReactiveCommand<string, Unit> pageUpCommand { get; set; }
        public ReactiveCommand<string, Unit> pageDownCommand { get; set; }

        public ObservableCollection<Map> maps { get; set; } = new ObservableCollection<Map>();

        private Thread apiMapThread;
        private bool apiMapThreadCancel = false;

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

            pageUpCommand = ReactiveCommand.Create<string>((x =>
            {
                if (currentPage < numberOfPages)
                {
                    GetMapPage(currentPage + 1);
                }
            }));

            pageDownCommand = ReactiveCommand.Create<string>((x =>
            {
                if (currentPage > 1)
                {
                    GetMapPage(currentPage - 1);
                }
            }));

            apiMapThread = new Thread(MapPageThreadFunction);
            GetMapPage(1);
        }

        public void GetMapPage(int page)
        {
            if (apiMapThread.IsAlive)
            {
                apiMapThreadCancel = true;
                //apiMapThread.Join();
            }
            apiMapThread = new Thread(MapPageThreadFunction);//todo apparently you should avoid using threads and use tasks instead
            apiMapThread.Start(page);
        }

        public void MapPageThreadFunction(object data)
        {
            int page = (int)data;
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                maps.Clear();
                currentPage = page;
            });

            for (int i = 0; i < pagecount; i++)
            {
                if (apiMapThreadCancel)
                {
                    apiMapThreadCancel = false;
                    break;
                }

                using (WebClient client = new WebClient())
                {
                    string res = client.DownloadString("https://synthriderz.com/api/beatmaps?limit=" + pagesize + "&page=" + (page * pagecount + i) + "&sort=published_at,DESC");
                    MapPage mapPage = JsonConvert.DeserializeObject<MapPage>(res);
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        maps.Add(mapPage.data);
                        numberOfPages = (mapPage.pagecount + pagesize - 1) / pagecount;
                    });
                }
            }
        }
    }
}
