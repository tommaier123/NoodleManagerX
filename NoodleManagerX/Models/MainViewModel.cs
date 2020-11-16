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

        private const string mapSearchQuerry = "{\"$or\":[{\"title\":{\"$contL\":\"<value>\"}},{\"artist\":{\"$contL\":\"<value>\"}},{\"mapper\":{\"$contL\":\"<value>\"}}]}";

        [Reactive] public int selectedTabIndex { get; set; } = 0;
        [Reactive] public int currentPage { get; set; } = 1;
        [Reactive] public int numberOfPages { get; set; } = 1;
        [Reactive] public string searchText { get; set; } = "";
        public string lastSearchText = "";
        [Reactive] public ComboBoxItem selectedSortMethod { get; set; }
        [Reactive] public ComboBoxItem selectedSortOrder { get; set; }

        public static string synthDirectory { get; set; } = @"C:\Program Files (x86)\Steam\steamapps\common\SynthRiders";

        public ReactiveCommand<Unit, Unit> minimizeCommand { get; set; }
        public ReactiveCommand<Unit, Unit> toggleFullscreenCommand { get; set; }
        public ReactiveCommand<Unit, Unit> closeCommand { get; set; }
        public ReactiveCommand<string, Unit> tabSelectCommand { get; set; }
        public ReactiveCommand<string, Unit> pageUpCommand { get; set; }
        public ReactiveCommand<string, Unit> pageDownCommand { get; set; }
        public ReactiveCommand<Unit, Unit> searchCommand { get; set; }

        public ObservableCollection<Map> maps { get; set; } = new ObservableCollection<Map>();

        private int apiMapRequestCounter = 0;

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

            searchCommand = ReactiveCommand.Create((() =>
            {
                GetMapPage();
            }));

            this.WhenAnyValue(x => x.currentPage).Subscribe(x => GetMapPage());
            this.WhenAnyValue(x => x.selectedSortMethod).Subscribe(x => GetMapPage());
            this.WhenAnyValue(x => x.selectedSortOrder).Subscribe(x => GetMapPage());
            //this.WhenAnyValue(x => x.searchText).Subscribe(x => GetMapPage());
        }

        public void GetMapPage()
        {
            if (lastSearchText != searchText)
            {
                currentPage = 1;
                lastSearchText = searchText;
            }
            maps.Clear();
            apiMapRequestCounter++;
            Task.Factory.StartNew(() => MapPageThreadFunction(apiMapRequestCounter));
        }

        public async void MapPageThreadFunction(int requestID)
        {
            try
            {
                for (int i = 1; i <= pagecount; i++)
                {
                    using (WebClient client = new WebClient())
                    {
                        string sortMethod = "published_at";
                        string sortOrder = "DESC";
                        if (selectedSortMethod?.Name != null) sortMethod = selectedSortMethod.Name;
                        if (selectedSortOrder?.Name != null) sortOrder = selectedSortOrder.Name;
                        string search = "";
                        if (searchText != "") search = "&s=" + mapSearchQuerry.Replace("<value>", searchText);

                        string req = "https://synthriderz.com/api/beatmaps?limit=" + pagesize + "&page=" + ((currentPage - 1) * pagecount + i) + search + "&sort=" + sortMethod + "," + sortOrder;
                        Console.WriteLine(req);
                        string res = await client.DownloadStringTaskAsync(req);
                        MapPage mapPage = JsonConvert.DeserializeObject<MapPage>(res);

                        if (apiMapRequestCounter != requestID) break;

                        if (i == 1)
                        {
                            //dont wait by discarding result with _ variable
                            _ = Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                maps.Add(mapPage.data);
                                numberOfPages = (int)Math.Ceiling((double)mapPage.pagecount / pagecount);
                            });
                        }
                        else //spend less time on the ui thread, not sure this is necessary
                        {
                            _ = Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                maps.Add(mapPage.data);
                            });
                        }
                    }
                }
            }
            catch { }
        }
    }
}
