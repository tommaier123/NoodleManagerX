using Newtonsoft.Json;
using NoodleManagerX.Models.Playlists;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace NoodleManagerX.Models.Stages
{
    class StageHandler : GenericHandler
    {
        public override ItemType itemType { get; set; } = ItemType.Stage;
        public override string apiEndpoint { get; set; } = "https://synthriderz.com/api/models/stages";
        public override string folder { get; set; } = "CustomStages";
        public override string join { get; set; } = "files&join=files.file&join=experience_beatmap&join=id";
        public override string[] extensions { get; set; } = { ".stage", ".stagedroid"};

        public override Task LoadLocalItems()
        {
            return base.LoadLocalItems();
        }

        public override dynamic DeserializePage(string json)
        {
            return JsonConvert.DeserializeObject<StagePage>(json);
        }
    }

#pragma warning disable 0649
    class StagePage : GenericPage
    {
        public List<StageItem> data;
    }
#pragma warning restore 0649
}

