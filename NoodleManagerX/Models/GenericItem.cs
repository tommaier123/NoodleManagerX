using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        [DataMember] public string cover_url { get; set; }
        [DataMember] public string download_url { get; set; }
        [DataMember] public string published_at { get; set; }

        [Reactive] public Bitmap cover_bmp { get; set; }
        [Reactive] public bool selected { get; set; }
        [Reactive] public bool downloading { get; set; } = false;
        [Reactive] public bool downloaded { get; set; } = false;
        [Reactive] public bool needsUpdate { get; set; } = false;
        public string filename { get; set; } = "";
        public DateTime updatedAt { get; set; }
        public virtual string target { get; set; }

        public virtual ItemType itemType { get; set; }

        public ReactiveCommand<Unit, Unit> downloadCommand { get; set; }


        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            UpdateDownloaded();
            LoadBitmap();

            Task.Run(() => updatedAt = DateTime.Parse(published_at, null, System.Globalization.DateTimeStyles.RoundtripKind));

            downloadCommand = ReactiveCommand.Create((() =>
            {
                Download();
            }));
        }

        public void UpdateDownloaded()
        {
            List<LocalItem> tmp = MainViewModel.s_instance.localItems.Where(x => x != null && x.CheckEquality(this)).ToList();

            if (tmp.Count() > 0)
            {
                _ = Dispatcher.UIThread.InvokeAsync(() =>
                {
                    downloaded = true;
                    if (itemType == ItemType.Map && !string.IsNullOrEmpty(tmp[0].hash))
                    {
                        needsUpdate = tmp[0].hash == ((MapItem)this).hash;
                    }
                    else
                    {
                        needsUpdate = DateTime.Compare(updatedAt, tmp[0].modifiedTime) > 0;
                    }
                });
            }

        }

        public void Download()
        {
            Task.Run(async () =>
            {
                try
                {
                    _ = Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        downloaded = false;
                        downloading = true;
                    });

                    WebClient webClient = new WebClient();
                    string url = "https://synthriderz.com" + download_url;

                    //remove once filename is in the api!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                    webClient.OpenRead(url);
                    string header_contentDisposition = webClient.ResponseHeaders["content-disposition"];
                    filename = new ContentDisposition(header_contentDisposition).FileName;

                    string filepath = Path.Combine(MainViewModel.s_instance.settings.synthDirectory, target, filename);

                    await webClient.DownloadFileTaskAsync(new Uri(url), filepath);

                    webClient.Dispose();

                    if (File.Exists(filepath))
                    {
                        _ = Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            downloaded = true;
                        });
                    }
                    else
                    {
                        MainViewModel.Log("Download failed " + id);
                        DownloadScheduler.queue.Add(this);
                    }
                }
                catch (Exception e)
                {
                    DownloadScheduler.queue.Add(this);
                    MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
                }

                _ = Dispatcher.UIThread.InvokeAsync(() =>
                {
                    downloading = false;
                });

                DownloadScheduler.downloading.Remove(this);
            });
        }

        public void LoadBitmap()
        {
            Task.Factory.StartNew(async () =>
            {
                if (string.IsNullOrEmpty(cover_url))
                {
                    cover_url = "/img/andromeda-gradient-128.png";
                }

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
        public LocalItem(int id, string hash, string filename, DateTime modifiedTime, ItemType itemType)
        {
            this.id = id;
            this.hash = hash;
            this.filename = filename;
            this.modifiedTime = modifiedTime;
            this.itemType = itemType;
        }

        public int id = -1;
        public string hash = "";
        public string filename = "";
        public DateTime modifiedTime = new DateTime();
        public ItemType itemType = ItemType.init;

        public bool CheckEquality(GenericItem item, bool checkHash = false)
        {
            if (item != null)
            {
                if (itemType == item.itemType && (itemType == ItemType.Map))
                {
                    return this.id == item.id && (!checkHash || hash == ((MapItem)item).hash);
                }
                else
                {
                    return filename == item.filename;
                }
            }
            return false;
        }
    }

    abstract class GenericPage
    {
        public int count = -1;
        public int total = -1;
        public int page = -1;
        public int pagecount = -1;

        public virtual ItemType itemType { get; set; }
    }

    [DataContract]
    class User : ReactiveObject
    {
        [DataMember] public int id { get; set; }
        [DataMember] public string username { get; set; }
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
