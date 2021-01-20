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
using static System.Environment;
using System.IO.Compression;
using System.Diagnostics;
using System.Linq;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Collections;

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
        [Reactive] public bool directoryValid { get; set; }

        [Reactive] public Settings settings { get; set; } = new Settings();

        public ReactiveCommand<Unit, Unit> minimizeCommand { get; set; }
        public ReactiveCommand<Unit, Unit> toggleFullscreenCommand { get; set; }
        public ReactiveCommand<Unit, Unit> closeCommand { get; set; }
        public ReactiveCommand<string, Unit> tabSelectCommand { get; set; }
        public ReactiveCommand<Unit, Unit> pageUpCommand { get; set; }
        public ReactiveCommand<Unit, Unit> pageDownCommand { get; set; }
        public ReactiveCommand<Unit, Unit> searchCommand { get; set; }
        public ReactiveCommand<Unit, Unit> getAllCommand { get; set; }
        public ReactiveCommand<Unit, Unit> getPageCommand { get; set; }
        public ReactiveCommand<Unit, Unit> selectDirectoryCommand { get; set; }


        public ObservableCollection<GenericItem> items { get; set; } = new ObservableCollection<GenericItem>();

        public ObservableCollection<MapItem> maps { get; private set; } = new ObservableCollection<MapItem>();
        public List<LocalItem> localItems { get; set; } = new List<LocalItem>();

        public const int pagecount = 6;
        public const int pagesize = 10;

        public bool closing = false;

        private int apiMapRequestCounter = 0;

        private const string mapSearchQuerry = "{\"$or\":[{\"title\":{\"$contL\":\"<value>\"}},{\"artist\":{\"$contL\":\"<value>\"}},{\"mapper\":{\"$contL\":\"<value>\"}}]}";

        public MainViewModel()
        {
            s_instance = this;

            MainWindow.s_instance.Closing += ClosingEvent;

            LoadSettings();

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

            pageUpCommand = ReactiveCommand.Create((() =>
            {
                if (currentPage < numberOfPages)
                {
                    currentPage++;
                }
            }));

            pageDownCommand = ReactiveCommand.Create((() =>
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

            getPageCommand = ReactiveCommand.Create((() =>
            {
                GetMapPage(true);
            }));

            selectDirectoryCommand = ReactiveCommand.Create((() =>
            {
                selectDirectory();
            }));


            this.WhenAnyValue(x => x.currentPage).Subscribe(x => GetMapPage());
            this.WhenAnyValue(x => x.currentPage).Subscribe(x => GetMapPage());//reload maps when the current page changes
            this.WhenAnyValue(x => x.selectedSortMethod).Subscribe(x => GetMapPage());//reload maps when the sort method changes
            this.WhenAnyValue(x => x.selectedSortOrder).Subscribe(x => GetMapPage());//reload maps when the sort order changes
            settings.Changed.Subscribe(x => SaveSettings());//save the settings when they change
            this.WhenAny(x => x.synthDirectory, x => x != null && CheckDirectory(x.GetValue())).Subscribe(x =>
            {
                directoryValid = CheckDirectory(synthDirectory);
                if (directoryValid) settings.synthDirectory = synthDirectory;//save the current directory to the settings if it has changed and is valid
            });

            items.CollectionChanged += ItemsCollectionChanged;

            LoadLocalMaps();

            if (!CheckDirectory(synthDirectory))
            {
                GetDirectoryFromRegistry();
            }
        }

        private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            maps.Clear();
            maps.Add(items.Where(x => x.itemType == ItemType.Map).Select(x => (MapItem)x));
            Console.WriteLine("items changed");
        }

        public Task LoadLocalMaps()
        {
            string directory = Path.Combine(settings.synthDirectory, "CustomSongs");
            if (Directory.Exists(directory))
            {
                return Task.Run(async () =>
                {
                    List<LocalItem> tmp = new List<LocalItem>();
                    foreach (string file in Directory.GetFiles(directory))
                    {
                        if (Path.GetExtension(file) == ".synth")
                        {
                            try
                            {
                                using (ZipArchive archive = ZipFile.OpenRead(file))
                                {
                                    foreach (ZipArchiveEntry entry in archive.Entries)
                                    {
                                        if (entry.FullName == "synthriderz.meta.json")
                                        {
                                            using (StreamReader sr = new StreamReader(entry.Open()))
                                            {
                                                LocalItem localMap = JsonConvert.DeserializeObject<LocalItem>(await sr.ReadToEndAsync());
                                                localMap.itemType = ItemType.Map;
                                                tmp.Add(localMap);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Log(MethodBase.GetCurrentMethod(), e);
                                Console.WriteLine("Deleting corrupted file " + Path.GetFileName(file));
                                File.Delete(file);
                            }
                        }
                    }
                    localItems.Add(tmp);

                    foreach (GenericItem item in items)
                    {
                        item.UpdateDownloaded();
                    }
                });
            }
            return Task.CompletedTask;
        }

        public void GetMapPage(bool download = false)
        {
            if (lastSearchText != searchText)
            {
                currentPage = 1;
                lastSearchText = searchText;
            }
            var tmp = items.Where(x => x.itemType != ItemType.Map);
            items.Clear();
            items.Add(tmp);
            apiMapRequestCounter++;
            Task.Run(() => MapPageTaskFunction(apiMapRequestCounter, download));
        }

        public void GetAll()
        {
            if (CheckDirectory(settings.synthDirectory, true))
            {
                searchText = lastSearchText = "";
                currentPage = 1;
                selectedSortMethodIndex = 0;
                selectedSortOrderIndex = 0;
                Task.Run(() => GetAllTaskFunction());
            }
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
                        client.Encoding = Encoding.UTF8;
                        string sortMethod = "published_at";
                        string sortOrder = "DESC";
                        string search = "";
                        if (selectedSortMethod?.Name != null) sortMethod = selectedSortMethod.Name;
                        if (selectedSortOrder?.Name != null) sortOrder = selectedSortOrder.Name;
                        if (searchText != "") search = "&s=" + mapSearchQuerry.Replace("<value>", searchText);

                        string req = "https://synthriderz.com/api/beatmaps?limit=" + pagesize + "&page=" + ((currentPage - 1) * pagecount + i) + search + "&sort=" + sortMethod + "," + sortOrder;
                        MapPage mapPage = JsonConvert.DeserializeObject<MapPage>(await client.DownloadStringTaskAsync(req));

                        if (apiMapRequestCounter != requestID && !download) break;

                        if (i == 1)
                        {
                            //dont wait by discarding result with _ variable
                            _ = Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                numberOfPages = (int)Math.Ceiling((double)mapPage.pagecount / pagecount);
                            });
                        }

                        _ = Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            items.Add(mapPage.data);
                        });

                        if (download)
                        {
                            foreach (MapItem map in mapPage.data)
                            {
                                if (!map.downloaded)
                                {
                                    DownloadScheduler.queue.Add(map);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e) { Log(MethodBase.GetCurrentMethod(), e); }
        }

        public GenericItem Unduplicate(GenericItem item, ObservableCollection<GenericItem> items, ItemType type)
        {
            var tmp = DownloadScheduler.queue.Where(x => x.id == item.id && x.itemType == type && x.itemType == item.itemType).ToList();
            if (tmp.Count() > 0 && tmp[0] != null)
            {
                return tmp[0];
            }
            tmp = items.Where(x => x.id == item.id && x.itemType == type && x.itemType == item.itemType).ToList();
            if (tmp.Count() > 0 && tmp[0] != null)
            {
                return tmp[0];
            }
            return item;
        }

        public async void GetAllTaskFunction()
        {
            try
            {
                Console.WriteLine("Get All Started");

                int pageCountAll = 1;
                int i = 1;

                do
                {
                    using (WebClient client = new WebClient())
                    {
                        string req = "https://synthriderz.com/api/beatmaps?limit=" + pagesize + "&page=" + i;
                        MapPage mapPage = JsonConvert.DeserializeObject<MapPage>(await client.DownloadStringTaskAsync(req));

                        if (closing) break;
                        foreach (MapItem map in mapPage.data)
                        {
                            var instances = items.Where(x => x.itemType == ItemType.Map && x.id == map.id).ToList();
                            if (instances.Count > 0)
                            {
                                if (!instances[0].downloaded)
                                {
                                    DownloadScheduler.queue.Add(instances[0]);
                                }
                            }
                            else
                            {
                                if (!map.downloaded)
                                {
                                    DownloadScheduler.queue.Add(map);
                                }
                            }
                        }
                        pageCountAll = mapPage.pagecount;
                        i++;
                    }
                }
                while (i <= pageCountAll);

            }
            catch (Exception e) { Log(MethodBase.GetCurrentMethod(), e); }
            Console.WriteLine("Get All Done");
        }

        public void OpenErrorDialog(string text)
        {
            _ = Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await MessageBox.Show(MainWindow.s_instance, text, "Error", MessageBox.MessageBoxButtons.Ok);
            });
        }

        public bool CheckDirectory(string path, bool dialog = false)
        {
            bool ret = false;
            if (!String.IsNullOrEmpty(path))
            {
                if (Directory.Exists(path) && File.Exists(Path.Combine(path, "SynthRiders.exe"))) ret = true;
            }

            if (dialog && !ret)
            {
                OpenErrorDialog("Invalid Synth Riders Directory");
            }
            return ret;
        }

        public async void selectDirectory()
        {
            OpenFolderDialog dialog = new OpenFolderDialog();
            dialog.Directory = synthDirectory;

            string directory = await dialog.ShowAsync(MainWindow.s_instance);

            if (!String.IsNullOrEmpty(directory))
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
            catch (Exception e) { Log(MethodBase.GetCurrentMethod(), e); }
        }

        public void LoadSettings()
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(SpecialFolder.ApplicationData), "NoodleManager", "Settings.json");
                Console.WriteLine("Loading Settings from " + path);
                if (File.Exists(path))
                {
                    using (StreamReader file = File.OpenText(path))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        settings = (Settings)serializer.Deserialize(file, typeof(Settings));
                    }
                    synthDirectory = settings.synthDirectory;
                }
            }
            catch (Exception e) { Log(MethodBase.GetCurrentMethod(), e); }
        }

        public void SaveSettings()
        {
            Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine("Saving Settings");
                    string output = JsonConvert.SerializeObject(settings);

                    string directory = Path.Combine(Environment.GetFolderPath(SpecialFolder.ApplicationData), "NoodleManager");
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (StreamWriter sw = new StreamWriter(Path.Combine(directory, "Settings.json")))
                    {
                        await sw.WriteAsync(output);
                    }
                }
                catch (Exception e) { Log(MethodBase.GetCurrentMethod(), e); }
            });
        }

        private void ClosingEvent(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DownloadScheduler.downloading.Count > 0)
            {
                if (!closing)
                {
                    e.Cancel = true;
                    ShowClosingDialog(DownloadScheduler.downloading.Count);
                }
            }
        }

        private async void ShowClosingDialog(int num)
        {
            MessageBox.MessageBoxResult res = await MessageBox.Show(MainWindow.s_instance, "There are still " + num + " running downloads." + Environment.NewLine + "Abort?", "Warning", MessageBox.MessageBoxButtons.OkCancel);

            if (res == MessageBox.MessageBoxResult.Ok)
            {
                DownloadScheduler.queue.Clear();
                closing = true;

                _ = Task.Run(async () =>
                  {
                      _ = Dispatcher.UIThread.InvokeAsync(() =>
                      {
                          MainWindow.s_instance.Hide();
                      });

                      while (DownloadScheduler.downloading.Count != 0)
                      {
                          await Task.Delay(500);
                      }
                      _ = Dispatcher.UIThread.InvokeAsync(() =>
                          {
                              MainWindow.s_instance.Close();
                          });
                  });
            }
        }

        public static void Log(MethodBase m, Exception e)
        {
            Console.WriteLine("Error " + m.ReflectedType.Name + " " + e.Message);
        }

        public class CustomerSorter : IComparer
        {
            public int Compare(object x, object y)
            {
                return ((int)x) - ((int)y);
            }
        }
    }
}
