using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using DynamicData;
using Microsoft.Win32;
using MsgBox;
using Newtonsoft.Json;
using NoodleManagerX.Mods;
using NoodleManagerX.Utils;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SharpAdbClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Environment;
using MemoryStream = System.IO.MemoryStream;
using Path = System.IO.Path;
using Stream = System.IO.Stream;

namespace NoodleManagerX.Models
{
    class MainViewModel : ReactiveObject
    {
        //dotnet publish -c Release -f net5.0 -r win-x64 --self-contained true /p:PublishSingleFile=true -p:PublishTrimmed=True -p:TrimMode=Link -p:PublishReadyToRun=false
        //dotnet publish -c Release -f net5.0 -r linux-x64 --self-contained true /p:PublishSingleFile=true -p:PublishTrimmed=True -p:TrimMode=Link -p:PublishReadyToRun=false
        //dotnet publish -c Release -f net5.0 -r osx-x64 --self-contained true /p:PublishSingleFile=true  -p:PublishTrimmed=True -p:TrimMode=Link -p:PublishReadyToRun=false


        //Todo:
        //get description when rightclicking an item and display in context menu
        //multiple files
        //don't leave thread safety up to luck
        //get a updated at timestamp for updating, published at is fine for filesystem timestamp
        //splitting into different lists isn't necessary
        //using PathIcon will inherit button foreground
        //blacklist mappers
        //clean up styles 

        public const int TAB_MAPS = 0;
        public const int TAB_PLAYLISTS = 1;
        public const int TAB_STAGES = 2;
        public const int TAB_AVATARS = 3;
        public const int TAB_MODS = 4;

        [Reactive] private string version { get; set; } = "V1.0.0";

        public static MainViewModel s_instance;

        [Reactive] public Settings settings { get; set; } = new Settings();

        [Reactive] public int selectedTabIndex { get; set; } = TAB_MAPS;
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
        [Reactive] public string progressText { get; set; } = null;
        [Reactive] public bool questConnected { get; set; } = false;
        [Reactive] public bool updatingLocalItems { get; set; } = false;
        [Reactive] public int previewVolume { get; set; } = 50;

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
        public ReactiveCommand<Unit, Unit> openLogsCommand { get; set; }
        public ReactiveCommand<Unit, Unit> twitterCommand { get; set; }
        public ReactiveCommand<Unit, Unit> githubCommand { get; set; }
        public ReactiveCommand<Unit, Unit> twitchCommand { get; set; }
        public ReactiveCommand<Unit, Unit> youtubeCommand { get; set; }

