using Avalonia.Threading;
using DynamicData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NoodleManagerX.Models;
using NoodleManagerX.Models.Mods;
using NoodleManagerX.Utils;
using System;
using System.Collections.Generic;
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
        public override string[] extensions { get; set; } = { ".dll" };

        public override dynamic DeserializePage(string json)
        {
            return JsonConvert.DeserializeObject<ModPage>(json);
        }

        public async override Task GetPage()
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
                        return;
                    };

                    var mods = ParseRemoteModList(rawResponse);

/*                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        MainViewModel.s_instance.items.AddRange(modItems);
                    });*/

                    //parallel.foreach is not necessary but seems more robust against changing collections, maybe with the list copy no more errors?
                    /*Parallel.ForEach(new List<GenericItem>(MainViewModel.s_instance.items), item =>
                    {
                        _ = item.UpdateDownloaded(MainViewModel.s_instance.downloadPage);
                    });*/
                }
            }
            catch (Exception e)
            {
                MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
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
