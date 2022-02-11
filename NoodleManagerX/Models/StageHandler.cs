using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace NoodleManagerX.Models
{
    class StageHandler : GenericHandler
    {
        public override ItemType itemType { get; set; } = ItemType.Stage;
        public override string apiEndpoint { get; set; } = "https://synthriderz.com/api/models/stages";
        public override string folder { get; set; } = "CustomStages";
        public override string[] extensions { get; set; } = { ".stage", ".spinstage" };

        public override dynamic DeserializePage(string json)
        {
            return JsonConvert.DeserializeObject<StagePage>(json);
        }
    }

    [DataContract]
    class StageItem : GenericItem
    {
        public override string target { get; set; } = "CustomStages";
        public override ItemType itemType { get; set; } = ItemType.Stage;
    }


    class StagePage : GenericPage
    {
        public List<StageItem> data;
    }
}