        public ObservableCollection<GenericItem> items { get; set; } = new ObservableCollection<GenericItem>();
        public ObservableCollection<string> blacklist { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<MapItem> maps { get; private set; } = new ObservableCollection<MapItem>();
        public ObservableCollection<PlaylistItem> playlists { get; private set; } = new ObservableCollection<PlaylistItem>();
        public ObservableCollection<StageItem> stages { get; private set; } = new ObservableCollection<StageItem>();
        public ObservableCollection<AvatarItem> avatars { get; private set; } = new ObservableCollection<AvatarItem>();
        public List<LocalItem> localItems { get; set; } = new List<LocalItem>();

        public AdbServer adbServer = new AdbServer();
        public AdbClient adbClient = new AdbClient();

        public bool pruning = false;//maps without metadata should be deleted
        public bool downloadPage = false;//so that loading maps get downloaded when using get page
        public volatile bool closing = false;
        public volatile bool getAllRunning = false;
        public volatile bool savingDB = false;
        public volatile bool savingBlacklist = false;
        public volatile bool savingSettings = false;

        public volatile int apiRequestCounter = 0;

        public MapHandler mapHandler = new MapHandler();
        public PlaylistHandler playlistHandler = new PlaylistHandler();
        public StageHandler stageHandler = new StageHandler();
        public AvatarHandler avatarHandler = new AvatarHandler();
        public ModHandler modHandler = new ModHandler();

        public MainViewModel()
        {
            s_instance = this;

            Task.Run(async () =>
            {
                MainWindow.s_instance.Closing += ClosingEvent;

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

                tabSelectCommand = ReactiveCommand.Create<string>(x =>
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
                });

                pageUpCommand = ReactiveCommand.Create(() =>
                {
                    if (currentPage < numberOfPages)
                    {
                        currentPage++;
                    }
                });

                pageDownCommand = ReactiveCommand.Create(() =>
                {
                    if (currentPage > 1)
                    {
                        currentPage--;
                    }
                });

                searchCommand = ReactiveCommand.Create(() =>
                {
                    GetPage();
                });

                getAllCommand = ReactiveCommand.Create(() =>
                {
                    GetAll();
                });

                getPageCommand = ReactiveCommand.Create(() =>
                {
                    if (selectedTabIndex != TAB_MAPS)
                    {
                        OpenErrorDialog("Currently only map downloading is supported");
                        return;
                    }

                    Task.Run(() =>
                    {
                        if (!updatingLocalItems)
                        {
                            downloadPage = true;//download items that are still being requested from the server
                            var toDownload = items.Where(x => !x.downloaded && !x.downloading);
                            if (toDownload.Count() > 0)
                            {
                                _ = Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    progress = 1;
                                    progressText = "Getting Page";

                                });
                                foreach (GenericItem item in toDownload)
                                {
                                    DownloadScheduler.Download(item);
                                }
                            }
                        }
                    });
                });

                selectDirectoryCommand = ReactiveCommand.Create(() =>
                {
                    selectDirectory();
                });

                connectQuestCommand = ReactiveCommand.Create(() =>
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
                });

