using Newtonsoft.Json;
using NoodleManagerX.Models;
using NoodleManagerX.Utils;
using Semver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Models.Mods
{
    public class ModVersion
    {
        [JsonProperty("version", Required = Required.Always)]
        [JsonConverter(typeof(SemVersionJsonConverter))]
        public SemVersion Version { get; set; } = null;

        [JsonProperty("downloadUrl", Required = Required.Always)]
        public string DownloadUrl { get; set; } = "";

        [JsonProperty("dependencies", Required = Required.Always)]
        public List<ModDependencyInfo> Dependencies { get; set; } = new List<ModDependencyInfo>();

        // TODO make this nested. Assumes max depth of 1 for dependencies right now
        public bool HasDependency(string dependencyModId)
        {
            foreach (var dependency in this.Dependencies)
            {
                if (dependencyModId == dependency.Id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
