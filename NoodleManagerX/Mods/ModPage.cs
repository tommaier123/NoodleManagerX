using NoodleManagerX.Models;
using NoodleManagerX.Models.Mods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Mods
{
    class ModPage : GenericPage
    {
        public List<ModInfo> Mods { get; set; }
    }
}
