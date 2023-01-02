using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Models.Playlists
{
    [Serializable]
    public struct PlaylistMap
    {
        public string hash;

        public string name;

        public string author;

        public string beatmapper;

        public int difficulty;

        public float trackDuration;

        public long addedTime;
    }
}
