﻿using Avalonia.Threading;
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

namespace NoodleManagerX.Mods
{
    class ModHandler : GenericHandler
    {
        public override ItemType itemType { get; set; } = ItemType.Mod;
        public override string apiEndpoint { get; set; } = "https://raw.githubusercontent.com/bookdude13/SRModsList/dev/SynthRiders";
        public override string folder { get; set; } = "Mods";

        // Save mod information in local file, to fake out same structure as other *Items
        // Needs this to call Deserialize function so the downloadCommand is initialized

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
                // TODO check if in local db, and if so use version from there.
                // Default is to assume latest (null) and resolve with that
                localSelection.Add(new ModVersionSelection(mod.Id, null));
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
                    mod => new ModItem(mod, null, dependencyGraph.ResolvedVersions[mod.Id].Version)
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
