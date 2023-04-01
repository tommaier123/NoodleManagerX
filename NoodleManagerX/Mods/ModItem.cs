using NoodleManagerX.Models;
using NoodleManagerX.Models.Mods;
using ReactiveUI;
using Semver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Net.Http;
using Avalonia.Controls.Shapes;
using Newtonsoft.Json;
using System.IO.Compression;
using Path = System.IO.Path;
using System.Data.SqlTypes;
using ReactiveUI.Fody.Helpers;

namespace NoodleManagerX.Mods
{
    [DataContract]
    class ModItem : GenericItem
    {
        public static string baseDownloadUrl = ModDownloadSource.GetModItemBaseDownloadUrl();
        public override string target { get; set; } = "Mods";
        public override ItemType itemType { get; set; } = ItemType.Mod;

        private Dictionary<string, ModVersion> ResolvedVersions { get; set; }

        [DataMember] public ModInfo ModInfo { get; private set; }
        [DataMember] public ModVersion SelectedVersion { get; private set; }
        [Reactive] public string DisplayVersion { get; private set; }
        [Reactive] public bool CanDelete { get; private set; } = true;

        [DataMember]
        public ModVersion ResolvedVersion
        {
            get
            {
                return ResolvedVersions[ModInfo.Id];
            }
        }

        [DataMember]
        public ModVersion InstalledVersion
        {
            get
            {
                return ((ModHandler)handler).GetInstalledVersion(ModInfo.Id);
            }
            private set
            {
                ((ModHandler)handler).UpdateInstalledVersion(ModInfo.Id, value);
            }
        }


        public ModItem(ModInfo modInfo, ModVersion installedVersion, Dictionary<string, ModVersion> resolvedVersions)
        {
            this.ModInfo = modInfo;
            this.ResolvedVersions = resolvedVersions;
            SelectVersion(installedVersion);

            this.name = modInfo.Name;
            this.description = modInfo.Description;
            this.user = new User
            {
                username = modInfo.Author
            };
        }

        public override void LoadBitmap()
        {
            Console.WriteLine("No bitmaps needed for mod items");
        }

        public void TryDownload()
        {
            if (MainViewModel.s_instance.selectedTabIndex != MainViewModel.TAB_MODS)
            {
                MainViewModel.Log("Ignoring mod download when not on mods tab");
                return;
            }

            if (MtpDevice.connected)
            {
                MainViewModel.s_instance.OpenErrorDialog("Mods only supported on PCVR");
                return;
            }

            if (StorageAbstraction.CanDownload() && !MainViewModel.s_instance.updatingLocalItems)
            {
                blacklisted = false;
                UpdateInstalledVersionOnDownload();
                DownloadScheduler.Download(this);
            }
        }

        public void TryDelete()
        {
            if (!MainViewModel.s_instance.updatingLocalItems)
            {
                if (downloaded && CanDelete)
                {
                    Task.Run(Delete);
                }
            }
        }

        protected override void SetupCommands()
        {
            downloadCommand = ReactiveCommand.Create((() =>
            {
                TryDownload();
            }));

            deleteCommand = ReactiveCommand.Create((() =>
            {
                TryDelete();
            }));
        }

        private async Task<string> DownloadAndSaveVersion(ModVersion modVersion)
        {
            var path = "";

            try
            {
                string url = baseDownloadUrl + "/" + modVersion.DownloadUrl;
                MainViewModel.Log($"Downloading from {url}");

                using HttpClient client = new HttpClient();
                using var rawResponse = await client.GetStreamAsync(url);
                using MemoryStream str = await CopyStreamToMemoryStream(rawResponse);
                if (String.IsNullOrEmpty(filename))
                {
                    filename = modVersion.DownloadUrl.Split("/").Last();
                }

                path = Path.Combine(target, filename);
                await StorageAbstraction.WriteFile(str, path);

                if (!await ExtractModFilesToSynthDir(path))
                {
                    DisplayVersion = "---";
                    path = "";
                }
            }
            catch (Exception e)
            {
                DisplayVersion = "---";
                MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
            }

            return path;
        }

        protected async override Task<string> RawDownloadAndSave()
        {
            // Download dependencies
            foreach (var dependency in SelectedVersion.Dependencies)
            {
                var depModItem = MainViewModel.s_instance.mods.FirstOrDefault(mod => mod.ModInfo.Id == dependency.Id);
                depModItem.TryDownload();
            }

            var baseModPath = await DownloadAndSaveVersion(SelectedVersion);
            if (baseModPath == "")
            {
                MainViewModel.Log($"Failed to download and save base file for mod {ModInfo.Id}");
                return "";
            }

            ((ModHandler)handler).RefreshUI();

            return baseModPath;
        }

