using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;

namespace NoodleManagerX.Models
{
    [DataContract]
    class Map : ReactiveObject
    {

        [DataMember] public string title { get; set; }
        [DataMember] public string artist { get; set; }
        [DataMember] public string mapper { get; set; }
        [DataMember] public string cover_url { get; set; }

        [Reactive] public Bitmap cover_bmp { get; set; }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            LoadBitmap();
        }

        public void LoadBitmap()//todo starting 50 threads might be a bad idea, try tasks instead
        {
            MemoryStream outstream = new MemoryStream();

            Thread th = new Thread(() =>
            {
                using (WebClient client = new WebClient())
                using (Stream instream = client.OpenRead("https://synthriderz.com" + cover_url.ToString() + "?size=150"))
                {
                    System.Drawing.Image image = System.Drawing.Image.FromStream(instream);
                    image.Save(outstream, System.Drawing.Imaging.ImageFormat.Bmp);
                    outstream.Position = 0;

                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        cover_bmp = new Bitmap(outstream);
                    });
                }
            });
            th.Start();
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
