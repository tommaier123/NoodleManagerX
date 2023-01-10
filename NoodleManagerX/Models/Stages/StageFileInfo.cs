using Newtonsoft.Json;
using NoodleManagerX.Models.Playlists;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Models.Stages
{
    [Serializable]
    public class StageFileInfo
    {
        [DataMember]
        [JsonProperty("id")]
        public int Id;

        /// <summary>
        /// Can be "normal", "spin" or "experience"
        /// </summary>
        [DataMember]
        [JsonProperty("mode")]
        public string Mode;

        /// <summary>
        /// Can be "pc" or "quest"
        /// </summary>
        [DataMember]
        [JsonProperty("platform")]
        public string Platform;

        [DataMember]
        [JsonProperty("type")]
        public string StageType;

        [DataMember]
        [JsonProperty("version")]
        public int Version;

        [DataMember]
        [JsonProperty("file")]
        public StageFile File;

        public bool IsPc()
        {
            return Platform == "pc";
        }

        public bool IsQuest()
        {
            return Platform == "quest";
        }

        public bool IsNormalStage()
        {
            return Mode == "normal";
        }

        public bool IsSpinStage()
        {
            return Mode == "spin";
        }

        public bool IsExperienceStage()
        {
            return Mode == "experience";
        }
    }
}
