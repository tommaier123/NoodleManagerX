using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using DynamicData;
using Microsoft.Win32;
using MsgBox;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SharpAdbClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Environment;
using Path = System.IO.Path;
using Stream = System.IO.Stream;
using MemoryStream = System.IO.MemoryStream;


namespace NoodleManagerX.Models
{
    class MainViewModel : ReactiveObject
    {
        //dotnet publish -c Release -f net5.0 -r win-x64 --self-contained true /p:PublishSingleFile=true -p:PublishTrimmed=True -p:TrimMode=Link -p:PublishReadyToRun=false
        //dotnet publish -c Release -f net5.0 -r linux-x64 --self-contained true /p:PublishSingleFile=true -p:PublishTrimmed=True -p:TrimMode=Link -p:PublishReadyToRun=false
        //dotnet publish -c Release -f net5.0 -r osx-x64 --self-contained true /p:PublishSingleFile=true  -p:PublishTrimmed=True -p:TrimMode=Link -p:PublishReadyToRun=false


        //Todo:
        //check which dispatchers should be awaited
        //better task structure in general
        //possibly remove unnecessary flags from function parameters, use state machine/individual parameters
        //get description when rightclicking an item and display in context menu
        //multiple files
        //update reminder
        //don't leave thread safety up to luck
        //get a updated at timestamp for updating, published at is fine for filesystem timestamp
        //look at exception handling with tasks



        [Reactive] private string version { get; set; } = "V0.6.0";

        public static MainViewModel s_instance;

        [Reactive] public Settings settings { get; set; } = new Settings();

        [Reactive] public int selectedTabIndex { get; set; } = 0;
        [Reactive] public int currentPage { get; set; } = 1;
        [Reactive] public int numberOfPages { get; set; } = 1;
        [Reactive] public string searchText { get; set; } = "";
        public string lastSearchText = "";

        [Reactive] public ComboBoxItem selectedSearchParameter { get; set; }
        [Reactive] public ComboBoxItem selectedDifficulty { get; set; }
        [Reactive] public ComboBoxItem selectedSortMethod { get; set; }
        [Reactive] public ComboBoxItem selectedSortOrder { get; set; }
        [Reactive] public int selectedSearchParameterIndex { get; set; } = 0;
        [Reactive] public int selectedDifficultyIndex { get; set; } = 0;
        [Reactive] public int selectedSortMethodIndex { get; set; } = 0;
        [Reactive] public int selectedSortOrderIndex { get; set; } = 0;
        [Reactive] private string synthDirectory { get; set; }
        [Reactive] public bool directoryValid { get; set; }
        [Reactive] public int progress { get; set; } = 0;
        [Reactive] public bool questConnected { get; set; } = false;

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
        public ReactiveCommand<Unit, Unit> connectQuestCommand { get; set; }

