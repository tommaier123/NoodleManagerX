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
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using MsgBox;

namespace NoodleManagerX.Models
{
    class MainViewModel : ReactiveObject
    {
        //dotnet publish -c Release -f netcoreapp3.1 -r win-x64 --self-contained true /p:PublishSingleFile=true
        //dotnet publish -c Release -f netcoreapp3.1 -r linux-x64 --self-contained true /p:PublishSingleFile=true
        //dotnet publish -c Release -f netcoreapp3.1 -r osx-x64 --self-contained true /p:PublishSingleFile=true

        public static MainViewModel s_instance;

        [Reactive] public int selectedTabIndex { get; set; } = 0;
        [Reactive] public int currentPage { get; set; } = 1;
        [Reactive] public int numberOfPages { get; set; } = 1;
        [Reactive] public string searchText { get; set; } = "";
        public string lastSearchText = "";
        [Reactive] public ComboBoxItem selectedSortMethod { get; set; }
        [Reactive] public ComboBoxItem selectedSortOrder { get; set; }
        [Reactive] public int selectedSortMethodIndex { get; set; } = 0;
        [Reactive] public int selectedSortOrderIndex { get; set; } = 0;

        [Reactive] public string synthDirectory { get; set; }
        public extern bool directoryValid { [ObservableAsProperty] get; }

        public ReactiveCommand<Unit, Unit> minimizeCommand { get; set; }
        public ReactiveCommand<Unit, Unit> toggleFullscreenCommand { get; set; }
        public ReactiveCommand<Unit, Unit> closeCommand { get; set; }
        public ReactiveCommand<string, Unit> tabSelectCommand { get; set; }
        public ReactiveCommand<string, Unit> pageUpCommand { get; set; }
        public ReactiveCommand<string, Unit> pageDownCommand { get; set; }
        public ReactiveCommand<Unit, Unit> searchCommand { get; set; }
        public ReactiveCommand<Unit, Unit> getAllCommand { get; set; }
        public ReactiveCommand<Unit, Unit> selectDirectoryCommand { get; set; }

        public ObservableCollection<Map> maps { get; set; } = new ObservableCollection<Map>();

        public const int pagecount = 6;
        public const int pagesize = 10;


        private int apiMapRequestCounter = 0;

        private const string mapSearchQuerry = "{\"$or\":[{\"title\":{\"$contL\":\"<value>\"}},{\"artist\":{\"$contL\":\"<value>\"}},{\"mapper\":{\"$contL\":\"<value>\"}}]}";

        public MainViewModel()
        {
            s_instance = this;
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

            getAllCommand = ReactiveCommand.Create((() =>
            {
                GetAll();
            }));

            selectDirectoryCommand = ReactiveCommand.Create((() =>
            {
                selectDirectory();
            }));

            this.WhenAnyValue(x => x.currentPage).Subscribe(x => GetMapPage());
            this.WhenAnyValue(x => x.selectedSortMethod).Subscribe(x => GetMapPage());
            this.WhenAnyValue(x => x.selectedSortOrder).Subscribe(x => GetMapPage());
            this.WhenAny(x => x.synthDirectory, x => x != null && CheckDirectory(x.GetValue())).ToPropertyEx(this, x => x.directoryValid);

            if (!directoryValid)
            {
                GetDirectoryFromRegistry();
            }
        }

        public bool CheckDirectory(string path, bool dialog = false)
        {
            bool ret = false;
            if (!String.IsNullOrEmpty(path))
            {
                if (Directory.Exists(path)) ret = true;
                else if (File.Exists(Path.Combine(path, "SynthRiders.exe"))) ret = true;
            }

            if (dialog && !ret)
            {
                OpenErrorDialog("Invalid Synth Riders Directory");
            }
            return ret;
        }

        public void OpenErrorDialog(string text)
        {
            _ = Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await MessageBox.Show(MainWindow.s_instance, text, "Error", MessageBox.MessageBoxButtons.Ok);
            });

        }

        public async void selectDirectory()
        {
            OpenFolderDialog dialog = new OpenFolderDialog();
            dialog.DefaultDirectory = synthDirectory;

            string directory = await dialog.ShowAsync(MainWindow.s_instance);

            if (directory != "")
            {
                if (CheckDirectory(directory))
                {
                    synthDirectory = directory;
                }
                else
                {
                    string parent = Directory.GetParent(directory).FullName;
                    if (CheckDirectory(parent))
                    {
                        synthDirectory = parent;
                    }
                }
            }
        }

        public void GetDirectoryFromRegistry()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    byte[] regBytes = (byte[])Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Kluge Interactive\SynthRiders", "com.synthriders.installpath_h4259148619", "");
                    if (regBytes != null)
                    {
                        string directory = Encoding.Default.GetString(regBytes);
                        directory = string.Concat(directory.Split(Path.GetInvalidPathChars()));
                        if (!String.IsNullOrEmpty(directory) && CheckDirectory(directory))
                        {
                            synthDirectory = directory;
                        }
                    }
                }
            }
            catch { }
        }

        public void GetMapPage(bool download = false)
        {
            if (lastSearchText != searchText)
            {
                currentPage = 1;
                lastSearchText = searchText;
            }
            maps.Clear();
            apiMapRequestCounter++;
            Task.Factory.StartNew(() => MapPageTaskFunction(apiMapRequestCounter, download));
        }

        public void GetAll()
        {
            if (CheckDirectory(synthDirectory, true))
            {
                searchText = lastSearchText = "";
                currentPage = 1;
                selectedSortMethodIndex = 0;
                selectedSortOrderIndex = 0;
                Task.Run(() => GetAllTaskFunction());
            }
        }

        public void DownloadMap(Map map)
        {
            Console.WriteLine("Downloading " + map.title);
        }

        public async void MapPageTaskFunction(int requestID, bool download = false)
        {
            try
            {
                Console.WriteLine("Getting Page");

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
                if (download)
                {
                    foreach (Map map in maps)
                    {
                        DownloadMap(map);
                    }
                }
            }
            catch { }
        }

        public async void GetAllTaskFunction()
        {
            try
            {
                Console.WriteLine("Downloading All");
                int pageCountAll = 1;
                int i = 1;
                do
                {
                    using (WebClient client = new WebClient())
                    {
                        string req = "https://synthriderz.com/api/beatmaps?limit=" + pagesize + "&page=" + i;
                        string res = await client.DownloadStringTaskAsync(req);
                        MapPage mapPage = JsonConvert.DeserializeObject<MapPage>(res);

                        foreach (Map map in mapPage.data)
                        {
                            DownloadMap(map);
                        }

                        pageCountAll = mapPage.pagecount;
                        i++;
                    }
                }
                while (i <= pageCountAll);
            }
            catch { }
        }
    }
}
