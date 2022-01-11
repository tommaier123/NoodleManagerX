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
        public const int maxDownloadAttempts = 3;
        public const int maxDeleteAttempts = 20;

        [DataMember] public int id { get; set; }
        [DataMember] public string cover_url { get; set; }
        [DataMember] public string download_url { get; set; }
        [DataMember] public string updated_at { get; set; }
        [DataMember] public int download_count { get; set; }
        [DataMember] public int upvote_count { get; set; }
        [DataMember] public int downvote_count { get; set; }
        [DataMember] public string description { get; set; }
        [DataMember] public string filename { get; set; } = "";
        [Reactive] public Bitmap cover_bmp { get; set; }
        [Reactive] public bool selected { get; set; }
        [Reactive] public bool downloading { get; set; } = false;
        [Reactive] public bool downloaded { get; set; } = false;
        [Reactive] public bool blacklisted { get; set; } = false;
        [Reactive] public bool needsUpdate { get; set; } = false;
        public GenericHandler handler
        {
            get
            {
                switch (itemType)
                {
                    case ItemType.Map: return MainViewModel.s_instance.mapHandler;
                    case ItemType.Playlist: return MainViewModel.s_instance.playlistHandler;
                    case ItemType.Avatar: return MainViewModel.s_instance.avatarHandler;
                    case ItemType.Stage: return MainViewModel.s_instance.stageHandler;
                    default: return null;
                }
            }
        }
        public virtual string display_title { get; }
        public virtual string display_creator { get; }
        public virtual string display_preview { get { return null; } }
        public virtual string[] display_difficulties { get { return null; } }
        public DateTime updatedAt { get; set; }
        public virtual string target { get; set; }

        public virtual ItemType itemType { get; set; }

        public ReactiveCommand<Unit, Unit> downloadCommand { get; set; }
        public ReactiveCommand<Unit, Unit> deleteCommand { get; set; }

        public int downloadAttempts = 0;


        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            LoadBitmap();

            UpdateBlacklisted();

            Task.Run(() =>
            {
                updatedAt = DateTime.Parse(updated_at, null, System.Globalization.DateTimeStyles.RoundtripKind);
            });

            downloadCommand = ReactiveCommand.Create((() =>
            {
                if (MainViewModel.s_instance.CheckDirectory(MainViewModel.s_instance.settings.synthDirectory, true))
                {
                    blacklisted = false;
                    DownloadScheduler.Download(this);
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
                        MainViewModel.s_instance.blacklist.Add(filename);
                    }
                    else
                    {
                        MainViewModel.s_instance.blacklist.Remove(filename);
                    }
                }
            }));
        }

        public Task UpdateDownloaded(bool forceUpdate = false)
        {
            return Task.Run(async () =>
             {
                 List<LocalItem> tmp = MainViewModel.s_instance.localItems.Where(x => x != null && x.CheckEquality(this)).ToList();
                 await Dispatcher.UIThread.InvokeAsync(() =>
                 {
                     if (tmp.Count() > 0)
                     {
                         if (tmp.Count() > 1)
                         {
                             tmp = tmp.Where(x => x.itemType == itemType).OrderByDescending(x => x.modifiedTime).ToList();
                             foreach (LocalItem l in tmp.Skip(1))
                             {
                                 Console.WriteLine("Old version deleted of " + l.filename);
                                 Delete(l.filename);
                             }
                         }

                         downloaded = true;
                         filename = tmp[0].filename;
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
                     if (needsUpdate || forceUpdate)
                     {
                         DownloadScheduler.Download(this);
                     }
                 });
             });
        }

        public void UpdateBlacklisted()
        {
            Task.Run(() =>
            {
                if (!String.IsNullOrEmpty(filename) && MainViewModel.s_instance.blacklist.Contains(filename))
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
                    string filepath = "";

                    try
                    {
                        if (downloaded)
                        {//remove from local items
                            MainViewModel.s_instance.localItems = MainViewModel.s_instance.localItems.Where(x => x.CheckEquality(this) == false).ToList();
                        }

                        _ = Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            downloaded = false;
                            downloading = true;
                        });

                        using (WebClient webClient = new WebClient())
                        {
                            string url = "https://synthriderz.com" + download_url;

                            //remove once filename is in the api!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                            if (String.IsNullOrEmpty(filename))
                            {
                                webClient.OpenRead(url);
                                string header_contentDisposition = webClient.ResponseHeaders["content-disposition"];
                                filename = new ContentDisposition(header_contentDisposition).FileName;
                            }

                            Console.WriteLine("Downloading " + filename);

                            filepath = Path.Combine(MainViewModel.s_instance.settings.synthDirectory, target, filename);

                            await webClient.DownloadFileTaskAsync(new Uri(url), filepath);
                        }
                    }
                    catch (Exception e)
                    {
                        MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
                        Requeue();
                    }

                    if (await handler.GetLocalItem(filepath, MainViewModel.s_instance.localItems, this))
                    {
                        File.SetLastWriteTime(filepath, updatedAt);

                        _ = Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            downloaded = true;
                        });
                    }
                    else
                    {
                        Requeue();
                    }

                    _ = Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        downloading = false;
                    });

                    DownloadScheduler.downloading.Remove(this);
                });
            }
        }

        private void Requeue()
        {
            if (downloadAttempts < maxDownloadAttempts)
            {
                MainViewModel.Log("Requeueing " + filename);
                downloadAttempts++;
                DownloadScheduler.queue.Add(this);
            }
            else
            {
                MainViewModel.Log("Timeout " + filename);
            }
        }

        public void Delete()
        {
            Delete(filename);
        }

        public void Delete(string filename)
        {
            Task.Run(async () =>
            {
                if (!String.IsNullOrEmpty(filename))
                {
                    string filepath = Path.Combine(MainViewModel.s_instance.settings.synthDirectory, target, filename);

                    int i = 0;

                    while (File.Exists(filepath) && i < maxDeleteAttempts)
                    {
                        i++;
                        try
                        {
                            File.Delete(filepath);
                        }
                        catch (IOException e)
                        {
                            MainViewModel.Log(e.Message);
                            await Task.Delay(200);
                        }
                    }
                    if (!File.Exists(filepath))
                    {
                        MainViewModel.s_instance.localItems = MainViewModel.s_instance.localItems.Where(x => x != null && x.itemType == itemType && x.filename == filename).ToList();

                        _ = Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            downloaded = false;
                        });
                    }
                    else
                    {
                        MainViewModel.Log("Failed to delete " + this.filename);
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
