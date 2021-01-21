using System;
using Newtonsoft.Json;
using DynamicData;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Net;
using System.Text;
using Avalonia.Threading;
using System.Reflection;
using System.Linq;

namespace NoodleManagerX.Models
{
    class MapHandler : GenericHandler
    {
        public override ItemType itemType { get; set; } = ItemType.Map;

        private const string mapSearchQuerry = "{\"$or\":[{\"title\":{\"$contL\":\"<value>\"}},{\"artist\":{\"$contL\":\"<value>\"}},{\"mapper\":{\"$contL\":\"<value>\"}}]}";

        public override void LoadLocalItems()
        {
            string directory = Path.Combine(MainViewModel.s_instance.settings.synthDirectory, "CustomSongs");
            if (Directory.Exists(directory))
            {
                Task.Run(async () =>
                {
                    List<LocalItem> tmp = new List<LocalItem>();
                    foreach (string file in Directory.GetFiles(directory))
                    {
                        if (Path.GetExtension(file) == ".synth")
                        {
                            try
                            {
                                using (ZipArchive archive = ZipFile.OpenRead(file))
                                {
                                    foreach (ZipArchiveEntry entry in archive.Entries)
                                    {
                                        if (entry.FullName == "synthriderz.meta.json")
                                        {
                                            using (StreamReader sr = new StreamReader(entry.Open()))
                                            {
                                                LocalItem localMap = JsonConvert.DeserializeObject<LocalItem>(await sr.ReadToEndAsync());
                                                localMap.itemType = ItemType.Map;
                                                tmp.Add(localMap);
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                Console.WriteLine("Deleting corrupted file " + Path.GetFileName(file));
                                File.Delete(file);
                            }
                        }
                    }
                    MainViewModel.s_instance.localItems.Add(tmp);
                });
            }
        }

        public override async void PageTaskFunction(int requestID, bool download = false)
        {
            try
            {
                Console.WriteLine("Getting Page");

                for (int i = 1; i <= pagecount; i++)
                {
                    using (WebClient client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;
                        string sortMethod = "published_at";
                        string sortOrder = "DESC";
                        string search = "";
                        if (MainViewModel.s_instance.selectedSortMethod?.Name != null) sortMethod = MainViewModel.s_instance.selectedSortMethod.Name;
                        if (MainViewModel.s_instance.selectedSortOrder?.Name != null) sortOrder = MainViewModel.s_instance.selectedSortOrder.Name;
                        if (MainViewModel.s_instance.searchText != "") search = "&s=" + mapSearchQuerry.Replace("<value>", MainViewModel.s_instance.searchText);

                        string req = "https://synthriderz.com/api/beatmaps?limit=" + pagesize + "&page=" + ((MainViewModel.s_instance.currentPage - 1) * pagecount + i) + search + "&sort=" + sortMethod + "," + sortOrder;
                        MapPage mapPage = JsonConvert.DeserializeObject<MapPage>(await client.DownloadStringTaskAsync(req));

                        if (MainViewModel.s_instance.apiRequestCounter != requestID && !download) break;

                        if (i == 1)
                        {
                            //dont wait by discarding result with _ variable
                            _ = Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                MainViewModel.s_instance.numberOfPages = (int)Math.Ceiling((double)mapPage.pagecount / pagecount);
                            });
                        }

                        _ = Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            MainViewModel.s_instance.items.Add(mapPage.data);
                        });

                        if (download)
                        {
                            foreach (MapItem map in mapPage.data)
                            {
                                if (!map.downloaded)
                                {
                                    DownloadScheduler.queue.Add(map);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e) { MainViewModel.Log(MethodBase.GetCurrentMethod(), e); }
        }

        public override async void GetAllTaskFunction()
        {
            try
            {
                Console.WriteLine("Get All Started");

                int pageCountAll = 1;
                int i = 1;

                do
                {
                    using (WebClient client = new WebClient())
                    {
                        string req = "https://synthriderz.com/api/beatmaps?limit=" + pagesize + "&page=" + i;
                        MapPage mapPage = JsonConvert.DeserializeObject<MapPage>(await client.DownloadStringTaskAsync(req));

                        if (MainViewModel.s_instance.closing) break;
                        foreach (MapItem map in mapPage.data)
                        {
                            var instances = MainViewModel.s_instance.items.Where(x => x.itemType == ItemType.Map && x.id == map.id).ToList();
                            if (instances.Count > 0)
                            {
                                if (!instances[0].downloaded)
                                {
                                    DownloadScheduler.queue.Add(instances[0]);
                                }
                            }
                            else
                            {
                                if (!map.downloaded)
                                {
                                    DownloadScheduler.queue.Add(map);
                                }
                            }
                        }
                        pageCountAll = mapPage.pagecount;
                        i++;
                    }
                }
                while (i <= pageCountAll);

            }
            catch (Exception e) { MainViewModel.Log(MethodBase.GetCurrentMethod(), e); }
            Console.WriteLine("Get All Done");
        }
    }
}
