using NoodleManagerX.Models;
using NoodleManagerX.Models.Mods;
using Semver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Mods
{
    [DataContract]
    class ModItem : GenericItem
    {
        public override string target { get; set; } = "Mods";
        public override ItemType itemType { get; set; } = ItemType.Mod;
        
        public SemVersion SelectedVersion { get; private set; }
        public SemVersion ResolvedVersion { get; private set; }
        public List<string> LocalFiles { get; private set; }

        public ModItem(ModInfo modInfo, SemVersion selectedVersion, SemVersion resolvedVersion)
        {
            this.name = modInfo.Name;
            this.description = modInfo.Description;
            this.user = new User
            {
                username = modInfo.Author
            };
            this.SelectedVersion = selectedVersion;
            this.ResolvedVersion = resolvedVersion;
        }

        public override void LoadBitmap()
        {
            Console.WriteLine("No bitmaps needed for mod items");
        }

        public string DisplayedVersion
        {
            get
            {
                if (this.SelectedVersion == null)
                {
                    // Not installed
                    return "---";
                }

                var displayed = this.SelectedVersion.ToString();
                if (this.SelectedVersion.ComparePrecedenceTo(this.ResolvedVersion) != 0)
                {
                    displayed += "*";
                }
                return displayed;
            }
        }

        public string DisplayResolvedVersion
        {
            get
            {
                if (this.ResolvedVersion == null)
                {
                    return "---";
                }
                return this.ResolvedVersion.ToString();
            }
        }
    }
}
