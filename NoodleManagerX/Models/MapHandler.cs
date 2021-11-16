using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace NoodleManagerX.Models
{
    class MapHandler : GenericHandler
    {
        public override ItemType itemType { get; set; } = ItemType.Map;
        public override Dictionary<string, string> queryFields { get; set; } = new Dictionary<string, string>() { { "text_search", "$tsQuery" }, { "user.username", "$contL" } };
        public override string select { get; set; } = "title,artist,mapper,duration,difficulties,hash,youtube_url,beat_saber_convert,filename";
        public override string apiEndpoint { get; set; } = "https://synthriderz.com/api/beatmaps";

        public override async void LoadLocalItems()
        {
            List<LocalItem> tmp = new List<LocalItem>();
            if (MainViewModel.s_instance.questSerial == "")
            {
                if (MainViewModel.s_instance.settings.synthDirectory != "")
                {
                    string directory = Path.Combine(MainViewModel.s_instance.settings.synthDirectory, "CustomSongs");
                    if (Directory.Exists(directory))
                    {
                        foreach (string file in Directory.GetFiles(directory))
                        {
                            if (Path.GetExtension(file) == ".synth")
                            {
                                await GetLocalItem(file, tmp);
                            }
                        }
                    }
                }
            }
            else
            {
                var files = MainViewModel.QuestDirectoryGetFiles("CustomSongs").Where(x => x.TrimEnd().EndsWith(".synth"));
                foreach (string file in files)
                {
                    await base.GetLocalItem(file, tmp);
                }
            }
            MainViewModel.s_instance.localItems.AddRange(tmp);
        }

        public override async Task<bool> GetLocalItem(string file, List<LocalItem> list, GenericItem item = null)
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
                                LocalItem localItem = JsonConvert.DeserializeObject<LocalItem>(await sr.ReadToEndAsync());
                                localItem.filename = Path.GetFileName(file);
                                localItem.modifiedTime = File.GetLastWriteTime(file);
                                localItem.itemType = ItemType.Map;
                                list.Add(localItem);
                                return true;
                            }
                        }
                    }
                }

                //no metadata -> create
                if (item != null)
                {
                    MainViewModel.Log("Creating metadata for " + item.filename);

                    JObject metadata = new JObject(new JProperty("id", item.id), new JProperty("hash", ((MapItem)item).hash));

                    using (ZipArchive archive = ZipFile.Open(file, ZipArchiveMode.Update))
                    {
                        ZipArchiveEntry entry = archive.CreateEntry("synthriderz.meta.json");
                        using (StreamWriter writer = new StreamWriter(entry.Open()))
                        {
                            writer.Write(metadata.ToString(Formatting.None));
                        }
                        //archive.ExtractToDirectory(extractPath);
                    }
                    return true;
                }
                else
                {
                    MainViewModel.Log("Deleting file without metadata " + Path.GetFileName(file));
                    File.Delete(file);
                }
            }
            catch
            {
                MainViewModel.Log("Deleting corrupted file " + Path.GetFileName(file));
                File.Delete(file);
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
    }


    class MapPage : GenericPage
    {
        public List<MapItem> data;
    }
}
