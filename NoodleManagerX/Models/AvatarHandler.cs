using System;
using Newtonsoft.Json;
using DynamicData;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Collections.Concurrent;

namespace NoodleManagerX.Models
{
    class AvatarHandler : GenericHandler
    {
        public override ItemType itemType { get; set; } = ItemType.Avatar;
        public override string apiEndpoint { get; set; } = "https://synthriderz.com/api/models/avatars";
        public override string folder { get; set; } = "Avatars";
        public override string[] extensions { get; set; } = { ".vrm"};

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
        public override string display_title
        {
            get { return name; }
        }
        public override string display_creator
        {
            get { return user.username; }
        }

        public override string target { get; set; } = "Avatars";
        public override ItemType itemType { get; set; } = ItemType.Avatar;
    }


    class AvatarPage : GenericPage
    {
        public List<AvatarItem> data;
    }
}