                openLogsCommand = ReactiveCommand.Create(() =>
                {
                    Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(SpecialFolder.ApplicationData), "NoodleManagerX")) { UseShellExecute = true });
                });

                twitterCommand = ReactiveCommand.Create(() => { Process.Start(new ProcessStartInfo("https://twitter.com/Nova_Max_") { UseShellExecute = true }); });
                githubCommand = ReactiveCommand.Create(() => { Process.Start(new ProcessStartInfo("https://github.com/tommaier123/NoodleManagerX") { UseShellExecute = true }); });
                twitchCommand = ReactiveCommand.Create(() => { Process.Start(new ProcessStartInfo("https://www.twitch.tv/nova_max_") { UseShellExecute = true }); });
                youtubeCommand = ReactiveCommand.Create(() => { Process.Start(new ProcessStartInfo("https://www.youtube.com/channel/UCMebdv6hmIddqPee9AtO6Nw") { UseShellExecute = true }); });

                await LoadSettings();

                if (!settings.ignoreUpdates)
                {
                    try
                    {
                        Octokit.GitHubClient github = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("NoodleManagerX"));
                        var all = github.Repository.Release.GetAll("tommaier123", "NoodleManagerX").Result;
                        if (!settings.getBetas) all = all.Where(x => x.Prerelease == false).ToList();
                        var latest = all.OrderByDescending(x => Int32.Parse(x.TagName.Substring(1).Replace(".", ""))).FirstOrDefault();

                        if (latest != null)
                        {
                            if (Int32.Parse(version.Substring(1).Replace(".", "")) < Int32.Parse(latest.TagName.Substring(1).Replace(".", "")))
                            {
                                Log("Update available to: " + latest.TagName);

                                string beta = "";
                                if (latest.Prerelease) beta = "Note: This is a beta version and may contain bugs" + Environment.NewLine;

                                var res = await Dispatcher.UIThread.InvokeAsync(async () =>
                                {
                                    return await MessageBox.Show(MainWindow.s_instance, "New Update to " + latest.TagName + " available:" + Environment.NewLine + beta + Environment.NewLine + latest.Body + Environment.NewLine + Environment.NewLine + "Do you want to Download it?", "Update Available", MessageBox.MessageBoxButtons.OkCancel);
                                });

                                if (res == MessageBox.MessageBoxResult.Ok)
                                {
                                    using (var client = new WebClient())
                                    {
                                        client.DownloadProgressChanged += (object sender, DownloadProgressChangedEventArgs e) =>
                                        {
                                            _ = Dispatcher.UIThread.InvokeAsync(() =>
                                         {
                                             progress = e.ProgressPercentage;
                                             progressText = "Updating: " + progress + "%";
                                         });
                                        };
                                        string temp = Path.GetTempPath();
                                        string location = Path.Combine(temp, "NoodleManagerX.exe");
                                        string locationHelper = Path.Combine(temp, "UpdateHelper.exe");
                                        Log("Writing update files to " + temp);
                                        if (System.IO.File.Exists(location)) System.IO.File.Delete(location);
                                        await client.DownloadFileTaskAsync("https://github.com/tommaier123/NoodleManagerX/releases/download/" + latest.TagName + "/NoodleManagerX.exe", location);

                                        using (Stream resourceFile = Assembly.GetExecutingAssembly().GetManifestResourceStream("NoodleManagerX.Resources.UpdateHelper.exe"))
                                        using (System.IO.FileStream fs = System.IO.File.Open(locationHelper, System.IO.FileMode.Create))
                                        {
                                            await resourceFile.CopyToAsync(fs);
                                        }

                                        string filename = Process.GetCurrentProcess().MainModule.FileName;
                                        Process proc = new Process();
                                        proc.StartInfo.FileName = locationHelper;
                                        proc.StartInfo.Arguments = "\"" + filename + "\"";
                                        proc.StartInfo.UseShellExecute = true;

                                        string testfile = Path.Combine(Path.GetDirectoryName(filename), "NmUpdate_can_be_deleted");
                                        try
                                        {
                                            using (System.IO.FileStream fs = System.IO.File.Open(testfile, System.IO.FileMode.Create)) { }
                                        }
                                        catch (UnauthorizedAccessException)
                                        {
                                            Log("Needs admin permissions to update");
                                            proc.StartInfo.Verb = "runas";
                                        }
                                        catch { }
                                        finally { try { System.IO.File.Delete(testfile); } catch { } }

                                        proc.Start();

                                        await Dispatcher.UIThread.InvokeAsync(() =>
                                        {
                                            closing = true;
                                            MainWindow.s_instance.Close();
                                        });
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e) { Log(MethodBase.GetCurrentMethod(), e); }
                }

                settings.Changed.Subscribe(x => { SaveSettings(); });//save the settings when they change

                if (!CheckDirectory(settings.synthDirectory))
                {
                    settings.synthDirectory = "";
                    GetDirectoryFromRegistry();
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    synthDirectory = settings.synthDirectory;
                    directoryValid = CheckDirectory(settings.synthDirectory);
                    previewVolume = settings.previewVolume;
                });

                // synthDirectory should be set by now
                if (directoryValid)
                {
                    var gameDataDir = Path.Combine(synthDirectory, "SynthRiders_Data");
                    UnityInformationHandler.Setup(gameDataDir);
                }

                this.WhenAnyValue(x => x.synthDirectory).Skip(1).Subscribe(x =>
                {
                    directoryValid = CheckDirectory(synthDirectory);
                    if (settings.synthDirectory != synthDirectory && directoryValid)
                    {
                        settings.synthDirectory = synthDirectory;
                        var gameDataDir = Path.Combine(synthDirectory, "SynthRiders_Data");
                        UnityInformationHandler.Setup(gameDataDir);
                        ReloadLocalSources(true);
                    }
                });

                this.WhenAnyValue(x => x.previewVolume).Skip(1).Subscribe(x =>
                {
                    PlaybackHandler.SetVolume(previewVolume);
                });

                //the correct number of events needs to be skipped in ordere to avoid duplication, this depends on the initialization order of view and viewModel
                this.WhenAnyValue(x => x.currentPage).Skip(1).Subscribe(x => GetPage());//reload maps when the current page changes
                this.WhenAnyValue(x => x.selectedSearchParameter).Skip(1).Subscribe(x => GetPage());//reload maps when the search parameter changes
                this.WhenAnyValue(x => x.selectedDifficulty).Skip(1).Subscribe(x => GetPage());//reload maps when the search difficulty changes
                this.WhenAnyValue(x => x.selectedSortMethod).Skip(1).Subscribe(x => GetPage());//reload maps when the sort method changes
                this.WhenAnyValue(x => x.selectedSortOrder).Skip(1).Subscribe(x => GetPage());//reload maps when the sort order changes

                items.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => UpdateCollections();
                blacklist.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => SaveBlacklist();

                MtpDevice.Connect();
                ReloadLocalSources();
                _ = GetPage();
            });
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
                    case TAB_MAPS:
                        maps.Clear();
                        maps.AddRange(items.Where(x => x.itemType == ItemType.Map).Select(x => (MapItem)x));
                        break;
                    case TAB_PLAYLISTS:
                        playlists.Clear();
                        playlists.AddRange(items.Where(x => x.itemType == ItemType.Playlist).Select(x => (PlaylistItem)x));
                        break;
                    case TAB_STAGES:
                        stages.Clear();
                        stages.AddRange(items.Where(x => x.itemType == ItemType.Stage).Select(x => (StageItem)x));
                        break;
                    case TAB_AVATARS:
                        avatars.Clear();
                        avatars.AddRange(items.Where(x => x.itemType == ItemType.Avatar).Select(x => (AvatarItem)x));
                        break;
                }
            });
        }

        public Task GetPage()
        {
            if (lastSearchText != searchText)
            {
                currentPage = 1;
                lastSearchText = searchText;
            }
            apiRequestCounter++;//invalidate all running requests
            downloadPage = false;

            return Task.Run(async () =>
            {
                switch (selectedTabIndex)
                {
                    case TAB_MAPS:
                        await mapHandler.GetPage();
                        break;
                    case TAB_PLAYLISTS:
                        await playlistHandler.GetPage();
                        break;
                    case TAB_STAGES:
                        await stageHandler.GetPage();
                        break;
                    case TAB_AVATARS:
                        await avatarHandler.GetPage();
                        break;
                    case TAB_MODS:
                        await modHandler.GetPage();
                        break;
                }
            });
        }

        public Task GetAll()
        {
            if (selectedTabIndex != TAB_MAPS)
            {
                OpenErrorDialog("Currently only map downloading is supported");
                return Task.CompletedTask;
            }

            if (StorageAbstraction.CanDownload() && !updatingLocalItems)
            {
                progress = 1;
                progressText = "Getting All";
                DownloadScheduler.toDownload = DownloadScheduler.queue.Count;

                return Task.Run(async () =>
                {
                    switch (selectedTabIndex)
                    {
                        case TAB_MAPS:
                            await mapHandler.GetAll();
                            break;
                        case TAB_PLAYLISTS:
                            await playlistHandler.GetAll();
                            break;
                        case TAB_STAGES:
                            await stageHandler.GetAll();
                            break;
                        case TAB_AVATARS:
                            await avatarHandler.GetAll();
                            break;
                    }
                });
            }
            return Task.CompletedTask;
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
                ret = System.IO.Directory.Exists(path) && (settings.skipDirectoryCheck || System.IO.File.Exists(Path.Combine(path, "SynthRiders.exe")));
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
                            settings.synthDirectory = directory.Trim('\\');
                        }
                    }
                }
            }
            catch (Exception e) { Log(MethodBase.GetCurrentMethod(), e); }
        }

        public Task LoadLocalItems()
        {
            if (updatingLocalItems)
            {
                return Task.CompletedTask;//prevent issues with multithreading
            }

            return Task.Run(async () =>
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    progress = 1;
                    progressText = "Loading Database";
                    updatingLocalItems = true;
                });

                localItems.Clear();
                bool dbExists = false;

                try
                {
                    string path = "NmDatabase.json";
                    MainViewModel.Log("Loading Database");
                    if (StorageAbstraction.FileExists(path))
                    {
                        List<LocalItem> tmp = new List<LocalItem>();
                        using (Stream stream = StorageAbstraction.ReadFile(path))
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
                        var res = await MessageBox.Show(MainWindow.s_instance, "Click OK to delete maps without metadata e.g. if they were not downloaded from synthriderz.com" + Environment.NewLine + "This will only happen once when loading a new game directory" + Environment.NewLine + "Choosing to cancel might lead to duplicate maps" + Environment.NewLine + "Building the database for the first time can take some minutes", "Warning", MessageBox.MessageBoxButtons.OkCancel);
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

                await SaveLocalItems();

                pruning = false;

                _ = Dispatcher.UIThread.InvokeAsync(() =>
                 {
                     updatingLocalItems = false;
                     progress = 0;
                     progressText = null;
                 });
            });
        }

        public Task SaveLocalItems()
        {
            return Task.Run(async () =>
            {
                try
                {
                    if (savingDB) return;
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

        public Task LoadSettings()
        {
            return Task.Run(() =>
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
            });
        }

        public Task SaveSettings()
        {
            return Task.Run(async () =>
            {
                try
                {
                    if (savingSettings) return;
                    savingSettings = true;
                    MainViewModel.Log("Saving Settings");

                    settings.previewVolume = previewVolume;
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

        public Task LoadBlacklist()
        {
            return Task.Run(() =>
            {
                try
                {
                    blacklist.Clear();
                    string path = "NmBlacklist.json";
                    MainViewModel.Log("Loading Blacklist");
                    if (StorageAbstraction.FileExists(path))
                    {
                        using (Stream stream = StorageAbstraction.ReadFile(path))
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

        public Task SaveBlacklist()
        {
            return Task.Run(async () =>
            {
                try
                {
                    if (savingBlacklist) return;
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
                e.Cancel = true;
                Task.Run(() =>
                {
                    if (getAllRunning)
                    {
                        _ = Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            ShowClosingDialog("Get All is still running." + Environment.NewLine + "Abort?");
                        });
                    }
                    else if (DownloadScheduler.downloading.Count > 0)
                    {
                        _ = Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            ShowClosingDialog("There are still " + DownloadScheduler.downloading.Count + " running downloads." + Environment.NewLine + "Abort?");
                        });
                    }
                    else
                    {
                        CloseSafely();
                    }
                });
            }
        }

        private async void ShowClosingDialog(string message)
        {
            MessageBox.MessageBoxResult res = await MessageBox.Show(MainWindow.s_instance, message, "Warning", MessageBox.MessageBoxButtons.OkCancel);

            if (res == MessageBox.MessageBoxResult.Ok)
            {
                CloseSafely();
            }
        }

        private async void CloseSafely()
        {
            DownloadScheduler.queue.Clear();
            //await SaveLocalItems(); //this will miss items that were deleted but they will be removed from the db on launch
            await SaveSettings();
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

                  while (savingDB || savingSettings || savingSettings)
                  {
                      await Task.Delay(100);
                  }

                  //cleanup
                  MtpDevice.Disconnect();

                  _ = Dispatcher.UIThread.InvokeAsync(() =>
                  {
                      MainWindow.s_instance.Close();
                  });
              });
        }

        public static void CheckUiThread(string message)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("UiThread " + message);
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("BackgroundThread " + message);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        public static void Log(MethodBase m, Exception e)
        {
            Log("Error " + m.Name + " " + e.Message + Environment.NewLine + e.TargetSite + Environment.NewLine + e.StackTrace);
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
