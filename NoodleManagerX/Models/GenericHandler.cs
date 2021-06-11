using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using DynamicData;
using Newtonsoft.Json;

namespace NoodleManagerX.Models
{
    class GenericHandler
    {
        public virtual ItemType itemType { get; set; }

        public const int pagecount = 6;
        public const int pagesize = 10;

        public virtual string allParameters { get; set; } = "\"name\":{\"$contL\":\"<value>\"}},{\"user.username\":{\"$contL\":\"<value>\"}";
        public virtual string select { get; set; } = "name,user";
        private string selectAll { get; set; } = "id,cover_url,download_url,published_at,download_count,upvote_count,downvote_count,description,score,rating,vote_diff,";
        public virtual string apiEndpoint { get; set; } = "";
        public virtual string folder { get; set; } = "";
        public virtual string[] extensions { get; set; } = { };

        public virtual async void LoadLocalItems()
        {
            if (MainViewModel.s_instance.settings.synthDirectory != "")
            {
                string directory = Path.Combine(MainViewModel.s_instance.settings.synthDirectory, folder);
                if (Directory.Exists(directory))
                {
                    List<LocalItem> tmp = new List<LocalItem>();
                    foreach (string file in Directory.GetFiles(directory))
                    {
                        if (extensions.Contains(Path.GetExtension(file)))
                        {
                            await GetLocalItem(file, tmp);
                        }
                    }
                    MainViewModel.s_instance.localItems.AddRange(tmp);
                }
            }
        }

        public virtual Task GetLocalItem(string file, List<LocalItem> list)
        {
            list.Add(new LocalItem(-1, "", Path.GetFileName(file), File.GetLastWriteTime(file), itemType));
            return Task.CompletedTask;
        }

        public void GetPage()
        {
            int requestID = MainViewModel.s_instance.apiRequestCounter;
            Clear();
            Task.Run(async () =>
            {
                try
                {
                    for (int i = 1; i <= pagecount; i++)
                    {
                        using (WebClient client = new WebClient())
                        {
                            client.Encoding = Encoding.UTF8;

                            string query = allParameters;
                            string searchbase = "\"<parameter>\":{\"$contL\":\"<value>\"}";
                            string filter = "\"difficulties\":{\"$jsonContainsAny\":[\"<difficulty>\"]}";
                            string convert = "\"beat_saber_convert\":{\"$ne\":true}";
                            string sortMethod = "published_at";
                            string sortOrder = "DESC";


                            if (!String.IsNullOrEmpty(MainViewModel.s_instance.selectedSearchParameter?.Name) && MainViewModel.s_instance.selectedSearchParameter.Name != "all")
                            {
                                query = searchbase.Replace("<parameter>", MainViewModel.s_instance.selectedSearchParameter.Name);
                            }

                            if (MainViewModel.s_instance.searchText != "")
                            {
                                query = query.Replace("<value>", MainViewModel.s_instance.searchText);
                            }
                            else
                            {
                                query = "";
                            }

                            if (MainViewModel.s_instance.selectedDifficultyIndex != 0 && !String.IsNullOrEmpty(MainViewModel.s_instance.selectedDifficulty?.Name))
                            {
                                filter = filter.Replace("<difficulty>", MainViewModel.s_instance.selectedDifficulty.Name);
                            }
                            else
                            {
                                filter = "";
                            }

                            if (MainViewModel.s_instance.settings.allowConverts || MainViewModel.s_instance.selectedTabIndex != 0)
                            {
                                convert = "";
                            }

                            string search = "{\"$and\":[{" + filter + "},{" + convert + "},{\"$or\":[{" + query + "}]}]}";

                            if (!String.IsNullOrEmpty(MainViewModel.s_instance.selectedSortMethod?.Name)) sortMethod = MainViewModel.s_instance.selectedSortMethod.Name;
                            if (!String.IsNullOrEmpty(MainViewModel.s_instance.selectedSortOrder?.Name)) sortOrder = MainViewModel.s_instance.selectedSortOrder.Name;

                            string req = apiEndpoint + "?select=" + selectAll + select + "&limit=" + pagesize + "&page=" + ((MainViewModel.s_instance.currentPage - 1) * pagecount + i) + "&s=" + search + "&sort=" + sortMethod + "," + sortOrder;
                            var page = DeserializePage(await client.DownloadStringTaskAsync(req));
                            if (MainViewModel.s_instance.apiRequestCounter != requestID) break;

                            if (i == 1)
                            {
                                //dont wait by discarding result with _ variable
                                _ = Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    MainViewModel.s_instance.numberOfPages = (int)Math.Ceiling((double)page.pagecount / pagecount);
                                });
                            }

                            List<GenericItem> tmp = new List<GenericItem>(page.data);

                            _ = Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                MainViewModel.s_instance.items.AddRange(tmp);
                                foreach (GenericItem item in MainViewModel.s_instance.items)
                                {
                                    item.UpdateDownloaded(MainViewModel.s_instance.downloadPage);
                                }
                            });
                        }
                    }
                }
                catch (Exception e) { MainViewModel.Log(MethodBase.GetCurrentMethod(), e); }
            });

        }

        public void GetAll()
        {
            Task.Run(async () =>
            {
                try
                {
                    MainViewModel.Log("Get All Started");

                    int pageCountAll = 1;
                    int i = 1;

                    do
                    {
                        using (WebClient client = new WebClient())
                        {
                            string req = apiEndpoint + "?limit=" + pagesize + "&page=" + i;
                            var page = DeserializePage(await client.DownloadStringTaskAsync(req));

                            if (MainViewModel.s_instance.closing) break;
                            foreach (GenericItem item in page.data)
                            {
                                List<GenericItem> instances = MainViewModel.s_instance.items.Where(x => x.itemType == ItemType.Map && x.id == item.id).ToList();
                                if (instances.Count > 0)
                                {
                                    if (!instances[0].downloaded || instances[0].needsUpdate)
                                    {
                                        DownloadScheduler.queue.Add(instances[0]);
                                    }
                                }
                                else
                                {
                                    await item.UpdateDownloaded();
                                    if (!item.downloaded || item.needsUpdate)
                                    {
                                        DownloadScheduler.queue.Add(item);
                                        if (item.needsUpdate) { MainViewModel.Log("Updating " + item.display_title); }
                                    }
                                }
                            }
                            pageCountAll = page.pagecount;
                            i++;
                        }
                    }
                    while (i <= pageCountAll);

                }
                catch (Exception e) { MainViewModel.Log(MethodBase.GetCurrentMethod(), e); }
                MainViewModel.Log("Get All Done");
            });
        }



        public virtual dynamic DeserializePage(string json) { return null; }

        public void Clear()
        {
            var tmp = MainViewModel.s_instance.items.Where(x => x.itemType != itemType);
            MainViewModel.s_instance.items.Clear();
            MainViewModel.s_instance.items.AddRange(tmp);
        }
    }
}
