using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Models.Stages
{
    [Serializable]
    public struct StageFile
    {
        [DataMember]
        [JsonProperty("id")]
        public int Id;

        [DataMember]
        [JsonProperty("filename")]
        public string Filename;

        [DataMember]
        [JsonProperty("extension")]
        public string Extension;

        [DataMember]
        [JsonProperty("filesize")]
        public int Size;

        [DataMember]
        [JsonProperty("visibility")]
        public string Visibility;

        [DataMember]
        [JsonProperty("cdn_url")]
        public string Url;

        [DataMember]
        [JsonProperty("download_count")]
        public int DownloadCount;

        [DataMember]
        [JsonProperty("created_at")]
        public DateTime CreatedAt;

    }
}
