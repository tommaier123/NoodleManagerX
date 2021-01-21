using System;
using Newtonsoft.Json;
using DynamicData;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace NoodleManagerX.Models
{
    class AvatarHandler : GenericHandler
    {
        public override ItemType itemType { get; set; } = ItemType.Avatar;

        public override string searchQuery { get; set; } = "{\"$or\":[{\"name\":{\"$contL\":\"<value>\"}},{\"user.username\":{\"$contL\":\"<value>\"}}]}";
        public override string apiEndpoint { get; set; } = "https://synthriderz.com/api/models/avatars";

        public override void LoadLocalItems()
        {
            string directory = Path.Combine(MainViewModel.s_instance.settings.synthDirectory, "Avatars");
            if (Directory.Exists(directory))
            {
                Task.Run(async () =>
                {
                    List<LocalItem> tmp = new List<LocalItem>();
                    foreach (string file in Directory.GetFiles(directory))
                    {
                        if (Path.GetExtension(file) == ".vrm")
                        {
                            tmp.Add(new LocalItem(-1, "", Path.GetFileName(file), ItemType.Avatar));
                        }
                    }
                    MainViewModel.s_instance.localItems.Add(tmp);
                });
            }
        }

        public override dynamic DeserializePage(string json)
        {
            return JsonConvert.DeserializeObject<AvatarPage>(json);
        }

    }

    [DataContract]
    class AvatarItem : GenericItem
    {
        [DataMember] public string name { get; set; }
        [DataMember] public User user { get; set; }
        public override string target { get; set; } = "Avatars";
        public override ItemType itemType { get; set; } = ItemType.Avatar;
    }


    class AvatarPage : GenericPage
    {
        public List<AvatarItem> data;
    }
}

