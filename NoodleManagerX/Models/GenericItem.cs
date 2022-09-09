using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Reactive;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MemoryStream = System.IO.MemoryStream;
using Path = System.IO.Path;
using Stream = System.IO.Stream;

namespace NoodleManagerX.Models
{
    [DataContract]
    abstract class GenericItem : ReactiveObject
    {
        public const int maxDeleteAttempts = 5;

        public abstract string target { get; set; }
        public abstract ItemType itemType { get; set; }

        [DataMember] public int id { get; set; }
        [DataMember] public string cover_url { get; set; }
        [DataMember] public string download_url { get; set; }
        [DataMember] public string published_at { get; set; }
        [DataMember] public int download_count { get; set; }
        [DataMember] public int upvote_count { get; set; }
        [DataMember] public int downvote_count { get; set; }
        [DataMember] public string description { get; set; }
        [DataMember] public string filename { get; set; }
        [Reactive] public Bitmap cover_bmp { get; set; }
        [Reactive] public bool selected { get; set; }
        [Reactive] public bool downloading { get; set; } = false;
        [Reactive] public bool downloaded { get; set; } = false;
        [Reactive] public bool blacklisted { get; set; } = false;
        [Reactive] public bool needsUpdate { get; set; } = false;
        [DataMember] public string name { get; set; }
        [DataMember] public User user { get; set; }
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
                    case ItemType.Mod: return MainViewModel.s_instance.modHandler;
                    default: return null;
                }
            }
        }

        public virtual string display_title
        {
            get { return name; }
        }
        public virtual string display_creator
        {
            get { return user.username; }
        }
        public virtual string display_preview { get { return null; } }
        public virtual string[] display_difficulties { get { return null; } }

        public DateTime updatedAt { get; set; }

        public ReactiveCommand<Unit, Unit> downloadCommand { get; set; }
        public ReactiveCommand<Unit, Unit> deleteCommand { get; set; }

        public int downloadAttempts = 0;


        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            Task.Run(() =>
            {
                LoadBitmap();

                UpdateBlacklisted();

                updatedAt = DateTime.Parse(published_at, null, System.Globalization.DateTimeStyles.RoundtripKind);

                downloadCommand = ReactiveCommand.Create((() =>
                {
                    if (MainViewModel.s_instance.selectedTabIndex != MainViewModel.TAB_MAPS)
                    {
                        MainViewModel.s_instance.OpenErrorDialog("Currently only map downloading is supported");
                        return;
                    }

                    if (StorageAbstraction.CanDownload() && !MainViewModel.s_instance.updatingLocalItems)
                    {
                        blacklisted = false;
                        DownloadScheduler.Download(this);
                    }
                }));

                deleteCommand = ReactiveCommand.Create((() =>
                {
                    if (!MainViewModel.s_instance.updatingLocalItems)
                    {
                        if (downloaded)
                        {
                            Task.Run(Delete);
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
                    }
                }));
            });
        }

        public async Task UpdateDownloaded(bool forceUpdate = false)
        {
            List<LocalItem> tmp = MainViewModel.s_instance.localItems.Where(x => x != null && x.CheckEquality(this)).ToList();

            if (tmp.Count() > 0)
            {
                if (tmp.Count() > 1)
                {
                    tmp = tmp.Where(x => x.itemType == itemType).OrderByDescending(x => x.modifiedTime).ToList();
                    foreach (LocalItem l in tmp.Skip(1))
                    {
                        MainViewModel.Log("Old version deleted of " + l.filename);
                        _ = Delete(l.filename);
                    }
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    downloaded = true;
                });

                filename = tmp[0].filename;
                if (itemType == ItemType.Map && !string.IsNullOrEmpty(tmp[0].hash))
                {
                    needsUpdate = tmp[0].hash != ((MapItem)this).hash;
                    if (needsUpdate) MainViewModel.Log("Hash difference detected for " + this.filename);
                }
                else
                {
                    needsUpdate = DateTime.Compare(updatedAt, tmp[0].modifiedTime) > 0;
                    if (needsUpdate) MainViewModel.Log("Date difference detected for " + this.filename);
                }
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    downloaded = false;
                    needsUpdate = false;
                });
            }
            if (needsUpdate || forceUpdate)
            {
                DownloadScheduler.Download(this);
            }
        }

        public void UpdateBlacklisted()
        {
            if (!String.IsNullOrEmpty(filename) && MainViewModel.s_instance.blacklist.Contains(filename))
            {
                _ = Dispatcher.UIThread.InvokeAsync(() =>
                {
                    blacklisted = true;
                });
            }
            else
            {
                _ = Dispatcher.UIThread.InvokeAsync(() =>
                {
                    blacklisted = false;
                });
            }
        }

        public async Task Download()
        {
            string path = "";
            MainViewModel.Log("Downloading " + display_title);
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

                string url = "https://synthriderz.com" + download_url;
                WebRequest request = WebRequest.Create(new Uri(url));
                using (WebResponse response = await request.GetResponseAsync())
                using (Stream stream = response.GetResponseStream())
                using (MemoryStream str = await FixMetadata(stream))
                {
                    if (String.IsNullOrEmpty(filename))
                    {
                        string contentDisposition = response.Headers["content-disposition"];
                        filename = HttpUtility.UrlDecode(new ContentDisposition(contentDisposition).FileName);
                    }

                    path = Path.Combine(target, filename);
                    await StorageAbstraction.WriteFile(str, path);
                }
            }
            catch (Exception e)
            {
                MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
            }

            try
            {
                if (await handler.GetLocalItem(path))
                {
                    if (updatedAt != new DateTime())
                    {
                        _ = Task.Run(() => StorageAbstraction.SetLastWriteTime(updatedAt, path));
                    }

                    _ = Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        downloaded = true;
                    });
                }
                else
                {
                    DownloadScheduler.Requeue(this);
                }

                _ = Dispatcher.UIThread.InvokeAsync(() =>
                {
                    downloading = false;
                });

                DownloadScheduler.Remove(this);
            }
            catch (Exception e) { MainViewModel.Log(MethodBase.GetCurrentMethod(), e); }
        }

        public virtual async Task<MemoryStream> FixMetadata(Stream stream)
        {
            MemoryStream ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            stream.Flush();
            stream.Close();
            return ms;
        }

        public void Delete()
        {
            if (Delete(filename))
            {
                _ = Dispatcher.UIThread.InvokeAsync(() =>
                {
                    downloaded = false;
                });
            }
        }

        public bool Delete(string filename)
        {
            if (!String.IsNullOrEmpty(filename))
            {
                try
                {
                    StorageAbstraction.DeleteFile(Path.Combine(target, filename));
                    MainViewModel.s_instance.localItems = MainViewModel.s_instance.localItems.Where(x => x != null && !(x.itemType == itemType && x.filename == filename)).ToList();
                    return true;
                }
                catch (Exception e) { MainViewModel.Log(MethodBase.GetCurrentMethod(), e); }
            }
            return false;
        }

        public virtual void LoadBitmap()
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
