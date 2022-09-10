using Avalonia.Threading;
using DynamicData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NoodleManagerX.Models;
using NoodleManagerX.Models.Mods;
using NoodleManagerX.Utils;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Semver;

namespace NoodleManagerX.Mods
{
    class ModHandler : GenericHandler
    {
        public override ItemType itemType { get; set; } = ItemType.Mod;
        public override string apiEndpoint { get; set; } = "https://raw.githubusercontent.com/bookdude13/SRModsList/dev/SynthRiders";
        public override string folder { get; set; } = "Mods";
        public override string[] extensions { get; set; } = { ".synthmod" };

        public override dynamic DeserializePage(string json)
        {
            return JsonConvert.DeserializeObject<ModPage>(json);
        }

        public async override Task GetAll()
        {
            Console.WriteLine("Mods page doesn't follow normal GetAll() process");
            await Task.CompletedTask;
        }

        private List<ModVersionSelection> GetLocalSelection(List<ModInfo> availableMods)
        {
            // TODO get from local db
            List<ModVersionSelection> localSelection = new();

            foreach (var mod in availableMods)
            {
                // Check if in local items list, and if so use version from there.
                // Default is to assume latest (null) and resolve with that
                var localItem = MainViewModel.s_instance.localItems.FirstOrDefault(item => item.hash == mod.Id);
                if (localItem == null)
                {
                    localSelection.Add(new ModVersionSelection(mod.Id, null));
                }
                else
                {
                    if (localItem.ItemVersion == null)
                    {
                        // Local selection is "latest"
                        localSelection.Add(new ModVersionSelection(mod.Id, null));
                    }
                    else
                    {
                        var selectedVersion = mod.Versions.FirstOrDefault(v => localItem.ItemVersion.ComparePrecedenceTo(v.Version) == 0);
                        if (selectedVersion == null)
                        {
                            MainViewModel.Log($"Locally selected version {localItem.ItemVersion} not found in mod list for mod {mod.Id}. Defaulting to latest");
                            localSelection.Add(new ModVersionSelection(mod.Id, null));
                        }
                        else
                        {
                            localSelection.Add(new ModVersionSelection(mod.Id, selectedVersion));
                        }
                    }
                }
            }

            return localSelection;
        }
        
        public async override Task GetPage()
        {
            var mods = await GetAvailableMods();
            Console.WriteLine($"{mods.Count} mods loaded");

            var localSelection = GetLocalSelection(mods);

            var dependencyGraph = new ModDependencyGraph();
            dependencyGraph.LoadMods(mods);
            var resolveResult = dependencyGraph.Resolve(localSelection);
            if (resolveResult == ModDependencyGraph.ResolvedState.RESOLVED)
            {
                Console.WriteLine($"Dependencies resolved: {dependencyGraph.ResolvedVersions.Count}");
                var modItems = mods.Select(
                    mod => new ModItem(
                        mod,
                        dependencyGraph.ResolvedVersions[mod.Id]
                    )
                );

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    MainViewModel.s_instance.numberOfPages = 1;
                    MainViewModel.s_instance.items.AddRange(modItems);
                });

                //parallel.foreach is not necessary but seems more robust against changing collections, maybe with the list copy no more errors?
                Parallel.ForEach(new List<GenericItem>(MainViewModel.s_instance.items), item =>
                {
                    _ = item.UpdateDownloaded();
                });
            }
            else
            {
                MainViewModel.Log("Failed to resolve dependencies");
                // TODO popup
            }
        }

        /// <summary>
        /// Mods are stored in a zip file with the .synthmod extension.
        /// Inside is the following:
        ///     A ModInfo.json file containing a serialized ModInfo representing this mod
        ///     All files that need to be copied, in a folder structure relative to the main SynthRiders directory
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public override async Task<bool> GetLocalItem(string path)
        {
            try
            {
                if (StorageAbstraction.FileExists(path))
                {
                    using (Stream stream = StorageAbstraction.ReadFile(path))
                    using (ZipArchive archive = new ZipArchive(stream))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            if (entry.FullName == "LocalItem.json")
                            {
                                using (System.IO.StreamReader sr = new System.IO.StreamReader(entry.Open()))
                                {
                                    LocalItem localItem = JsonConvert.DeserializeObject<LocalItem>(await sr.ReadToEndAsync());
                                    localItem.filename = Path.GetFileName(path);
                                    localItem.modifiedTime = StorageAbstraction.GetLastWriteTime(path);
                                    localItem.itemType = ItemType.Mod;
                                    MainViewModel.s_instance.localItems.Add(localItem);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
                MainViewModel.Log("Deleting corrupted file " + Path.GetFileName(path));
                try { StorageAbstraction.DeleteFile(path); }
                catch (Exception ee) { MainViewModel.Log(MethodBase.GetCurrentMethod(), ee); }
            }

            return false;
        }

        private async Task<List<ModInfo>> GetAvailableMods()
        {
            int requestID = MainViewModel.s_instance.apiRequestCounter;
            Clear();
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string gameVersion = GetCurrentGameVersion();
                    string requestUrl = apiEndpoint + "/" + Uri.EscapeDataString(gameVersion) + "/mods.json";
                    string rawResponse = await client.GetStringAsync(requestUrl);
                    if (MainViewModel.s_instance.apiRequestCounter != requestID)
                    {
                        // Stale request; cancel and end early
                        return new List<ModInfo>();
                    };

                    return ParseRemoteModList(rawResponse);
                }
            }
            catch (Exception e)
            {
                MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
                return new List<ModInfo>();
            }
        }

        private List<ModInfo> ParseRemoteModList(string rawRemoteModList)
        {
            if (rawRemoteModList == null)
            {
                return new();
            }

            try
            {
                var modList = JsonConvert.DeserializeObject<ModInfoList>(rawRemoteModList);
                return modList.AvailableMods;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed to deserialize mod list: " + e.Message);
                return new();
            }
        }

        private string GetCurrentGameVersion()
        {
            return UnityInformationHandler.GameVersion;
        }
    }
}
