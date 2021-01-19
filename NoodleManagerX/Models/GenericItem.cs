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
    abstract class GenericItem : ReactiveObject
    {
        [DataMember] public int id { get; set; }
        [DataMember] public string hash { get; set; }
        [DataMember] public string cover_url { get; set; }
        [DataMember] public string download_url { get; set; }
        [Reactive] public Bitmap cover_bmp { get; set; }
        [Reactive] public bool selected { get; set; }
        [Reactive] public bool downloading { get; set; } = false;
        [Reactive] public bool downloaded { get; set; }
        public virtual string target { get; set; }

        public WebClient webClient;

        public virtual ItemType itemType { get; set; }

        public ReactiveCommand<Unit, Unit> downloadCommand { get; set; }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            UpdateDownloaded();
            LoadBitmap();

            downloadCommand = ReactiveCommand.Create((() =>
            {
                Download();
            }));
        }

        public virtual void UpdateDownloaded()
        {

        }

        public void Download()
        {
            Task.Run(async () =>
            {
                try
                {

                    while (MainViewModel.GetDownloading() >= MainViewModel.downloadTasks)
                    {
                        if (MainViewModel.s_instance.closing) return;
                        await Task.Delay(10);
                    }

                    _ = Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        downloaded = false;
                        downloading = true;
                    });

                    Console.WriteLine("Downloading " + id);

                    webClient = new WebClient();
                    string url = "https://synthriderz.com" + download_url;

                    webClient.OpenRead(url);
                    string header_contentDisposition = webClient.ResponseHeaders["content-disposition"];
                    string filename = Path.Combine(MainViewModel.s_instance.settings.synthDirectory, target, new ContentDisposition(header_contentDisposition).FileName);

                    await webClient.DownloadFileTaskAsync(new Uri(url), filename);

                    webClient.Dispose();

                    if (File.Exists(filename))
                    {
                        _ = Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            downloaded = true;
                        });
                    }
                }
                catch (Exception e) { MainViewModel.Log(MethodBase.GetCurrentMethod(), e); }

                _ = Dispatcher.UIThread.InvokeAsync(() =>
                {
                    downloading = false;
                });

            });
        }

        public void LoadBitmap()
        {
            Task.Factory.StartNew(async () =>
            {
                using (WebClient client = new WebClient())
                using (Stream instream = await client.OpenReadTaskAsync(new Uri("https://synthriderz.com" + cover_url.ToString() + "?size=150")))
                using (MagickImage image = new MagickImage(instream))
                using (MemoryStream outstream = new MemoryStream())
                {
                    image.Format = MagickFormat.Bmp;
                    await image.WriteAsync(outstream);
                    outstream.Position = 0;
                    Bitmap tmp = new Bitmap(outstream);

                    _ = Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        cover_bmp = tmp;
                    });
                }
            }, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Current);//prefer fairness so that the first images are likely to be loaded first
        }
    }

    class LocalItem
    {
        public LocalItem(int _id, string _hash, string _filename, ItemType _itemType)
        {
            id = _id;
            hash = _hash;
            filename = _filename;
            itemType = _itemType;
        }

        public int id = -1;
        public string hash = "";
        public string filename = "";
        public ItemType itemType = ItemType.init;
    }

    abstract class GenericPage
    {
        public int count = -1;
        public int total = -1;
        public int page = -1;
        public int pagecount = -1;
    }

    enum ItemType
    {
        init,
        Map,
        Playlist,
        Stage,
        Avatar,
        Mod
    }
}
