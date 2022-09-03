using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Models.Mods
{
    public class ModInfo
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = "N/A";

        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; } = "N/A";

        [JsonProperty("author", Required = Required.Always)]
        public string Author { get; set; } = "N/A";

        [JsonProperty("description", Required = Required.Always)]
        public string Description { get; set; } = "";

        [JsonProperty("versions", Required = Required.Always)]
        public List<ModVersion> Versions { get; set; } = new List<ModVersion>();

        [JsonIgnore]
        public ModVersion ResolvedVersion = null;
    }
}
