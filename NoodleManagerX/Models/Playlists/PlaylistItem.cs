using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Models.Playlists
{
    [DataContract]
    public class PlaylistItem : GenericItem
    {
        [DataMember] public List<PlaylistEntryItem> items { get; set; }
        public string duration { get { return items != null ? items.Count().ToString() : "0"; } }

        public override string target { get; set; } = "CustomPlaylists";
        public override ItemType itemType { get; set; } = ItemType.Playlist;
    }

    [DataContract]
    public class PlaylistEntryItem
    {
        [DataMember] public int id { get; set; }
    }
}
