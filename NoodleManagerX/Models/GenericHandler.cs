using Avalonia.Threading;
using DynamicData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Path = System.IO.Path;


namespace NoodleManagerX.Models
{
    abstract class GenericHandler
    {
        public abstract ItemType itemType { get; set; }
        public abstract string apiEndpoint { get; set; }
        public abstract string folder { get; set; }
        public abstract string[] extensions { get; set; }

        public const int pageChunkCount = 6;
        public const int pageChunkSize = 10;
        public const int getAllPageSize = 24;
        public int chunkCount = 1;

        public virtual Dictionary<string, string> queryFields { get; set; } = new Dictionary<string, string>() { { "name", "$contL" }, { "user.username", "$contL" } };
        public virtual string join { get; set; } = "";
        public virtual string selectAll { get; set; } = "id,cover_url,download_url,published_at,download_count,upvote_count,downvote_count,score,rating,vote_diff,user,filename,";
        public virtual string select { get; set; } = "name";
        public virtual string selectDownloadAll { get; set; } = "id,download_url,published_at,updated_at,filename,";
        public virtual string selectDownload { get; set; } = "";

        public async Task LoadLocalItems()
        {
            try
            {
                List<LocalItem> remove = new List<LocalItem>();
                string[] localFilenames = (StorageAbstraction.GetFilesInDirectory(folder)).Select(x => Path.GetFileName(x)).ToArray();

                foreach (LocalItem item in MainViewModel.s_instance.localItems)
                {
                    if (item.itemType == itemType)
                    {
                        if (!localFilenames.Contains(item.filename))
                        {
                            MainViewModel.Log("Missing from files " + item.filename);
                            remove.Add(item);
                        }
                    }
                }
                MainViewModel.s_instance.localItems.Remove(remove);


                int i = 0;
                foreach (string filename in localFilenames)
                {
                    i++;
                    _ = Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        MainViewModel.s_instance.progress = (int)(i / (localFilenames.Length * 0.01f));
                        MainViewModel.s_instance.progressText = "Loading Database: " + MainViewModel.s_instance.progress + "% (" + i + "/" + localFilenames.Length + ")";
                    });

                    if (extensions.Contains(Path.GetExtension(filename)))
                    {
                        List<LocalItem> existingEntries = MainViewModel.s_instance.localItems.Where(x => x.filename == filename).ToList();
                        if (existingEntries.Count() == 0)
                        {
                            MainViewModel.Log("Missing from db " + filename);
                            await GetLocalItem(Path.Combine(folder, filename));
                        }
                        else
                        {
                            if (existingEntries.Count() > 1)//duplicates in database, take the latest one
                            {
                                MainViewModel.Log("Duplicates in database of: " + filename);
                                existingEntries = existingEntries.OrderByDescending(x => x.modifiedTime).ToList();
                                foreach (LocalItem entry in existingEntries.Skip(1))
                                {
                                    MainViewModel.s_instance.localItems.Remove(entry);
                                }
                            }
                        }
                    }
                }
                _ = Dispatcher.UIThread.InvokeAsync(() =>
                {
                    MainViewModel.s_instance.progress = 0;
                    MainViewModel.s_instance.progressText = null;
                });
            }
            catch (Exception e) { MainViewModel.Log(MethodBase.GetCurrentMethod(), e); }
        }

        public virtual Task<bool> GetLocalItem(string path)
        {
            try
            {
                if (StorageAbstraction.FileExists(path))
                {
                    MainViewModel.s_instance.localItems.Add(new LocalItem(-1, "", Path.GetFileName(path), StorageAbstraction.GetLastWriteTime(path), itemType));
                    return Task.FromResult(true);
                }
            }
            catch (Exception e) { MainViewModel.Log(MethodBase.GetCurrentMethod(), e); }
            return Task.FromResult(false);
        }

        public async Task GetPage()
        {
            int requestID = MainViewModel.s_instance.apiRequestCounter;
            Clear();
            chunkCount = 1;
            try
            {
                for (int i = 1; i <= Math.Min(pageChunkCount, chunkCount); i++)
                {
                    using (WebClient client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;

                        string sortMethod = "published_at";
                        string sortOrder = "DESC";

                        JArray searchParameters = new JArray();
                        JObject query = new JObject();
                        JObject filter = new JObject();
                        JObject convert = new JObject();

                        if (MainViewModel.s_instance.searchText != "")
                        {
                            Dictionary<string, string> queries = queryFields;

                            if (!String.IsNullOrEmpty(MainViewModel.s_instance.selectedSearchParameter?.Name) && MainViewModel.s_instance.selectedSearchParameter.Name != "all")
                            {
                                queries = new Dictionary<string, string>() { { MainViewModel.s_instance.selectedSearchParameter.Name, "$contL" } };
                            }

                            query =
                            new JObject(
                                new JProperty("$or",
                                    new JArray(
                                        from q in queries
                                        select new JObject(
                                            new JProperty(q.Key,
                                            new JObject(
                                                new JProperty(q.Value,
                                                new JValue(MainViewModel.s_instance.searchText)
                                                )
                                            )
                                            )
                                        )
                                    )
                                    )
                               );
                        }

                        if (MainViewModel.s_instance.selectedDifficultyIndex != 0 && !String.IsNullOrEmpty(MainViewModel.s_instance.selectedDifficulty?.Name))
                        {
                            filter =
                            new JObject(
                                    new JProperty("difficulties",
                                    new JObject(
                                        new JProperty("$jsonContainsAny",
                                        new JArray(
                                            new JValue(MainViewModel.s_instance.selectedDifficulty.Name)
                                        )
                                        )
                                    )
                                    )
                                );
                        }

                        if (!MainViewModel.s_instance.settings.allowConverts && MainViewModel.s_instance.selectedTabIndex == 0)
                        {
                            convert =
                            new JObject(
                                    new JProperty("beat_saber_convert",
                                    new JObject(
                                        new JProperty("$ne",
                                            new JValue(true)
                                        )
                                    )
                                    )
                                );
                        }

                        searchParameters.Add(query);
                        searchParameters.Add(filter);
                        searchParameters.Add(convert);

                        JObject search = new JObject(
                            new JProperty("$and", searchParameters)
                            );


                        if (!String.IsNullOrEmpty(MainViewModel.s_instance.selectedSortMethod?.Name)) sortMethod = MainViewModel.s_instance.selectedSortMethod.Name;
                        if (!String.IsNullOrEmpty(MainViewModel.s_instance.selectedSortOrder?.Name)) sortOrder = MainViewModel.s_instance.selectedSortOrder.Name;

                        string req = apiEndpoint + "?select=" + selectAll + select + "&limit=" + pageChunkSize + "&page=" + ((MainViewModel.s_instance.currentPage - 1) * pageChunkCount + i) + "&s=" + search.ToString(Formatting.None) + "&join=" + join + "&sort=" + sortMethod + "," + sortOrder;

                        dynamic page = DeserializePage(await client.DownloadStringTaskAsync(req));
                        if (MainViewModel.s_instance.apiRequestCounter != requestID) break;

                        if (i == 1)
                        {
                            chunkCount = page.pagecount;

                            _ = Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                MainViewModel.s_instance.numberOfPages = (int)Math.Ceiling((double)page.pagecount / pageChunkCount);
                            });
                        }

                        List<GenericItem> tmp = new List<GenericItem>(page.data);

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            MainViewModel.s_instance.items.AddRange(tmp);
                        });

                        //parallel.foreach is not necessary but seems more robust against changing collections
                        Parallel.ForEach(MainViewModel.s_instance.items, item =>
                        {
                            _ = item.UpdateDownloaded(MainViewModel.s_instance.downloadPage);
                        });
                    }
                }
            }
            catch (Exception e) { MainViewModel.Log(MethodBase.GetCurrentMethod(), e); }
        }

        public async Task GetAll()
        {
            if (!MainViewModel.s_instance.getAllRunning)
            {
                MainViewModel.s_instance.getAllRunning = true;

                try
                {
                    MainViewModel.Log("Get All Started");

                    int pageCountAll = 1;
                    int i = 1;

                    do
                    {
                        using (WebClient client = new WebClient())
                        {
                            JObject convert = new JObject();
                            if (!MainViewModel.s_instance.settings.allowConverts && MainViewModel.s_instance.selectedTabIndex == 0)
                            {
                                convert =
                                new JObject(
                                        new JProperty("beat_saber_convert",
                                        new JObject(
                                            new JProperty("$ne",
                                                new JValue(true)
                                            )
                                        )
                                        )
                                    );
                            }

                            string req = apiEndpoint + "?select=" + selectDownloadAll + selectDownload + "&limit=" + getAllPageSize + "&page=" + i + "&s=" + convert.ToString(Formatting.None);

                            dynamic page = DeserializePage(await client.DownloadStringTaskAsync(req));

                            if (MainViewModel.s_instance.closing) break;
                            foreach (GenericItem item in page.data)
                            {
                                List<GenericItem> instances = MainViewModel.s_instance.items.Where(x => x.id == item.id).ToList();
                                if (instances.Count > 0)
                                {
                                    if (!instances[0].downloaded || instances[0].needsUpdate)
                                    {
                                        DownloadScheduler.Download(instances[0]);
                                    }
                                }
                                else
                                {
                                    await item.UpdateDownloaded();
                                    if (!item.downloaded || item.needsUpdate)
                                    {
                                        if (item.needsUpdate) { MainViewModel.Log("Updating " + item.filename); }
                                        DownloadScheduler.Download(item);
                                    }
                                }
                            }
                            pageCountAll = page.pagecount;

                            if (DownloadScheduler.queue.Count == 0 && DownloadScheduler.downloading.Count == 0)
                            {
                                _ = Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    if (i < pageCountAll)
                                    {
                                        MainViewModel.s_instance.progress = (int)(i / (pageCountAll * 0.01f));
                                        MainViewModel.s_instance.progressText = "Getting All (checking for new maps): " + MainViewModel.s_instance.progress + "% (" + i + "/" + pageCountAll + ")";
                                    }
                                    else
                                    {
                                        MainViewModel.s_instance.progress = 0;
                                        MainViewModel.s_instance.progressText = null;
                                    }
                                });
                            }

                            i++;
                        }
                    }
                    while (i <= pageCountAll);

                }
                catch (Exception e) { MainViewModel.Log(MethodBase.GetCurrentMethod(), e); }
                finally
                {
                    MainViewModel.Log("Get All Done");
                    MainViewModel.s_instance.getAllRunning = false;
                }
            }
        }

        public abstract dynamic DeserializePage(string json);

        public void Clear()
        {
            var tmp = MainViewModel.s_instance.items.Where(x => x.itemType != itemType);
            MainViewModel.s_instance.items.Clear();
            MainViewModel.s_instance.items.AddRange(tmp);
        }
    }
}
