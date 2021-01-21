using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Collections.ObjectModel;
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
using System.Linq;
using System.Collections.Specialized;
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
        public ObservableCollection<PlaylistItem> playlists { get; private set; } = new ObservableCollection<PlaylistItem>();
        public ObservableCollection<StageItem> stages { get; private set; } = new ObservableCollection<StageItem>();
        public ObservableCollection<AvatarItem> avatars { get; private set; } = new ObservableCollection<AvatarItem>();

        public List<LocalItem> localItems { get; set; } = new List<LocalItem>();

        public bool closing = false;

        public int apiRequestCounter = 0;

        public MapHandler mapHandler = new MapHandler();
        public PlaylistHandler playlistHandler = new PlaylistHandler();
        public StageHandler stageHandler = new StageHandler();
        public AvatarHandler avatarHandler = new AvatarHandler();

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
                currentPage = 1;
                numberOfPages = 1;
                GetPage();
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
                GetPage();
            }));

            getAllCommand = ReactiveCommand.Create((() =>
            {
                GetAll();
            }));

            getPageCommand = ReactiveCommand.Create((() =>
            {
                GetPage(true);
            }));

            selectDirectoryCommand = ReactiveCommand.Create((() =>
            {
                selectDirectory();
            }));


            this.WhenAnyValue(x => x.currentPage).Subscribe(x => GetPage());
            this.WhenAnyValue(x => x.currentPage).Subscribe(x => GetPage());//reload maps when the current page changes
            this.WhenAnyValue(x => x.selectedSortMethod).Subscribe(x => GetPage());//reload maps when the sort method changes
            this.WhenAnyValue(x => x.selectedSortOrder).Subscribe(x => GetPage());//reload maps when the sort order changes
            settings.Changed.Subscribe(x => SaveSettings());//save the settings when they change
            this.WhenAny(x => x.synthDirectory, x => x != null && CheckDirectory(x.GetValue())).Subscribe(x =>
            {
                directoryValid = CheckDirectory(synthDirectory);
                if (directoryValid) settings.synthDirectory = synthDirectory;//save the current directory to the settings if it has changed and is valid
            });

            items.CollectionChanged += ItemsCollectionChanged;

            LoadLocalItems();

            if (!CheckDirectory(synthDirectory))
            {
                GetDirectoryFromRegistry();
            }
        }

        private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (selectedTabIndex)
            {
                case 0:
                    maps.Clear();
                    maps.Add(items.Where(x => x.itemType == ItemType.Map).Select(x => (MapItem)x));
                    break;
                case 1:
                    playlists.Clear();
                    playlists.Add(items.Where(x => x.itemType == ItemType.Playlist).Select(x => (PlaylistItem)x));
                    break;
                case 2:
                    stages.Clear();
                    stages.Add(items.Where(x => x.itemType == ItemType.Stage).Select(x => (StageItem)x));
                    break;
                case 3:
                    avatars.Clear();
                    avatars.Add(items.Where(x => x.itemType == ItemType.Avatar).Select(x => (AvatarItem)x));
                    break;
            }
        }

        public void LoadLocalItems()
        {

            mapHandler.LoadLocalItems();
            playlistHandler.LoadLocalItems();
            stageHandler.LoadLocalItems();
            avatarHandler.LoadLocalItems();

            foreach (GenericItem item in items)
            {
                item.UpdateDownloaded();
            }
        }

        public void GetPage(bool download = false)
        {
            if (lastSearchText != searchText)
            {
                currentPage = 1;
                lastSearchText = searchText;
            }
            apiRequestCounter++;

            switch (selectedTabIndex)
            {
                case 0:
                    mapHandler.GetPage(download);
                    break;
                case 1:
                    playlistHandler.GetPage(download);
                    break;
                case 2:
                    stageHandler.GetPage(download);
                    break;
                case 3:
                    avatarHandler.GetPage(download);
                    break;
            }
        }

        public void GetAll()
        {
            if (CheckDirectory(settings.synthDirectory, true))
            {
                searchText = lastSearchText = "";
                currentPage = 1;
                selectedSortMethodIndex = 0;
                selectedSortOrderIndex = 0;

                switch (selectedTabIndex)
                {
                    case 0:
                        mapHandler.GetAll();
                        break;
                    case 1:
                        playlistHandler.GetAll();
                        break;
                    case 2:
                        stageHandler.GetAll();
                        break;
                    case 3:
                        avatarHandler.GetAll();
                        break;
                }
            }
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
