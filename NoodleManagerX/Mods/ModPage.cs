using NoodleManagerX.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Mods
{
    class ModPage : GenericPage
    {
        public List<ModVersion> Items { get; set; }
    }
}
