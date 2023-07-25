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
using NoodleManagerX.Models.Mods;
using System.Net.Http;
using Newtonsoft.Json;
using Avalonia.Threading;

namespace NoodleManagerX.Models.Stages
{
    [DataContract]
    class StageItem : GenericItem
    {
        public override string target { get; set; } = "CustomStages";
        public override ItemType itemType { get; set; } = ItemType.Stage;

        [DataMember]
        [JsonProperty("files")]
        public List<StageFileInfo> Files { get; set; } = new List<StageFileInfo>();

        [DataMember]
        [JsonProperty("experience_beatmap")]
        public MapItem ExperienceBeatmap = null;

        public List<StageFile> GetDownloadFilesForCurrentDevice()
        {
            return Files
                .Where(file => MtpDevice.connected ? file.IsQuest() : file.IsPc())
                .Select(file => file.File)
                .ToList();
        }

        public override void Delete()
        {
            bool deletedAll = true;
            var deviceFiles = GetDownloadFilesForCurrentDevice();
            deviceFiles.ForEach(stageFile => deletedAll = Delete(stageFile.Filename) && deletedAll);
            if (deletedAll)
            {
                _ = Dispatcher.UIThread.InvokeAsync(() =>
                {
                    downloaded = false;
                });
            }
        }

        public override async Task UpdateDownloaded(bool forceUpdate = false)
        {
            var numDownloaded = 0;
            var expectedFiles = GetDownloadFilesForCurrentDevice();
            foreach (var file in expectedFiles)
            {
                List<LocalItem> matchingLocalItems = MainViewModel.s_instance.localItems
                    .Where(x => x != null && x.filename == file.Filename)
                    .ToList();
                matchingLocalItems.Sort();

                if (matchingLocalItems.Count() > 0)
                {
                    if (matchingLocalItems.Count() > 1)
                    {
                        matchingLocalItems = matchingLocalItems.Where(x => x.itemType == itemType).OrderByDescending(x => x.modifiedTime).ToList();
                        foreach (LocalItem l in matchingLocalItems.Skip(1))
                        {
                            MainViewModel.Log("Old version deleted of " + l.filename);
                            _ = Delete(l.filename);
                        }
                    }

                    numDownloaded++;

                    filename = matchingLocalItems[0].filename;
                    needsUpdate = DateTime.Compare(updatedAt, matchingLocalItems[0].modifiedTime) > 0;
                    if (needsUpdate) MainViewModel.Log("Date difference detected for " + this.filename);
                }
            }

            if (expectedFiles.Count == 0)
            {
                visible = false;
            }
            if (numDownloaded == expectedFiles.Count)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    downloaded = true;
                });
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    downloaded = false;
                    needsUpdate = false;
                });
            }

            if (needsUpdate || forceUpdate)
            {
                DownloadScheduler.Download(this);
            }
        }

        public override void UpdateBlacklisted()
        {
            var platformFiles = GetDownloadFilesForCurrentDevice();
            foreach (var file in platformFiles)
            {
                if (!String.IsNullOrEmpty(file.Filename) && MainViewModel.s_instance.blacklist.Contains(file.Filename))
                {
                    _ = Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        blacklisted = true;
                    });
                    return;
                }
            }

            // Not found in blacklist
            _ = Dispatcher.UIThread.InvokeAsync(() =>
            {
                blacklisted = false;
            });
        }

        protected async override Task<string> RawDownloadAndSave()
        {
            var path = "";

            // Try to download all modes of this stage.
            // Use the first mode in the list as the identifier (path)

            var downloadFiles = GetDownloadFilesForCurrentDevice();
            foreach (StageFile stageFile in downloadFiles)
            {
                try
                {
                    string url = stageFile.Url;
                    if (url == null)
                    {
                        url = "https://synthriderz.com" + download_url + "?file_id=" + stageFile.Id;
                    }

                    using HttpClient client = new HttpClient();
                    using var rawResponse = await client.GetStreamAsync(url);
                    using MemoryStream str = await CopyStreamToMemoryStream(rawResponse);
                    if (String.IsNullOrEmpty(filename))
                    {
                        filename = stageFile.Filename;
                    }

                    path = Path.Combine(target, stageFile.Filename);
                    await StorageAbstraction.WriteFile(str, path);
                }
                catch (Exception e)
                {
                    MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
                }
            }

            return path;
        }
    }
}
