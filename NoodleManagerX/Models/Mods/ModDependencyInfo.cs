using Newtonsoft.Json;
using NoodleManagerX.Utils;
using Semver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Models.Mods
{
    public class ModDependencyInfo
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = "N/A";

        [JsonProperty("minVersion", Required = Required.Always)]
        [JsonConverter(typeof(SemVersionJsonConverter))]
        public SemVersion MinVersion { get; set; } = null;

        [JsonProperty("maxVersion", Required = Required.Default)]
        [JsonConverter(typeof(SemVersionJsonConverter))]
        public SemVersion MaxVersion { get; set; } = null;
    }
}
