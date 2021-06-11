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
        [DataMember] public int download_count { get; set; }
        [DataMember] public int upvote_count { get; set; }
        [DataMember] public int downvote_count { get; set; }
        [DataMember] public string description { get; set; }
        [DataMember] public string download_filename { get; set; } = "";
        [Reactive] public Bitmap cover_bmp { get; set; }
        [Reactive] public bool selected { get; set; }
        [Reactive] public bool downloading { get; set; } = false;
        [Reactive] public bool downloaded { get; set; } = false;
        [Reactive] public bool blacklisted { get; set; } = false;
        [Reactive] public bool needsUpdate { get; set; } = false;
        public virtual string display_title { get; }
        public virtual string display_creator { get; }
        public virtual string display_preview { get { return null; } }
        public virtual string[] display_difficulties { get { return null; } }
        public DateTime updatedAt { get; set; }
        public virtual string target { get; set; }

        public virtual ItemType itemType { get; set; }

        public ReactiveCommand<Unit, Unit> downloadCommand { get; set; }
        public ReactiveCommand<Unit, Unit> deleteCommand { get; set; }


        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            LoadBitmap();

            UpdateBlacklisted();

            Task.Run(() =>
            {
                updatedAt = DateTime.Parse(published_at, null, System.Globalization.DateTimeStyles.RoundtripKind);
            });

            downloadCommand = ReactiveCommand.Create((() =>
            {
                if (MainViewModel.s_instance.CheckDirectory(MainViewModel.s_instance.settings.synthDirectory, true))
                {
                    blacklisted = false;
                    Download();
                }
            }));

            deleteCommand = ReactiveCommand.Create((() =>
            {
                if (downloaded)
                {
                    Delete();
                }
                else
                {
                    blacklisted = !blacklisted;

                    if (blacklisted)
                    {
                        MainViewModel.s_instance.blacklist.Add(download_filename);
                    }
                    else
                    {
                        MainViewModel.s_instance.blacklist.Remove(download_filename);
                    }
                }
            }));
        }

        public void UpdateDownloaded()
        {
            Task.Run(() =>
            {
                List<LocalItem> tmp = MainViewModel.s_instance.localItems.Where(x => x != null && x.CheckEquality(this)).ToList();
                _ = Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (tmp.Count() > 0)
                    {
                        downloaded = true;
                        download_filename = tmp[0].filename;
                        if (itemType == ItemType.Map && !string.IsNullOrEmpty(tmp[0].hash))
                        {
                            needsUpdate = tmp[0].hash != ((MapItem)this).hash;
                            if (needsUpdate) Console.WriteLine("Hash difference detected for " + this.display_title);
                        }
                        else
                        {
                            needsUpdate = DateTime.Compare(updatedAt, tmp[0].modifiedTime) > 0;
                            if (needsUpdate) Console.WriteLine("Date difference detected for " + this.display_title);
                        }
                    }
                    else
                    {
                        downloaded = false;
                        needsUpdate = false;
                    }
                });
            });
        }

        public void UpdateBlacklisted()
        {
            Task.Run(() =>
            {
                if (!String.IsNullOrEmpty(download_filename) && MainViewModel.s_instance.blacklist.Contains(download_filename))
                {
                    _ = Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        blacklisted = true;
                    });
                }
            });
        }

        public void Download()
        {
            if (MainViewModel.s_instance.settings.synthDirectory != "" && !blacklisted)
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
                        if (String.IsNullOrEmpty(download_filename))
                        {
                            webClient.OpenRead(url);
                            string header_contentDisposition = webClient.ResponseHeaders["content-disposition"];
                            download_filename = new ContentDisposition(header_contentDisposition).FileName;
                        }

                        string filepath = Path.Combine(MainViewModel.s_instance.settings.synthDirectory, target, download_filename);

                        await webClient.DownloadFileTaskAsync(new Uri(url), filepath);

                        webClient.Dispose();

                        if (File.Exists(filepath))
                        {
                            LocalItem localItem = new LocalItem(id, "", Path.GetFileName(filepath), DateTime.Now, this.itemType);
                            MainViewModel.s_instance.localItems.Add(localItem);

                            _ = Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                downloaded = true;
                            });
                        }
                        else
                        {
                            //implement timeout!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                            MainViewModel.Log("Download failed. Requeueing " + display_title);
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
        }

        public void Delete()
        {
            Task.Run(async () =>
            {
                if (!String.IsNullOrEmpty(download_filename))
                {
                    string filepath = Path.Combine(MainViewModel.s_instance.settings.synthDirectory, target, download_filename);

                    if (File.Exists(filepath))
                    {
                        try
                        {
                            File.Delete(filepath);
                        }
                        catch (IOException e)
                        {
                            //implement timeout!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                            MainViewModel.Log(e.Message);
                            await Task.Delay(100);
                            Delete();
                        }

                        MainViewModel.s_instance.localItems = MainViewModel.s_instance.localItems.Where(x => x != null && !x.CheckEquality(this)).ToList();

                        _ = Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            downloaded = false;
                        });
                    }
                }
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
            if (item != null && itemType == item.itemType)
            {
                if (itemType == ItemType.Map && id != -1)
                {
                    return this.id == item.id && (!checkHash || hash == ((MapItem)item).hash);
                }
                else
                {
                    return filename == item.download_filename;
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
