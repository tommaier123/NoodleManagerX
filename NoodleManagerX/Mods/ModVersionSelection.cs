using NoodleManagerX.Models.Mods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Mods
{
    public class ModVersionSelection
    {
        public string ModId { get; private set; }
        public ModVersion ModVersion { get; private set; }

        public ModVersionSelection(string modId, ModVersion modVersion)
        {
            ModId = modId;
            ModVersion = modVersion;
        }
    }
}
