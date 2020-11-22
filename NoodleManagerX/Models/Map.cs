using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NoodleManagerX.Models
{
    [DataContract]
    class Map : ReactiveObject
    {
        [DataMember] public int id { get; set; }
        [DataMember] public string hash { get; set; }
        [DataMember] public string title { get; set; }
        [DataMember] public string artist { get; set; }
        [DataMember] public string mapper { get; set; }
        [DataMember] public string duration { get; set; }
        [DataMember] public string[] difficulties { get; set; }
        [DataMember] public string cover_url { get; set; }
        [DataMember] public string filename_original { get; set; }
        [Reactive] public Bitmap cover_bmp { get; set; }
        [Reactive] public bool selected { get; set; }
        [Reactive] public bool downloaded { get; set; }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            LoadBitmap();
            UpdateDownloaded();
        }

        public void UpdateDownloaded()
        {
            downloaded = MainViewModel.s_instance.localMaps.Select(x => x.id).Contains(id);
        }

        public void LoadBitmap()
        {
            Task.Factory.StartNew(async () =>
            {
                MemoryStream outstream = new MemoryStream();
                using (WebClient client = new WebClient())
                using (Stream instream = await client.OpenReadTaskAsync(new Uri("https://synthriderz.com" + cover_url.ToString() + "?size=150")))
                {
                    System.Drawing.Image image = System.Drawing.Image.FromStream(instream);
                    image.Save(outstream, System.Drawing.Imaging.ImageFormat.Bmp);
                    outstream.Position = 0;
                }
                _ = Dispatcher.UIThread.InvokeAsync(() =>
                  {
                      cover_bmp = new Bitmap(outstream);
                  });
            }, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Current);//prefer fairness so that the first images are likely to be loaded first
        }
    }

    class MapPage
    {
        public List<Map> data;
        public int count = -1;
        public int total = -1;
        public int page = -1;
        public int pagecount = -1;
    }

    class LocalMap
    {
        public int id = -1;
        public string hash = "";
        public string filename = "";
    }
}
