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
        public override string apiEndpoint { get; set; } = ModDownloadSource.GetModsBaseUrl();
        public override string folder { get; set; } = "Mods";
        public override string[] extensions { get; set; } = { ".synthmod" };

        private Dictionary<string, ModVersion> installedVersions = new();

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
            List<ModVersionSelection> localSelection = new();

            foreach (var mod in availableMods)
            {
                // For now, assume we are using the latest version (null).
                // In the future, explicit version selection can be added here.
                localSelection.Add(new ModVersionSelection(mod.Id, null));
            }

            return localSelection;
        }

        private Dictionary<string, ModVersion> GetLocalInstalledVersions(List<ModInfo> availableMods)
        {
            Dictionary<string, ModVersion> localInstalls = new();

            foreach (var mod in availableMods)
            {
                // Check if in local items list, and if so use version from there.
                // Default is to assume latest (null) and resolve with that
                var localItem = MainViewModel.s_instance.localItems.FirstOrDefault(item => item.hash == mod.Id);
                if (localItem == null)
                {
                    localInstalls[mod.Id] = null;
                }
                else
                {
                    if (localItem.ItemVersion == null)
                    {
                        localInstalls[mod.Id] = null;
                    }
                    else
                    {
                        ModVersion localVersion = mod.Versions.FirstOrDefault(v => localItem.ItemVersion.ComparePrecedenceTo(v.Version) == 0);
                        if (localVersion == null)
                        {
                            MainViewModel.Log($"Locally installed version {localItem.ItemVersion} not found in mod list for mod {mod.Id}. Defaulting to null (not installed)");
                            localInstalls[mod.Id] = null;
                        }
                        else
                        {
                            localInstalls[mod.Id] = localVersion;
                        }
                    }
                }
            }

            return localInstalls;
        }

        public async override Task GetPage()
        {
            int requestID = MainViewModel.s_instance.apiRequestCounter;
            Clear();

            var mods = await GetAvailableMods();
            MainViewModel.Log($"{mods.Count} mods loaded");

            var localSelection = GetLocalSelection(mods);
            installedVersions = GetLocalInstalledVersions(mods);

            var dependencyGraph = new ModDependencyGraph();
            dependencyGraph.LoadMods(mods);
            var resolveResult = dependencyGraph.Resolve(localSelection);
            if (resolveResult == ModDependencyGraph.ResolvedState.RESOLVED)
            {
                MainViewModel.Log($"Dependencies resolved: {dependencyGraph.ResolvedVersions.Count}");

                var modItems = mods.Select(
                    mod => new ModItem(
                        mod,
                        installedVersions[mod.Id],
                        dependencyGraph.ResolvedVersions
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
                MainViewModel.s_instance.OpenErrorDialog("Failed to resolve mod dependencies");
            }
        }

        public ModVersion GetInstalledVersion(string modId)
        {
            return installedVersions.GetValueOrDefault(modId);
        }

        public void UpdateInstalledVersion(string modId, ModVersion installedVersion)
        {
            installedVersions[modId] = installedVersion;
        }

        public bool IsAnInstalledDependency(string checkedModId)
        {
            foreach (var modId in installedVersions.Keys)
            {
                // Skip ourselves
                if (modId == checkedModId)
                {
                    continue;
                }

                var version = installedVersions[modId];
                // Skip uninstalled mods
                if (version == null)
                {
                    continue;
                }

                // Installed and depends on this mod
                if (version.HasDependency(checkedModId))
                {
                    return true;
                }
            }

            return false;
        }

        public void RefreshUI()
        {
            foreach (var modItem in MainViewModel.s_instance.mods)
            {
                modItem.RefreshDeleteStatus();
            }
        }

        /// <summary>
        /// Mods are stored in a zip file with the .synthmod extension.
        /// Inside is the following:
        ///     A LocalItem.json file containing a serialized LocalItem representing the mod item.
        ///         The only field here that is set is the hash, equal to the ModId
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
            MainViewModel.Log("Game version: " + GetCurrentGameVersion());
            int requestID = MainViewModel.s_instance.apiRequestCounter;
            Clear();
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string requestUrl = apiEndpoint + "/mods.json";
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

                var availableMods = modList.AvailableMods;
                //ChangeVersionsForTest(availableMods);
                return availableMods;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed to deserialize mod list: " + e.Message);
                return new();
            }
        }

        /// <summary>
        /// Only used for spot checking version updates.
        /// DON'T USE IN PROD BUILDS!
        /// </summary>
        /// <param name="availableMods"></param>
        /// <returns></returns>
        private List<ModInfo> ChangeVersionsForTest(List<ModInfo> availableMods)
        {
            var tmp = availableMods.First(info => info.Id == "SRModsList");
            tmp.Versions.Add(new ModVersion
            {
                DownloadUrl = tmp.Versions[0].DownloadUrl,
                Dependencies = tmp.Versions[0].Dependencies,
                Version = new SemVersion(1, 2)
            });
            return availableMods;
        }

        private string GetCurrentGameVersion()
        {
            return UnityInformationHandler.GameVersion;
        }
    }
}
