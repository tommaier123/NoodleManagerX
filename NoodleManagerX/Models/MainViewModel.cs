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
using SharpAdbClient;
using System.Net;
using System.IO.Compression;
using System.Diagnostics;
using ManagedBass;
using System.Reactive.Linq;

namespace NoodleManagerX.Models
{
    class MainViewModel : ReactiveObject
    {
        //dotnet publish -c Release -f netcoreapp3.1 -r win-x64 --self-contained true /p:PublishSingleFile=true -p:PublishTrimmed=True -p:TrimMode=Link -p:PublishReadyToRun=false
        //dotnet publish -c Release -f netcoreapp3.1 -r linux-x64 --self-contained true /p:PublishSingleFile=true -p:PublishTrimmed=True -p:TrimMode=Link -p:PublishReadyToRun=false
        //dotnet publish -c Release -f netcoreapp3.1 -r osx-x64 --self-contained true /p:PublishSingleFile=true  -p:PublishTrimmed=True -p:TrimMode=Link -p:PublishReadyToRun=false





        //Todo:
        //check which dispatchers should be awaited
        //possibly remove functions with switch parameters
        //strip unnecessary ADB files
        //use api filename instead of content dispositon
        //context menu for description
        //multiple files/versioning
        //implement download timeout/retry counter
        //fix settings path dispaly


        public static MainViewModel s_instance;

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

        [Reactive] public Settings settings { get; set; } = new Settings();
        [Reactive] public string questSerial { get; set; } = "";

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
        public ObservableCollection<string> blacklist { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<MapItem> maps { get; private set; } = new ObservableCollection<MapItem>();
        public ObservableCollection<PlaylistItem> playlists { get; private set; } = new ObservableCollection<PlaylistItem>();
        public ObservableCollection<StageItem> stages { get; private set; } = new ObservableCollection<StageItem>();
        public ObservableCollection<AvatarItem> avatars { get; private set; } = new ObservableCollection<AvatarItem>();
        public List<LocalItem> localItems { get; set; } = new List<LocalItem>();

        public AdbServer adbServer = new AdbServer();
        public AdbClient adbClient = new AdbClient();
        public DeviceMonitor deviceMonitor;

        public bool closing = false;
        public bool downloadPage = false;//so that loading pages get downloaded when using get page
        public bool updatingLocalItems = false;

        public bool getAllRunning = false;

        public int apiRequestCounter = 0;

        public MapHandler mapHandler = new MapHandler();
        public PlaylistHandler playlistHandler = new PlaylistHandler();
        public StageHandler stageHandler = new StageHandler();
        public AvatarHandler avatarHandler = new AvatarHandler();

        public MainViewModel()
        {
            s_instance = this;

            MainWindow.s_instance.Closing += ClosingEvent;

            ExtractResources();
            LoadSettings();
            LoadBlacklist();

            if (!CheckDirectory(settings.synthDirectory))
            {
                settings.synthDirectory = "";
                GetDirectoryFromRegistry();
            }
            else
            {
                synthDirectory = settings.synthDirectory;
            }

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
                downloadPage = true;
                foreach (GenericItem item in items.Where(x => !x.downloaded && !x.downloading))
                {
                    DownloadScheduler.queue.Add(item);
                }
            }));

            selectDirectoryCommand = ReactiveCommand.Create((() =>
            {
                selectDirectory();
            }));

            //the correct number of events needs to be skipped in ordere to avoid duplication
            //1 for properties that get initialized
            //2 for properties that get declared and are initialized via bindings

            this.WhenAnyValue(x => x.currentPage).Skip(1).Subscribe(x => GetPage());//reload maps when the current page changes
            this.WhenAnyValue(x => x.selectedSearchParameter).Skip(2).Subscribe(x => GetPage());//reload maps when the search parameter changes
            this.WhenAnyValue(x => x.selectedDifficulty).Skip(2).Subscribe(x => GetPage());//reload maps when the search difficulty changes
            this.WhenAnyValue(x => x.selectedSortMethod).Skip(2).Subscribe(x => GetPage());//reload maps when the sort method changes
            this.WhenAnyValue(x => x.selectedSortOrder).Skip(2).Subscribe(x => GetPage());//reload maps when the sort order changes

            this.WhenAnyValue(x => x.questSerial).Skip(1).Subscribe(x => LoadLocalItems());//reload local when the quest status changes
            settings.Changed.Subscribe(x => { SaveSettings(); LoadLocalItems(); LoadBlacklist(); });//save the settings when they change
            this.WhenAnyValue(x => x.synthDirectory).Skip(1).Subscribe(x =>
            {
                directoryValid = CheckDirectory(synthDirectory);
                if (directoryValid)
                {
                    Log("Directory changed to " + synthDirectory);
                    settings.synthDirectory = synthDirectory;//save the current directory to the settings if it has changed and is valid
                    LoadBlacklist();
                    LoadLocalItems();
                }
            });

