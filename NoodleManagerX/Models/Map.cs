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
        [DataMember] public string download_url { get; set; }
        [DataMember] public string filename_original { get; set; }
        [Reactive] public Bitmap cover_bmp { get; set; }
        [Reactive] public bool selected { get; set; }
        [Reactive] public bool downloaded { get; set; }

        public WebClient webClient;

        public ReactiveCommand<Unit, Unit> downloadMapCommand { get; set; }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            LoadBitmap();
            UpdateDownloaded();

            downloadMapCommand = ReactiveCommand.Create((() =>
            {
                Download();
            }));
        }

        public void UpdateDownloaded()
        {
            _ = Dispatcher.UIThread.InvokeAsync(() =>
            {
                downloaded = MainViewModel.s_instance.localMaps.Select(x => x.id).Contains(id) || MainViewModel.s_instance.downloadingMaps.Contains(this);
            });
        }

        public void Download()
        {
            if (!downloaded)
            {
                Task.Run(async () =>
                {
                    MainViewModel.s_instance.downloadingMaps.Add(this);

                    try
                    {
                        _ = Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            downloaded = true;
                        });

                        if (!MainViewModel.s_instance.closing)
                        {
                            webClient = new WebClient();
                            string url = "https://synthriderz.com" + download_url;

                            webClient.OpenRead(url);
                            string header_contentDisposition = webClient.ResponseHeaders["content-disposition"];
                            string filename = Path.Combine(MainViewModel.s_instance.settings.synthDirectory, "CustomSongs", new ContentDisposition(header_contentDisposition).FileName);

                            Console.WriteLine("Downloading " + title);
                            await webClient.DownloadFileTaskAsync(new Uri(url), filename);

                            webClient.Dispose();

                            if (File.Exists(filename))
                            {
                                MainViewModel.s_instance.localMaps.Add(new LocalMap(id, hash, filename));
                            }
                        }
                    }
                    catch (Exception e) { MainViewModel.Log(MethodBase.GetCurrentMethod(), e); }

                    MainViewModel.s_instance.downloadingMaps.Remove(this);

                    _ = Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        this.UpdateDownloaded();
                    });
                });
            }
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
        public LocalMap(int _id, string _hash, string _filename)
        {
            id = _id;
            hash = _hash;
            filename = _filename;
        }

        public int id = -1;
        public string hash = "";
        public string filename = "";
    }
}
