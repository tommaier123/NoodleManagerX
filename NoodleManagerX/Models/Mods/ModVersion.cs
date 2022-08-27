using Newtonsoft.Json;
using NoodleManagerX.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Models.Mods
{
    class ModVersion
    {
        [JsonProperty("name", Required=Required.Always)]
        public string Name { get; set; } = "N/A";

        [JsonProperty("author", Required = Required.Always)]
        public string Author { get; set; } = "N/A";
        
        [JsonProperty("description", Required = Required.Always)]
        public string Description { get; set; } = "";
        
        [JsonProperty("version", Required = Required.Always)]
        public string Version { get; set; } = "";

        [JsonProperty("downloadUrl", Required = Required.Always)]
        public string DownloadUrl { get; set; } = "";

        [JsonProperty("dependencies", Required=Required.Always)]
        public List<ModDependencyInfo> Dependencies { get; set; } = new List<ModDependencyInfo>();
    }
}
