using NoodleManagerX.Models.Mods;
using NoodleManagerX.Mods;
using Semver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Models
{
    public class LocalItem
    {
        public LocalItem(int id, string hash, string filename, DateTime modifiedTime, ItemType itemType)
        {
            this.id = id;
            this.hash = hash;
            this.filename = filename;
            this.modifiedTime = modifiedTime;
            this.itemType = itemType;
        }


        public int id = -1;
        public string hash = "";
        public string filename = "";
        public DateTime modifiedTime = new DateTime();
        public ItemType itemType = ItemType.init;

        public SemVersion ItemVersion
        {
            get
            {
                if (String.IsNullOrEmpty(filename))
                {
                    return null;
                }

                if (filename.StartsWith(hash + "_") && filename.EndsWith(".synthmod"))
                {
                    var versionStr = filename.Substring(hash.Length + 1, filename.Length - hash.Length - 1 - ".synthmod".Length);
                    try
                    {
                        return SemVersion.Parse(versionStr, SemVersionStyles.Any);
                    }
                    catch (Exception ex)
                    {
                        MainViewModel.Log($"Failed to parse version '{versionStr}' for mod {hash}");
                    }
                }
                return null;
            }
        }

        public bool CheckEquality(GenericItem item, bool checkHash = false)
        {
            if (item != null && itemType == item.itemType)
            {
                if (itemType == ItemType.Map && id != -1)
                {
                    return this.id == item.id && (!checkHash || hash == ((MapItem)item).hash);
                }
                else if (itemType == ItemType.Mod && id != -1)
                {
                    // id isn't set for mods, so always compare with hash (which is set to the modinfo id)
                    // Version is located within filename. Filename is always {modId}_{version}.synthmod
                    var modItem = (ModItem)item;
                    var isSameVersion = ItemVersion == null ||
                        modItem.InstalledVersion?.Version == null ||
                        ItemVersion.ComparePrecedenceTo(modItem.InstalledVersion?.Version) == 0;
                    return hash == modItem.ModInfo?.Id && isSameVersion;
                }
                else
                {
                    return filename == item.filename;
                }
            }
            return false;
        }
    }
}
