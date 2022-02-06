using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace NoodleManagerX.Models
{
    class MapHandler : GenericHandler
    {
        public override ItemType itemType { get; set; } = ItemType.Map;
        public override Dictionary<string, string> queryFields { get; set; } = new Dictionary<string, string>() { { "text_search", "$tsQuery" }, { "user.username", "$contL" } };
        public override string select { get; set; } = "title,artist,mapper,duration,difficulties,hash,youtube_url,beat_saber_convert";
        public override string selectDownload { get; set; } = "hash,beat_saber_convert";
        public override string apiEndpoint { get; set; } = "https://synthriderz.com/api/beatmaps";

        public override async Task<bool> LoadLocalItems()
        {
            List<LocalItem> tmp = new List<LocalItem>();
            if (MainViewModel.s_instance.settings.synthDirectory != "")
            {
                if (StorageAbstraction.DirectoryExists("CustomSongs"))
                {
                    foreach (string file in StorageAbstraction.GetFilesInDirectory("CustomSongs"))
                    {
                        string path = Path.Combine("CustomSongs", Path.GetFileName(file));

                        if (Path.GetExtension(path) == ".synth")
                        {
                            await GetLocalItem(path, tmp);
                        }
                    }
                }
            }
            MainViewModel.s_instance.localItems.AddRange(tmp);
            return true;
        }

        public override async Task<bool> GetLocalItem(string path, List<LocalItem> list)
        {
            try
            {
                using (Stream stream = StorageAbstraction.ReadFile(path))
                using (ZipArchive archive = new ZipArchive(stream))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName == "synthriderz.meta.json")
                        {
                            using (StreamReader sr = new StreamReader(entry.Open()))
                            {
                                LocalItem localItem = JsonConvert.DeserializeObject<LocalItem>(await sr.ReadToEndAsync());
                                localItem.filename = Path.GetFileName(path);
                                //localItem.modifiedTime = File.GetLastWriteTime(path);
                                localItem.itemType = ItemType.Map;
                                list.Add(localItem);
                                return true;
                            }
                        }
                    }

                    if (MainViewModel.s_instance.pruning)
                    {
                        MainViewModel.Log("Deleting old file without metadata " + Path.GetFileName(path));
                        StorageAbstraction.DeleteFile(path);
                    }
                }
            }
            catch (Exception e)
            {
                MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
                MainViewModel.Log("Deleting corrupted file " + Path.GetFileName(path));
                StorageAbstraction.DeleteFile(path);
            }

            return false;
        }

        public override dynamic DeserializePage(string json)
        {
            return JsonConvert.DeserializeObject<MapPage>(json);
        }
    }

    [DataContract]
    class MapItem : GenericItem
    {
        [DataMember] public string title { get; set; }
        [DataMember] public string artist { get; set; }
        [DataMember] public string mapper { get; set; }
        [DataMember] public string duration { get; set; }
        [DataMember] public string[] difficulties { get; set; }
        [DataMember] public string hash { get; set; }
        [DataMember] public string youtube_url { get; set; }
        [Reactive] public bool playing { get; set; } = false;
        public ReactiveCommand<Unit, Unit> playPreviewCommand { get; set; }

        public override string display_title
        {
            get { return title; }
        }
        public override string display_creator
        {
            get { return mapper; }
        }
        public override string display_preview
        {
            get { return youtube_url; }
        }
        public override string[] display_difficulties
        {
            get { return difficulties; }
        }
        public override string target { get; set; } = "CustomSongs";
        public override ItemType itemType { get; set; } = ItemType.Map;

        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            playPreviewCommand = ReactiveCommand.Create((() =>
            {
                PlaybackHandler.Play(this);
            }));
        }

        public override MemoryStream FixMetadata(Stream stream)
        {
            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);

            using (ZipArchive archive = new ZipArchive(ms, ZipArchiveMode.Update, true))
            {
                foreach (ZipArchiveEntry zipEntry in archive.Entries)
                {
                    if (zipEntry.FullName == "synthriderz.meta.json")
                    {
                        return ms;
                    }
                }

                MainViewModel.Log("Creating metadata for " + filename);
                JObject metadata = new JObject(new JProperty("id", id), new JProperty("hash", hash));

                ZipArchiveEntry entry = archive.CreateEntry("synthriderz.meta.json");
                using (StreamWriter writer = new StreamWriter(entry.Open()))
                {
                    writer.Write(metadata.ToString(Formatting.None));
                }

                return ms;
            }
        }
    }


    class MapPage : GenericPage
    {
        public List<MapItem> data;
    }
}
