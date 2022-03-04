using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace NoodleManagerX.Models
{
    class PlaylistHandler : GenericHandler
    {
        public override ItemType itemType { get; set; } = ItemType.Playlist;
        public override string join { get; set; } = "items||id";
        public override string apiEndpoint { get; set; } = "https://synthriderz.com/api/playlists";
        public override string folder { get; set; } = "Playlist";
        public override string[] extensions { get; set; } = { ".playlist" };

        public override dynamic DeserializePage(string json)
        {
            return JsonConvert.DeserializeObject<PlaylistPage>(json);
        }
    }

    [DataContract]
    class PlaylistItem : GenericItem
    {
        [DataMember] public List<PlaylistEntryItem> items { get; set; }
        public string duration { get { return items != null ? items.Count().ToString() : "0"; } }

        public override string target { get; set; } = "Playlist";
        public override ItemType itemType { get; set; } = ItemType.Playlist;
    }

    [DataContract]
    class PlaylistEntryItem
    {
        [DataMember] public int id { get; set; }
    }

#pragma warning disable 0649
    class PlaylistPage : GenericPage
    {
        public List<PlaylistItem> data;
    }
#pragma warning restore 0649
}

