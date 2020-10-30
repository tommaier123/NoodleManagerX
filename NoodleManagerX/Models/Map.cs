using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace NoodleManagerX.Models
{
    [DataContract]
    class Map : ReactiveObject
    {

        [DataMember] [Reactive] public string title { get; set; }
        [DataMember] [Reactive] public string artist { get; set; }
        [DataMember] [Reactive] public string mapper { get; set; }
        [DataMember] [Reactive] public string cover_url { get; set; }//+"?size=150"

        public Map(string _title)
        {
            title = _title;
        }
    }

    class MapPage
    {
        public ObservableCollection<Map> data;
        public int count = -1;
        public int total = -1;
        public int page = -1;
        public int pagecount = -1;
    }
}
