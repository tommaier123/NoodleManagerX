using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Mods
{
    public class ModDependencyInfo
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = "N/A";

        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; } = "N/A";

        [JsonProperty("author", Required = Required.Always)]
        public string Author { get; set; } = "N/A";

        [JsonProperty("minVersion", Required = Required.Always)]
        public string MinVersion{ get; set; } = "1.0.0.0";

        [JsonProperty("maxVersion", Required = Required.Default)]
        public string MaxVersion { get; set; } = null;
    }
}
