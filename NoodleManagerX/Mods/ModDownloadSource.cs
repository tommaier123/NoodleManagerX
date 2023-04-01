using NoodleManagerX.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Mods
{
    class ModDownloadSource
    {
        public static string GetModItemBaseDownloadUrl()
        {
            return $"{GetModsBaseUrl()}/Downloads";
        }

        public static string GetModsBaseUrl()
        {
            return $"https://raw.githubusercontent.com/bookdude13/SRModsList/{GetModListBranch()}/SynthRiders";
        }

        private static string GetModListBranch()
        {
            var usingBetas = MainViewModel.s_instance?.settings?.getBetas ?? false;
            return usingBetas ? "dev" : "master";
        }
    }
}
