using DynamicData;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;

namespace NoodleManagerX.Models
{
    class MapHandler : GenericHandler
    {
        public override ItemType itemType { get; set; } = ItemType.Map;

        public override string allParameters { get; set; } = "\"title\":{\"$contL\":\"<value>\"}},{\"artist\":{\"$contL\":\"<value>\"}},{\"mapper\":{\"$contL\":\"<value>\"}";
        public override string select { get; set; } = "id,cover_url,download_url,published_at,title,artist,mapper,duration,difficulties,hash";
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
                                                    tmp.Add(localItem);
                                                }
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                    MainViewModel.Log("Deleting corrupted file " + Path.GetFileName(file));
                                    File.Delete(file);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                var files = MainViewModel.QuestDirectoryGetFiles("CustomSongs").Where(x=>x.TrimEnd().EndsWith(".synth"));
                foreach (string file in files)
                {
                    tmp.Add(new LocalItem(-1, "", file, new System.DateTime(), ItemType.Map));
                }
            }
            foreach (LocalItem item in tmp)
            {
                MainViewModel.s_instance.localItems.Add(item);
            }
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
        public override string target { get; set; } = "CustomSongs";
        public override ItemType itemType { get; set; } = ItemType.Map;
    }


    class MapPage : GenericPage
    {
        public List<MapItem> data;

        public override ItemType itemType { get; set; } = ItemType.Map;
    }
}