        public ObservableCollection<GenericItem> items { get; set; } = new ObservableCollection<GenericItem>();
        public ObservableCollection<string> blacklist { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<MapItem> maps { get; private set; } = new ObservableCollection<MapItem>();
        public ObservableCollection<PlaylistItem> playlists { get; private set; } = new ObservableCollection<PlaylistItem>();
        public ObservableCollection<StageItem> stages { get; private set; } = new ObservableCollection<StageItem>();
        public ObservableCollection<AvatarItem> avatars { get; private set; } = new ObservableCollection<AvatarItem>();
        public List<LocalItem> localItems { get; set; } = new List<LocalItem>();

        public AdbServer adbServer = new AdbServer();
        public AdbClient adbClient = new AdbClient();

        public bool closing = false;
        public bool downloadPage = false;//so that loading maps get downloaded when using get page
        public bool updatingLocalItems = false;
        public bool getAllRunning = false;
        public bool pruning = false;//maps without metadata should be deleted
        public bool savingDB = false;
        public bool savingBlacklist = false;
        public bool savingSettings = false;

        public int apiRequestCounter = 0;

        public MapHandler mapHandler = new MapHandler();
        public PlaylistHandler playlistHandler = new PlaylistHandler();
        public StageHandler stageHandler = new StageHandler();
        public AvatarHandler avatarHandler = new AvatarHandler();

        public MainViewModel()
        {
            s_instance = this;

            Task.Run(async () =>
            {
                MainWindow.s_instance.Closing += ClosingEvent;

                LoadSettings();

                settings.Changed.Subscribe(x => { SaveSettings(); });//save the settings when they change

                this.WhenAnyValue(x => x.synthDirectory).Skip(1).Subscribe(x =>
                {
                    directoryValid = CheckDirectory(synthDirectory);
                    if (settings.synthDirectory != synthDirectory && directoryValid)
                    {
                        settings.synthDirectory = synthDirectory;
                        ReloadLocalSources(true);
                    }
                });

                _= Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!CheckDirectory(settings.synthDirectory))
                    {
                        settings.synthDirectory = "";
                        GetDirectoryFromRegistry();
                    }
                    else
                    {
                        synthDirectory = settings.synthDirectory;
                    }
                });

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
                    searchText = lastSearchText = "";
                    selectedSearchParameterIndex = 0;
                    selectedDifficultyIndex = 0;
                    selectedSortMethodIndex = 0;
                    selectedSortOrderIndex = 0;
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
                    if (!updatingLocalItems)
                    {
                        downloadPage = true;
                        foreach (GenericItem item in items.Where(x => !x.downloaded && !x.downloading))
                        {
                            DownloadScheduler.Download(item);
                        }
                    }
                }));

                selectDirectoryCommand = ReactiveCommand.Create((() =>
                {
                    selectDirectory();
                }));

                connectQuestCommand = ReactiveCommand.Create((() =>
                {
                    Task.Run(async () =>
                    {
                        while (savingBlacklist || savingDB)
                        {
                            await Task.Delay(100);
                        }


                        if (DownloadScheduler.downloading.Count == 0 && DownloadScheduler.queue.Count == 0)
                        {
                            if (MtpDevice.connected)
                            {
                                MtpDevice.Disconnect();
                            }
                            else
                            {
                                MtpDevice.Connect(true);
                            }
                        }
                        else
                        {
                            OpenErrorDialog("Can't disconnect while downloads are running");
                        }
                    });
                }));

                //the correct number of events needs to be skipped in ordere to avoid duplication
                //1 for properties that get initialized
                //2 for properties that get declared and are initialized via bindings

                this.WhenAnyValue(x => x.currentPage).Skip(1).Subscribe(x => GetPage());//reload maps when the current page changes
                this.WhenAnyValue(x => x.selectedSearchParameter).Skip(2).Subscribe(x => GetPage());//reload maps when the search parameter changes
                this.WhenAnyValue(x => x.selectedDifficulty).Skip(2).Subscribe(x => GetPage());//reload maps when the search difficulty changes
                this.WhenAnyValue(x => x.selectedSortMethod).Skip(2).Subscribe(x => GetPage());//reload maps when the sort method changes
                this.WhenAnyValue(x => x.selectedSortOrder).Skip(2).Subscribe(x => GetPage());//reload maps when the sort order changes

                items.CollectionChanged += ItemsCollectionChanged;
                blacklist.CollectionChanged += BlacklistCollectionChanged;

                MtpDevice.Connect();
                ReloadLocalSources();
                GetPage();
            });
        }

        private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateCollections();
        }

        private void BlacklistCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SaveBlacklist();
        }

        public void ReloadLocalSources(bool directoryChanged = false)
        {
            if (!(directoryChanged && MtpDevice.connected))//don't reload when directory changes and quest is connected
            {
                LoadLocalItems();
                LoadBlacklist();
            }
        }

        public void UpdateCollections()
        {
            _ = Dispatcher.UIThread.InvokeAsync(() =>
            {
                switch (selectedTabIndex)
                {
                    case 0:
                        maps.Clear();
                        maps.AddRange(items.Where(x => x.itemType == ItemType.Map).Select(x => (MapItem)x));
                        break;
                    case 1:
                        playlists.Clear();
                        playlists.AddRange(items.Where(x => x.itemType == ItemType.Playlist).Select(x => (PlaylistItem)x));
                        break;
                    case 2:
                        stages.Clear();
                        stages.AddRange(items.Where(x => x.itemType == ItemType.Stage).Select(x => (StageItem)x));
                        break;
                    case 3:
                        avatars.Clear();
                        avatars.AddRange(items.Where(x => x.itemType == ItemType.Avatar).Select(x => (AvatarItem)x));
                        break;
                }
            });
        }

        public void GetPage()
        {
            if (lastSearchText != searchText)
            {
                currentPage = 1;
                lastSearchText = searchText;
            }
            apiRequestCounter++;//invalidate all running requests
            downloadPage = false;

            switch (selectedTabIndex)
            {
                case 0:
                    mapHandler.GetPage();
                    break;
                case 1:
                    playlistHandler.GetPage();
                    break;
                case 2:
                    stageHandler.GetPage();
                    break;
                case 3:
                    avatarHandler.GetPage();
                    break;
            }
        }

        public void GetAll()
        {
            if (StorageAbstraction.CanDownload() && !updatingLocalItems)
            {
                progress = 1;
                DownloadScheduler.toDownload = DownloadScheduler.queue.Count;

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

        public bool CheckDirectory(string path, bool showDialog = false)
        {
            bool ret = false;

            if (!String.IsNullOrEmpty(path))
            {
                if (System.IO.Directory.Exists(path) && System.IO.File.Exists(Path.Combine(path, "SynthRiders.exe"))) ret = true;
            }

            if (showDialog && !ret)
            {
                OpenErrorDialog("Invalid Synth Riders Directory");
            }
            return ret;
        }

        public async void selectDirectory()
        {
            try
            {
                OpenFolderDialog dialog = new OpenFolderDialog();
                dialog.Directory = synthDirectory;

                string directory = await dialog.ShowAsync(MainWindow.s_instance);

                if (!String.IsNullOrEmpty(directory))
                {
                    string parent = System.IO.Directory.GetParent(directory).FullName;
                    if (CheckDirectory(parent))
                    {
                        synthDirectory = parent;
                    }
                    else
                    {
                        synthDirectory = directory;
                    }
                }
            }
            catch (Exception e) { Log(MethodBase.GetCurrentMethod(), e); }
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
                            synthDirectory = directory.Trim('\\');
                        }
                    }
                }
            }
            catch (Exception e) { Log(MethodBase.GetCurrentMethod(), e); }
        }

        public void LoadLocalItems()
        {
            if (updatingLocalItems)
            {
                return;//prevent issues with multithreading
            }

            Task.Run(async () =>
            {
                _ = Dispatcher.UIThread.InvokeAsync(() =>
                {
                    progress = 1;

                });

                updatingLocalItems = true;
                localItems.Clear();
                bool dbExists = false;

                try
                {
                    string path = "NmDatabase.json";
                    MainViewModel.Log("Loading Database");
                    if (await StorageAbstraction.FileExists(path))
                    {
                        List<LocalItem> tmp = new List<LocalItem>();
                        using (Stream stream = await StorageAbstraction.ReadFile(path))
                        using (System.IO.StreamReader file = new System.IO.StreamReader(stream))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            tmp = (List<LocalItem>)serializer.Deserialize(file, typeof(List<LocalItem>));
                        }
                        localItems.AddRange(tmp);
                        dbExists = true;
                    }
                }
                catch (Exception e) { Log(MethodBase.GetCurrentMethod(), e); }

                if (!dbExists)
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        var res = await MessageBox.Show(null, "Click OK to delete maps without metadata e.g. if they were not downloaded from synthriderz.com" + Environment.NewLine + "This will only happen once when loading a new game directory" + Environment.NewLine + "Choosing to cancel might lead to duplicate maps" + Environment.NewLine + "Building the database for the first time can take some minutes", "Warning", MessageBox.MessageBoxButtons.OkCancel);
                        if (res == MessageBox.MessageBoxResult.Ok)
                        {
                            pruning = true;
                        }
                    });
                }

                Log("Loading local items");

                await mapHandler.LoadLocalItems();
                await playlistHandler.LoadLocalItems();
                await stageHandler.LoadLocalItems();
                await avatarHandler.LoadLocalItems();

                Log("Local items loaded");

                foreach (GenericItem item in items)
                {
                    _ = item.UpdateDownloaded();
                }

                updatingLocalItems = false;
                pruning = false;

                await SaveLocalItems();

                _ = Dispatcher.UIThread.InvokeAsync(() =>
                 {
                     progress = 0;

                 });
            });
        }

        public Task SaveLocalItems()
        {
            return Task.Run(async () =>
            {
                try
                {
                    if (StorageAbstraction.CanDownload(true))
                    {
                        savingDB = true;
                        Log("Saving Database");
                        string output = JsonConvert.SerializeObject(localItems);

                        MemoryStream stream = new MemoryStream();
                        System.IO.StreamWriter sw = new System.IO.StreamWriter(stream);
                        await sw.WriteAsync(output);
                        sw.Flush();
                        await StorageAbstraction.WriteFile(stream, "NmDatabase.json");
                    }
                }
                catch (Exception e) { Log(MethodBase.GetCurrentMethod(), e); }
                finally { savingDB = false; }
            });
        }

        public void LoadSettings()
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(SpecialFolder.ApplicationData), "NoodleManagerX", "Settings.json");
                MainViewModel.Log("Loading Settings from " + path);

                if (System.IO.File.Exists(path))
                {
                    using (System.IO.StreamReader file = System.IO.File.OpenText(path))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        settings = (Settings)serializer.Deserialize(file, typeof(Settings));
                    }
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
                    savingSettings = true;
                    MainViewModel.Log("Saving Settings");
                    string output = JsonConvert.SerializeObject(settings);

                    string directory = Path.Combine(Environment.GetFolderPath(SpecialFolder.ApplicationData), "NoodleManagerX");
                    if (!System.IO.Directory.Exists(directory))
                    {
                        System.IO.Directory.CreateDirectory(directory);
                    }

                    using (System.IO.StreamWriter sw = new System.IO.StreamWriter(Path.Combine(directory, "Settings.json")))
                    {
                        await sw.WriteAsync(output);
                    }
                }
                catch (Exception e) { Log(MethodBase.GetCurrentMethod(), e); }
                finally { savingSettings = false; }
            });
        }

        public void LoadBlacklist()
        {
            Task.Run(async () =>
            {
                try
                {
                    blacklist.Clear();
                    string path = "NmBlacklist.json";
                    MainViewModel.Log("Loading Blacklist");
                    if (await StorageAbstraction.FileExists(path))
                    {
                        using (Stream stream = await StorageAbstraction.ReadFile(path))
                        using (System.IO.StreamReader file = new System.IO.StreamReader(stream))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            blacklist.AddRange((List<string>)serializer.Deserialize(file, typeof(List<string>)));
                        }
                    }
                    foreach (GenericItem item in items)
                    {
                        item.UpdateBlacklisted();
                    }
                }
                catch (Exception e) { Log(MethodBase.GetCurrentMethod(), e); }
            });
        }

        public void SaveBlacklist()
        {
            Task.Run(async () =>
            {
                try
                {
                    if (StorageAbstraction.CanDownload(true))
                    {
                        savingBlacklist = true;
                        Log("Saving Blacklist");
                        string output = JsonConvert.SerializeObject(blacklist.ToList());

                        MemoryStream stream = new MemoryStream();
                        System.IO.StreamWriter sw = new System.IO.StreamWriter(stream);
                        await sw.WriteAsync(output);
                        sw.Flush();
                        await StorageAbstraction.WriteFile(stream, "NmBlacklist.json");
                    }
                }
                catch (Exception e) { Log(MethodBase.GetCurrentMethod(), e); }
                finally { savingBlacklist = false; }
            });
        }

        private void ClosingEvent(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!closing)
            {
                if (getAllRunning)
                {
                    e.Cancel = true;
                    ShowClosingDialog("Get All is still running." + Environment.NewLine + "Abort?");
                }
                else if (DownloadScheduler.downloading.Count > 0)
                {
                    e.Cancel = true;
                    ShowClosingDialog("There are still " + DownloadScheduler.downloading.Count + " running downloads." + Environment.NewLine + "Abort?");
                }
            }
            if (!e.Cancel == true)
            {
                while (savingDB || savingSettings || savingSettings)
                {
                    Task.Delay(100).Wait();
                }

                MtpDevice.Disconnect();
            }
        }

        private async void ShowClosingDialog(string message)
        {
            MessageBox.MessageBoxResult res = await MessageBox.Show(MainWindow.s_instance, message, "Warning", MessageBox.MessageBoxButtons.OkCancel);

            if (res == MessageBox.MessageBoxResult.Ok)
            {
                DownloadScheduler.queue.Clear();
                await SaveLocalItems();
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
            Log("Error " + m.ReflectedType.Name + " " + e.Message);
        }

        public static void Log(string message)
        {
            Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine(message);

                    string directory = Path.Combine(Environment.GetFolderPath(SpecialFolder.ApplicationData), "NoodleManagerX");
                    if (!System.IO.Directory.Exists(directory))
                    {
                        System.IO.Directory.CreateDirectory(directory);
                    }

                    using (System.IO.StreamWriter sw = System.IO.File.AppendText(Path.Combine(directory, "Log.txt")))
                    {
                        await sw.WriteAsync(DateTime.Now.ToString("dd'.'MM HH':'mm':'ss") + "     " + message + Environment.NewLine);
                    }
                }
                catch { }
            });
        }
    }
}
