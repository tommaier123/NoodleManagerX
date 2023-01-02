using Avalonia.Threading;
using DynamicData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace NoodleManagerX.Models.Playlists
{
    class PlaylistHandler : GenericHandler
    {
        public override ItemType itemType { get; set; } = ItemType.Playlist;
        public override string join { get; set; } = "items||id";
        public override string apiEndpoint { get; set; } = "https://synthriderz.com/api/playlists";
        public override string folder { get; set; } = "Playlist";
        public override string[] extensions { get; set; } = { ".playlist" };

        public override dynamic DeserializePage(string json)
        {
            return JsonConvert.DeserializeObject<PlaylistPage>(json);
        }

        public override Task LoadLocalItems()
        {
            return base.LoadLocalItems();
        }

        private PlaylistFile? GetPlaylistFileFromPath(string path)
        {
            try
            {
                using Stream stream = StorageAbstraction.ReadFile(path);
                using StreamReader contents = new StreamReader(stream);
                JsonSerializer serializer = new JsonSerializer();
                PlaylistFile playlist = (PlaylistFile)serializer.Deserialize(contents, typeof(PlaylistFile));
                return playlist;
            }
            catch (Exception e) { MainViewModel.Log(MethodBase.GetCurrentMethod(), e); }

            return null;
        }

        public override Task<bool> GetLocalItem(string path)
        {
            try
            {
                if (StorageAbstraction.FileExists(path))
                {
                    PlaylistFile? playlist = GetPlaylistFileFromPath(path);
                    if (playlist == null)
                    {
                        MainViewModel.Log($"Failed to load playlist {path} from file");
                        return Task.FromResult(false);
                    }

                    var localItem = new LocalItem(-1, playlist.Value.Name, Path.GetFileName(path), StorageAbstraction.GetLastWriteTime(path), itemType);

                    MainViewModel.s_instance.localItems.Add(localItem);
                    return Task.FromResult(true);
                }
            }
            catch (Exception e) { MainViewModel.Log(MethodBase.GetCurrentMethod(), e); }
            return Task.FromResult(false);
        }
    }

#pragma warning disable 0649
    class PlaylistPage : GenericPage
    {
        public List<PlaylistItem> data;
    }
#pragma warning restore 0649

    struct PlaylistNameFile
    {
        public string Name { get; set; }
        public string FilePath { get; set; }
    }
}

