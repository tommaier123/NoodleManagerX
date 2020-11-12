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
using System.Threading.Tasks;

namespace NoodleManagerX.Models
{
    class MainViewModel : ReactiveObject
    {
        //dotnet publish -c Release -f netcoreapp3.1 -r win-x64 --self-contained true /p:PublishSingleFile=true
        //dotnet publish -c Release -f netcoreapp3.1 -r linux-x64 --self-contained true /p:PublishSingleFile=true
        //dotnet publish -c Release -f netcoreapp3.1 -r osx-x64 --self-contained true /p:PublishSingleFile=true

        public const int pagecount = 6;
        public const int pagesize = 10;

        [Reactive] public int selectedTabIndex { get; set; } = 0;
        [Reactive] public int currentPage { get; set; } = 1;
        [Reactive] public int numberOfPages { get; set; } = 1;

        public ReactiveCommand<Unit, Unit> minimizeCommand { get; set; }
        public ReactiveCommand<Unit, Unit> toggleFullscreenCommand { get; set; }
        public ReactiveCommand<Unit, Unit> closeCommand { get; set; }
        public ReactiveCommand<string, Unit> tabSelectCommand { get; set; }
        public ReactiveCommand<string, Unit> pageUpCommand { get; set; }
        public ReactiveCommand<string, Unit> pageDownCommand { get; set; }

        public ObservableCollection<Map> maps { get; set; } = new ObservableCollection<Map>();

        private Task apiMapTask;
        private bool apiMapTaskCancel = false;

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
                    currentPage++;
                }
            }));

            pageDownCommand = ReactiveCommand.Create<string>((x =>
            {
                if (currentPage > 1)
                {
                    currentPage--;
                }
            }));

            this.WhenAnyValue(x => x.currentPage).Subscribe(x => GetMapPage());
        }

        public void GetMapPage()
        {
            if (apiMapTask != null && apiMapTask.Status.Equals(TaskStatus.Running))
            {
                apiMapTaskCancel = true;
                apiMapTask.Wait();
            }
            apiMapTask = Task.Factory.StartNew(MapPageThreadFunction);
        }

        public void MapPageThreadFunction()
        {
            long sum = 0;
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                maps.Clear();
            });

            for (int i = 0; i < pagecount; i++)
            {

                if (apiMapTaskCancel) { apiMapTaskCancel = false; break; }

                using (WebClient client = new WebClient())
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    string res = client.DownloadString("https://synthriderz.com/api/beatmaps?limit=" + pagesize + "&page=" + (currentPage * pagecount + i) + "&sort=published_at,DESC");
                    watch.Stop();
                    Console.WriteLine(watch.ElapsedMilliseconds);
                    sum += watch.ElapsedMilliseconds;

                    MapPage mapPage = JsonConvert.DeserializeObject<MapPage>(res);

                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        maps.Add(mapPage.data);
                        numberOfPages = (mapPage.pagecount) / pagecount;
                    });
                }
            }
            Console.WriteLine(sum);
        }
    }
}
