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


        public ModItem(ModInfo modInfo, ModVersion selectedVersion, ModVersion resolvedVersion)
        {
            this.ModInfo = modInfo;
            this.SelectedVersion = selectedVersion;
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
            }
            catch (Exception e)
            {
                MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
            }

            return path;
        }

        public string DisplaySelectedVersion
        {
            get
            {
                if (this.SelectedVersion?.Version == null)
                {
                    // Not installed
                    return "---";
                }

                var displayed = this.SelectedVersion.Version.ToString();
                if (this.SelectedVersion.Version.ComparePrecedenceTo(this.ResolvedVersion?.Version) != 0)
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
