using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NoodleManagerX.Models
{
    [DataContract]
    class Map : ReactiveObject
    {

        [DataMember] public string title { get; set; }
        [DataMember] public string artist { get; set; }
        [DataMember] public string mapper { get; set; }
        [DataMember] public string duration { get; set; }
        [DataMember] public string[] difficulties { get; set; }
        [DataMember] public string cover_url { get; set; }

        [Reactive] public Bitmap cover_bmp { get; set; }
        [Reactive] public bool selected { get; set; }
        [Reactive] public bool downloaded { get; set; } = true;

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            LoadBitmap();
        }

        public void LoadBitmap()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            MemoryStream outstream = new MemoryStream();

            Task.Factory.StartNew(async () =>
            {
                using (WebClient client = new WebClient())
                using (Stream instream = await client.OpenReadTaskAsync(new Uri("https://synthriderz.com" + cover_url.ToString() + "?size=150")))
                {
                    System.Drawing.Image image = System.Drawing.Image.FromStream(instream);
                    image.Save(outstream, System.Drawing.Imaging.ImageFormat.Bmp);
                    outstream.Position = 0;
                }
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    cover_bmp = new Bitmap(outstream);
                });
            }, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Current);//prefer fairness so that the first images are likely to be loaded first
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
