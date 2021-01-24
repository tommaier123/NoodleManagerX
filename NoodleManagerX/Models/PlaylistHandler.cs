using System;
using Newtonsoft.Json;
using DynamicData;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace NoodleManagerX.Models
{
    class PlaylistHandler : GenericHandler
    {
        public override ItemType itemType { get; set; } = ItemType.Playlist;

        public override string allParameters { get; set; } = "\"name\":{\"$contL\":\"<value>\"}},{\"user.username\":{\"$contL\":\"<value>\"}";
        public override string select { get; set; } = "id,cover_url,download_url,published_at,name,user";
        public override string apiEndpoint { get; set; } = "https://synthriderz.com/api/playlists";

        public override void LoadLocalItems()
        {
            string directory = Path.Combine(MainViewModel.s_instance.settings.synthDirectory, "Playlist");
            if (Directory.Exists(directory))
            {
                Task.Run(() =>
                {
                    List<LocalItem> tmp = new List<LocalItem>();
                    foreach (string file in Directory.GetFiles(directory))
                    {
                        if (Path.GetExtension(file) == ".playlist")
                        {
                            tmp.Add(new LocalItem(-1, "", Path.GetFileName(file), File.GetLastWriteTime(file), ItemType.Playlist));
                        }
                    }
                    foreach (LocalItem item in tmp)
                    {
                        MainViewModel.s_instance.localItems.Add(item);
                    }
                });
            }
        }

        public override dynamic DeserializePage(string json)
        {
            return JsonConvert.DeserializeObject<PlaylistPage>(json);
        }

    }

    [DataContract]
    class PlaylistItem : GenericItem
    {
        [DataMember] public string name { get; set; }
        [DataMember] public User user { get; set; }
        public override string target { get; set; } = "Playlist";
        public override ItemType itemType { get; set; } = ItemType.Playlist;
    }


    class PlaylistPage : GenericPage
    {
        public List<PlaylistItem> data;
    }
}