        private async Task<bool> ExtractModFilesToSynthDir(string synthmodPath)
        {
            if (MtpDevice.connected)
            {
                MainViewModel.s_instance.OpenErrorDialog("Cannot download mod while MTP device (Oculus) is connected!");
                return false;
            }

            try
            {
                if (StorageAbstraction.FileExists(synthmodPath))
                {
                    using (Stream stream = StorageAbstraction.ReadFile(synthmodPath))
                    using (ZipArchive archive = new ZipArchive(stream))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            if (entry.FullName != "LocalItem.json")
                            {
                                if (StorageAbstraction.FileExists(entry.FullName))
                                {
                                    MainViewModel.Log($"WARNING: Overwriting {entry.FullName}");
                                    // TODO use a more generic function once modding is supported on Oculus
                                    var fullPath = StorageAbstraction.GetFullComputerPath(entry.FullName);
                                    entry.ExtractToFile(fullPath, true);
                                }
                                else
                                {
                                    if (String.IsNullOrEmpty(entry.Name))
                                    {
                                        // No file name, so directory
                                        MainViewModel.Log($"Creating directory for {entry.FullName}");
                                        await StorageAbstraction.CreateDirectory(entry.FullName);
                                    }
                                    else
                                    {
                                        MainViewModel.Log($"Extracting file {entry.FullName}");
                                        // TODO use a more generic function once modding is supported on Oculus
                                        var fullPath = StorageAbstraction.GetFullComputerPath(entry.FullName);
                                        entry.ExtractToFile(fullPath);
                                    }
                                }
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception e) { MainViewModel.Log(MethodBase.GetCurrentMethod(), e); }

            return false;
        }

        public override bool Delete(string filename)
        {
            if (MtpDevice.connected)
            {
                MainViewModel.s_instance.OpenErrorDialog("Cannot delete mod while MTP device (Oculus) is connected!");
                return false;
            }

            if (!String.IsNullOrEmpty(filename) && filename.EndsWith(".synthmod"))
            {
                try
                {
                    var synthmodPath = Path.Combine(target, filename);
                    if (StorageAbstraction.FileExists(synthmodPath))
                    {
                        using (Stream stream = StorageAbstraction.ReadFile(synthmodPath))
                        using (ZipArchive archive = new ZipArchive(stream))
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                if (entry.FullName != "LocalItem.json")
                                {
                                    var fullPath = Path.Combine(MainViewModel.s_instance.settings.synthDirectory, entry.FullName);
                                    if (StorageAbstraction.FileExists(entry.FullName))
                                    {
                                        MainViewModel.Log("Deleting " + fullPath);
                                        StorageAbstraction.DeleteFile(fullPath);
                                    }
                                    else if (!StorageAbstraction.DirectoryExists(fullPath))
                                    {
                                        MainViewModel.Log($"Mod file {entry.FullName} already deleted");
                                    }
                                }
                            }
                        }

                        // Deleted all pieces, now delete the raw file itself
                        MainViewModel.Log($"Deleting {synthmodPath}");
                        StorageAbstraction.DeleteFile(synthmodPath);
                    }

                    // Cache installed version before it is cleared
                    var installedVersion = InstalledVersion;

                    // Unassign versions for dependency checks
                    SelectVersion(null);
                    InstalledVersion = null;

                    DeleteOrphanedDependencies(installedVersion);

                    // Remove from localItems
                    MainViewModel.s_instance.localItems = MainViewModel.s_instance.localItems.Where(x => x != null && !(x.itemType == itemType && x.filename == filename)).ToList();

                    // Refresh mod items after link to dependencies is severed
                    ((ModHandler)handler).RefreshUI();

                    return true;
                }
                catch (Exception e) { MainViewModel.Log(MethodBase.GetCurrentMethod(), e); }
            }
            return false;
        }

        private void DeleteOrphanedDependencies(ModVersion installedVersion)
        {
            if (installedVersion == null)
            {
                MainViewModel.Log($"Installed version is null for mod '{ModInfo.Id}', cannot delete orphaned dependencies");
            }
            foreach (var dependency in installedVersion.Dependencies)
            {
                var isDepOrphaned = MainViewModel.s_instance.mods.FirstOrDefault(mod => {
                    if (mod.ModInfo.Id == ModInfo.Id)
                    {
                        // Ignore ourselves
                        return false;
                    }

                    return mod.InstalledVersion?.HasDependency(dependency.Id) ?? false;
                }) == null;

                if (isDepOrphaned)
                {
                    MainViewModel.Log($"Deleting orphaned dependency {dependency.Id} for mod {ModInfo.Id}");
                    var depModItem = MainViewModel.s_instance.mods.FirstOrDefault(mod => mod.ModInfo.Id == dependency.Id);
                    depModItem.RefreshDeleteStatus();
                    depModItem.TryDelete();
                }
            }
        }

        public void RefreshDeleteStatus()
        {
            CanDelete = !((ModHandler)handler).IsAnInstalledDependency(ModInfo.Id);
        }

        private void UpdateInstalledVersionOnDownload()
        {
            SelectedVersion = ResolvedVersion;
            InstalledVersion = ResolvedVersion;
            DisplayVersion = InstalledVersion?.Version?.ToString() ?? "ERR";
        }

        private void SelectVersion(ModVersion version)
        {
            SelectedVersion = version;
            DisplayVersion = VersionToDisplayString(version);
        }

        public string VersionToDisplayString(ModVersion version)
        {
            if (version?.Version == null)
            {
                return "---";
            }

            var displayed = version.Version.ToString();
            if (ResolvedVersion.Version.ComparePrecedenceTo(version?.Version) != 0)
            {
                displayed += "*";
            }
            return displayed;
        }
    }
}
