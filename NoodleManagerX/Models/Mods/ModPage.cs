using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Models.Mods
{
    class ModPage : GenericPage
    {
        public List<ModVersion> Items { get; set; }
    }
}
