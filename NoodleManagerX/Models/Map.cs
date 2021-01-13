using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Reactive;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NoodleManagerX.Models
{
    [DataContract]
    class Map : GenericItem
    {
        [DataMember] public string title { get; set; }
        [DataMember] public string artist { get; set; }
        [DataMember] public string mapper { get; set; }
        [DataMember] public string duration { get; set; }
        [DataMember] public string[] difficulties { get; set; }
        [DataMember] public string filename_original { get; set; }
        public override string target { get; set; } = "CustomSongs";

        public override void UpdateDownloaded()
        {
            _ = Dispatcher.UIThread.InvokeAsync(() =>
            {
                downloaded = MainViewModel.s_instance.localItems.Select(x => x.id).Contains(id);
                if (!downloaded)
                {
                    //Console.WriteLine("updated " + title +" not downloaded "+id);
                }
            });
        }
    }


    class MapPage:Page
    {
        public List<Map> data;
    }
}
