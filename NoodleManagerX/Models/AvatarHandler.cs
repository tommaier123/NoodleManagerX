using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace NoodleManagerX.Models
{
    class AvatarHandler : GenericHandler
    {
        public override ItemType itemType { get; set; } = ItemType.Avatar;
        public override string apiEndpoint { get; set; } = "https://synthriderz.com/api/models/avatars";
        public override string folder { get; set; } = "Avatars";
        public override string[] extensions { get; set; } = { ".vrm" };

        public override dynamic DeserializePage(string json)
        {
            return JsonConvert.DeserializeObject<AvatarPage>(json);
        }
    }

    [DataContract]
    class AvatarItem : GenericItem
    {
        public override string target { get; set; } = "Avatars";
        public override ItemType itemType { get; set; } = ItemType.Avatar;
    }

#pragma warning disable 0649
    class AvatarPage : GenericPage
    {
        public List<AvatarItem> data;
    }
#pragma warning restore 0649
}

