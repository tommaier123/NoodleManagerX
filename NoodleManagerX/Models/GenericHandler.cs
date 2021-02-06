using System;
using System.Collections.Generic;
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

        public virtual string allParameters { get; set; } = "";
        public virtual string select { get; set; } = "";
        public virtual string apiEndpoint { get; set; } = "";

        public virtual void LoadLocalItems() { }

        public void GetPage(bool download = false)
        {
            int requestID = MainViewModel.s_instance.apiRequestCounter;
            Clear();
            Task.Run(async () =>
            {
                try
                {
                    MainViewModel.Log("Getting Page " + MainViewModel.s_instance.currentPage);

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

                            string req = apiEndpoint + "?select=" + select + "&limit=" + pagesize + "&page=" + ((MainViewModel.s_instance.currentPage - 1) * pagecount + i) + "&s=" + search + "&sort=" + sortMethod + "," + sortOrder;
                            var page = DeserializePage(await client.DownloadStringTaskAsync(req));

                            if (MainViewModel.s_instance.apiRequestCounter != requestID && !download) break;

                            if (i == 1)
                            {
                                //dont wait by discarding result with _ variable
                                _ = Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    MainViewModel.s_instance.numberOfPages = (int)Math.Ceiling((double)page.pagecount / pagecount);
                                });
                            }

                            List<GenericItem> tmp = new List<GenericItem>();

                            foreach (GenericItem item in page.data)
                            {
                                tmp.Add(item);
                            }

                            _ = Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                MainViewModel.s_instance.items.Add(tmp);
                            });

                            if (download)
                            {
                                foreach (GenericItem item in page.data)
                                {
                                    if (!item.downloaded)
                                    {
                                        DownloadScheduler.queue.Add(item);
                                    }
                                }
                            }
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
                                    if (!instances[0].downloaded)
                                    {
                                        DownloadScheduler.queue.Add(instances[0]);
                                    }
                                }
                                else
                                {
                                    if (!item.downloaded || item.needsUpdate)
                                    {
                                        DownloadScheduler.queue.Add(item);
                                        if (item.needsUpdate) { MainViewModel.Log("Update"); }
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
            MainViewModel.s_instance.items.Add(tmp);
        }
    }
}
