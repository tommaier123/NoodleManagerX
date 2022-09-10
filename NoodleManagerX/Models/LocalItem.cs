using NoodleManagerX.Mods;
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
                    return hash == ((ModItem)item).ModInfo?.Id;
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
