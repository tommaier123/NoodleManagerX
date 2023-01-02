using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NoodleManagerX.Models.Playlists
{
    /// <summary>
    /// This is the PlaylistItem used in Synth Riders, saved to .playlist file
    /// </summary>
    [Serializable]
    public struct PlaylistFile
    {
        [JsonProperty("dataString")]
        public List<PlaylistMap> Songs;

        public int SelectedIconIndex;

        public int SelectedTexture;

        [JsonProperty("namePlaylist")]
        public string Name;

        [JsonProperty("description")]
        public string Description;

        [JsonProperty("gradientTop")]
        public string GradientTop;

        [JsonProperty("gradientDown")]
        public string GradientDown;

        [JsonProperty("colorTitle")]
        public string TitleColorString;

        [JsonProperty("colorTexture")]
        public string TextureColorString;

        [JsonProperty("creationDate")]
        public string CreationDate;

        public bool IsEqual(PlaylistFile target)
        {
            if (Songs == null || target.Songs == null || Songs.Count != target.Songs.Count)
            {
                return false;
            }

            if (Name.Equals(target.Name))
            {
                return Songs.SequenceEqual(target.Songs);
            }

            return false;
        }
    }
}