            items.CollectionChanged += ItemsCollectionChanged;
            blacklist.CollectionChanged += BlacklistCollectionChanged;

            LoadLocalItems();
            GetPage();
        }

        private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateCollections();
        }

        private void BlacklistCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SaveBlacklist();
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

        public void LoadLocalItems()
        {
            if (updatingLocalItems)
            {
                return;//prevent issues with multithreading
            }

            Task.Run(() =>
            {
                updatingLocalItems = true;
                Log("Loading local items");
                localItems.Clear();

                mapHandler.LoadLocalItems();
                playlistHandler.LoadLocalItems();
                stageHandler.LoadLocalItems();
                avatarHandler.LoadLocalItems();

                Log("Done loading local items");

                foreach (GenericItem item in items)
                {
                    item.UpdateDownloaded();
                }
                updatingLocalItems = false;
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
            if (CheckDirectory(settings.synthDirectory, true))
            {
                searchText = lastSearchText = "";
                currentPage = 1;
                selectedSearchParameterIndex = 0;
                selectedDifficultyIndex = 0;
                selectedSortMethodIndex = 0;
                selectedSortOrderIndex = 0;
                GetPage();

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

        public void ExtractResources()
        {
            Task.Run(() =>
            {
                try
                {
                    string platform = "linux";
                    string extension = "";

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        platform = "windows";
                        extension = ".exe";
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        platform = "osx";
                    }

                    string location = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    string resources = Path.Combine(location, "Resources");
                    string adbExe = Path.Combine(resources, "ADB", "adb" + extension);

                    using (Stream resourceFile = Assembly.GetExecutingAssembly().GetManifestResourceStream("NoodleManagerX.Resources." + platform + ".zip"))
                    using (ZipArchive archive = new ZipArchive(resourceFile))
                    {
                        string newVersion = "";
                        string oldVersion = "0";
                        ZipArchiveEntry versionEntry = archive.GetEntry(@"Resources/version.txt");

                        if (versionEntry != null)
                        {
                            using (StreamReader sr = new StreamReader(versionEntry.Open()))
                            {
                                newVersion = sr.ReadToEnd();
                            }
                        }
                        string versionFile = Path.Combine(resources, "version.txt");
                        if (File.Exists(versionFile))
                        {
                            using (StreamReader sr = new StreamReader(versionFile))
                            {
                                oldVersion = sr.ReadToEnd();
                            }
                        }

                        bool update = oldVersion != newVersion && newVersion != "";

                        if (update)
                        {
                            Log("Updating resources from version " + oldVersion + " to " + newVersion + " at " + location);
                            bool writeable = true;
                            if (Directory.Exists(resources))
                            {
                                try//check if all files can be opened
                                {
                                    foreach (string file in Directory.GetFiles(resources, "*", SearchOption.AllDirectories))
                                    {
                                        using (FileStream stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite))
                                        {
                                            stream.Close();
                                        }
                                    }
                                    Directory.Delete(resources, true);
                                }
                                catch (Exception e)
                                {
                                    writeable = false;
                                    MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
                                }
                            }
                            if (writeable)
                            {
                                foreach (ZipArchiveEntry entry in archive.Entries)
                                {
                                    if (!Path.EndsInDirectorySeparator(entry.FullName))
                                    {
                                        string path = Path.GetFullPath(Path.Combine(location, entry.FullName));
                                        string directory = Path.GetDirectoryName(path);
                                        Directory.CreateDirectory(directory);
                                        entry.ExtractToFile(path, true);
                                    }
                                }
                            }
                        }
                        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            Shell("chmod 755 " + adbExe);
                        }
                    }
                    StartAdbServer(adbExe);
                }
                catch (Exception e)
                {
                    MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
                }
            });
        }

        public void StartAdbServer(string path)
        {
            Task.Run(() =>
            {
                try
                {
                    Log("Looking for adb executable at " + path);
                    var result = adbServer.StartServer(path, restartServerIfNewer: false);
                    MainViewModel.Log("Adb server " + result);

                    deviceMonitor = new DeviceMonitor(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)));
                    deviceMonitor.DeviceConnected += this.OnDeviceConnected;
                    deviceMonitor.DeviceDisconnected += this.OnDevicDisconnected;
                    deviceMonitor.Start();
                }
                catch (Exception e)
                {
                    MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
                }
            });
        }

        public string Shell(string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }

        private void OnDeviceConnected(object sender, DeviceDataEventArgs e)
        {
            Task.Run(async () =>
            {
                await Task.Delay(100);
                List<DeviceData> devices = adbClient.GetDevices();
                foreach (DeviceData device in devices)
                {
                    if (device.Serial == e.Device.Serial)
                    {
                        //Log(device.Product + " " + device.Model + " " + device.Name + " " + device.Serial);

                        if (device.Product == "vr_monterey")
                        {
                            if (questSerial != device.Serial)
                            {
                                if (questSerial == "")
                                {
                                    questSerial = device.Serial;
                                    Log("Quest connected " + questSerial);
                                }
                                else
                                {
                                    Log("Multiple quests connected");
                                    _ = Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        _ = MessageBox.Show(MainWindow.s_instance, "There are multiple quests connected." + Environment.NewLine + "Only one can be used at a time.", "Warning", MessageBox.MessageBoxButtons.Ok);
                                    });
                                }
                            }
                        }
                    }
                }
            });
        }

        private void OnDevicDisconnected(object sender, DeviceDataEventArgs e)
        {
            Task.Run(() =>
            {
                if (questSerial == e.Device.Serial)
                {
                    Log("Quest disconnected " + questSerial);
                    questSerial = "";
                }
            });
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
            try
            {
                OpenFolderDialog dialog = new OpenFolderDialog();
                dialog.Directory = synthDirectory;

                string directory = await dialog.ShowAsync(MainWindow.s_instance);

                if (!String.IsNullOrEmpty(directory))
                {
                    string parent = Directory.GetParent(directory).FullName;
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
            catch { }
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

        public static List<string> QuestDirectoryGetFiles(string path)
        {
            List<string> ret = new List<string>();
            if (QuestPathExists(path))
            {

                var quests = MainViewModel.s_instance.adbClient.GetDevices().Where(x => x.Serial == MainViewModel.s_instance.questSerial);
                if (quests.Count() > 0)
                {
                    DeviceData device = quests.First();

                    var receiver = new ConsoleOutputReceiver();

                    MainViewModel.s_instance.adbClient.ExecuteRemoteCommand("ls sdcard/Android/data/com.kluge.SynthRiders/files/" + path, device, receiver);
                    ret.AddRange(receiver.ToString().Split(new char[] { '\n' }));
                }
            }
            return ret;
        }

        public static bool QuestPathExists(string path)
        {
            List<string> ret = new List<string>();
            var quests = MainViewModel.s_instance.adbClient.GetDevices().Where(x => x.Serial == MainViewModel.s_instance.questSerial);
            if (quests.Count() > 0)
            {
                DeviceData device = quests.First();

                var receiver = new ConsoleOutputReceiver();

                MainViewModel.s_instance.adbClient.ExecuteRemoteCommand("ls sdcard/Android/data/com.kluge.SynthRiders/files", device, receiver);

                return receiver.ToString().Contains(path);
            }
            return false;
        }

        public void LoadSettings()
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(SpecialFolder.ApplicationData), "NoodleManager", "Settings.json");
                MainViewModel.Log("Loading Settings from " + path);
                if (File.Exists(path))
                {
                    using (StreamReader file = File.OpenText(path))
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
                    MainViewModel.Log("Saving Settings");
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

        public void LoadBlacklist()
        {
            try
            {
                blacklist.Clear();
                if (CheckDirectory(settings.synthDirectory))
                {
                    string path = Path.Combine(settings.synthDirectory, "NmBlacklist.json");
                    MainViewModel.Log("Loading Blacklist from " + path);
                    if (File.Exists(path))
                    {
                        using (StreamReader file = File.OpenText(path))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            blacklist.AddRange((List<string>)serializer.Deserialize(file, typeof(List<string>)));
                        }
                    }
                }
                foreach (GenericItem item in items)
                {
                    item.UpdateBlacklisted();
                }
            }
            catch (Exception e) { Log(MethodBase.GetCurrentMethod(), e); }
        }

        public void SaveBlacklist()
        {
            Task.Run(async () =>
            {
                try
                {
                    if (CheckDirectory(settings.synthDirectory))
                    {
                        MainViewModel.Log("Saving Blacklist");
                        string output = JsonConvert.SerializeObject(blacklist.ToList());

                        using (StreamWriter sw = new StreamWriter(Path.Combine(settings.synthDirectory, "NmBlacklist.json")))
                        {
                            await sw.WriteAsync(output);
                        }
                    }
                }
                catch (Exception e) { Log(MethodBase.GetCurrentMethod(), e); }
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
                //cleanup
                Bass.Free();
            }
        }

        private async void ShowClosingDialog(string message)
        {
            MessageBox.MessageBoxResult res = await MessageBox.Show(MainWindow.s_instance, message, "Warning", MessageBox.MessageBoxButtons.OkCancel);

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
            Log("Error " + m.ReflectedType.Name + " " + e.Message);
        }

        public static void Log(string message)
        {
            Console.WriteLine(message);
            File.AppendAllText("log.txt", message + Environment.NewLine);
        }
    }
}
