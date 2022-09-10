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

namespace NoodleManagerX.Mods
{
    [DataContract]
    class ModItem : GenericItem
    {
        public static string baseDownloadUrl = "https://raw.githubusercontent.com/bookdude13/SRModsList/dev/SynthRiders/Downloads";
        public override string target { get; set; } = "Mods";
        public override ItemType itemType { get; set; } = ItemType.Mod;

        [DataMember] public ModInfo ModInfo { get; private set; }
        [DataMember] public ModVersion SelectedVersion { get; private set; }
        [DataMember] public ModVersion ResolvedVersion { get; private set; }


        public ModItem(ModInfo modInfo, ModVersion resolvedVersion)
        {
            this.ModInfo = modInfo;
            this.ResolvedVersion = resolvedVersion;

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

        protected override void SetupCommands()
        {
            downloadCommand = ReactiveCommand.Create((() =>
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
                    SelectedVersion = ResolvedVersion;
                    DownloadScheduler.Download(this);
                }
            }));

            deleteCommand = ReactiveCommand.Create((() =>
            {
                if (!MainViewModel.s_instance.updatingLocalItems)
                {
                    if (downloaded)
                    {
                        Task.Run(Delete);
                    }
                    else
                    {
                        blacklisted = !blacklisted;

                        if (blacklisted)
                        {
                            MainViewModel.s_instance.blacklist.Add(filename);
                        }
                        else
                        {
                            MainViewModel.s_instance.blacklist.Remove(filename);
                        }
                    }
                }
            }));
        }

        protected async override Task<string> RawDownloadAndSave()
        {
            var path = "";

            try
            {
                // TODO rename DownloadUrl to DownloadPath
                string url = baseDownloadUrl + "/" + SelectedVersion.DownloadUrl;

                using HttpClient client = new HttpClient();
                using var rawResponse = await client.GetStreamAsync(url);
                using MemoryStream str = await FixMetadata(rawResponse);
                if (String.IsNullOrEmpty(filename))
                {
                    filename = SelectedVersion.DownloadUrl.Split("/").Last();
                }

                path = Path.Combine(target, filename);
                await StorageAbstraction.WriteFile(str, path);

                if (! await ExtractModFilesToSynthDir(path))
                {
                    path = "";
                }
            }
            catch (Exception e)
            {
                MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
            }

            return path;
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
                                    await StorageAbstraction.WriteFile(await FixMetadata(stream), entry.FullName);
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
                                        await StorageAbstraction.WriteFile(await FixMetadata(stream), entry.FullName);
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

            // TODO delete dependencies as well, if they are orphaned
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

                    MainViewModel.s_instance.localItems = MainViewModel.s_instance.localItems.Where(x => x != null && !(x.itemType == itemType && x.filename == filename)).ToList();
                    return true;
                }
                catch (Exception e) { MainViewModel.Log(MethodBase.GetCurrentMethod(), e); }
            }
            return false;
        }

        public string DisplaySelectedVersion
        {
            get
            {
                var localItem = MainViewModel.s_instance.localItems.FirstOrDefault(item => item.CheckEquality(this));
                if (localItem == null)
                {
                    // Not installed
                    return "---";
                }

                if (localItem.ItemVersion == null)
                {
                    return "Latest";
                }

                var displayed = localItem.ItemVersion.ToString();
                if (localItem.ItemVersion.ComparePrecedenceTo(this.ResolvedVersion?.Version) != 0)
                {
                    displayed += "*";
                }
                return displayed;
            }
        }

        public string DisplayResolvedVersion
        {
            get
            {
                if (this.ResolvedVersion?.Version == null)
                {
                    return "---";
                }
                return this.ResolvedVersion.Version.ToString();
            }
        }
    }
}
