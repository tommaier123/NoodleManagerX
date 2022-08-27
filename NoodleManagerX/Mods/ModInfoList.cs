using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Mods
{
    public class ModInfoList
    {
        [JsonProperty("availableMods")]
        public List<ModVersion> AvailableMods { get; set; }
    }
}
