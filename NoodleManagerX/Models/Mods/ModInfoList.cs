using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Models.Mods
{
    class ModInfoList
    {
        [JsonProperty("availableMods")]
        public List<ModVersion> AvailableMods { get; set; }
    }
}
