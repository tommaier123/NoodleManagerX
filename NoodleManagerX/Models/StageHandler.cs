using System;
using Newtonsoft.Json;
using DynamicData;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace NoodleManagerX.Models
{
    class StageHandler : GenericHandler
    {
        public override ItemType itemType { get; set; } = ItemType.Stage;

        public override string searchQuery { get; set; } = "{\"$or\":[{\"name\":{\"$contL\":\"<value>\"}},{\"user.username\":{\"$contL\":\"<value>\"}}]}";
        public override string apiEndpoint { get; set; } = "https://synthriderz.com/api/models/stages";

        public override void LoadLocalItems()
        {
            string directory = Path.Combine(MainViewModel.s_instance.settings.synthDirectory, "CustomStages");
            if (Directory.Exists(directory))
            {
                Task.Run(async () =>
                {
                    List<LocalItem> tmp = new List<LocalItem>();
                    foreach (string file in Directory.GetFiles(directory))
                    {
                        if (Path.GetExtension(file) == ".stage"|| Path.GetExtension(file) == ".spinstage")
                        {
                            tmp.Add(new LocalItem(-1, "", Path.GetFileName(file), ItemType.Stage));
                        }
                    }
                    MainViewModel.s_instance.localItems.Add(tmp);
                });
            }
        }

        public override dynamic DeserializePage(string json)
        {
            return JsonConvert.DeserializeObject<StagePage>(json);
        }

    }

    [DataContract]
    class StageItem : GenericItem
    {
        [DataMember] public string name { get; set; }
        [DataMember] public User user { get; set; }
        public override string target { get; set; } = "CustomStages";
        public override ItemType itemType { get; set; } = ItemType.Stage;
    }


    class StagePage : GenericPage
    {
        public List<StageItem> data;
    }
}

