using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Reactive;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using MemoryStream = System.IO.MemoryStream;
using Path = System.IO.Path;
using Stream = System.IO.Stream;

namespace NoodleManagerX.Models
{
    class MapHandler : GenericHandler
    {
        public override ItemType itemType { get; set; } = ItemType.Map;
        public override Dictionary<string, string> queryFields { get; set; } = new Dictionary<string, string>() { { "text_search", "$tsQuery" }, { "user.username", "$contL" } };
        public override string select { get; set; } = "title,artist,mapper,duration,difficulties,hash,youtube_url,beat_saber_convert";
        public override string selectDownload { get; set; } = "hash,beat_saber_convert";
        public override string apiEndpoint { get; set; } = "https://synthriderz.com/api/beatmaps";
        public override string folder { get; set; } = "CustomSongs";
        public override string[] extensions { get; set; } = { ".synth" };

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
                            if (entry.FullName == "synthriderz.meta.json")
                            {
                                using (System.IO.StreamReader sr = new System.IO.StreamReader(entry.Open()))
                                {
                                    LocalItem localItem = JsonConvert.DeserializeObject<LocalItem>(await sr.ReadToEndAsync());
                                    localItem.filename = Path.GetFileName(path);
                                    localItem.modifiedTime = StorageAbstraction.GetLastWriteTime(path);
                                    localItem.itemType = ItemType.Map;
                                    MainViewModel.s_instance.localItems.Add(localItem);
                                    return true;
                                }
                            }
                        }

                        if (MainViewModel.s_instance.pruning)
                        {
                            MainViewModel.Log("Deleting old file without metadata " + Path.GetFileName(path));
                            StorageAbstraction.DeleteFile(path);
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

        public override dynamic DeserializePage(string json)
        {
            return JsonConvert.DeserializeObject<MapPage>(json);
        }
    }

    [DataContract]
    class MapItem : GenericItem
    {
        public override string target { get; set; } = "CustomSongs";
        public override ItemType itemType { get; set; } = ItemType.Map;

        [DataMember] public string title { get; set; }
        [DataMember] public string artist { get; set; }
        [DataMember] public string mapper { get; set; }
        [DataMember] public string duration { get; set; }
        [DataMember] public string[] difficulties { get; set; }
        [DataMember] public string hash { get; set; }
        [DataMember] public string youtube_url { get; set; }
        [Reactive] public bool playing { get; set; } = false;
        public ReactiveCommand<Unit, Unit> playPreviewCommand { get; set; }

        public override string display_title
        {
            get { return title; }
        }
        public override string display_creator
        {
            get { return mapper; }
        }
        public override string display_preview
        {
            get { return youtube_url; }
        }
        public override string[] display_difficulties
        {
            get { return difficulties; }
        }

        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            if (PlaybackHandler.currentlyPlaying?.filename == filename)
            {
                playing = PlaybackHandler.currentlyPlaying.playing;
                PlaybackHandler.currentlyPlaying = this;
            }

            playPreviewCommand = ReactiveCommand.Create((() =>
            {
                PlaybackHandler.Play(this);
            }));
        }

        public override async Task<MemoryStream> CopyStreamToMemoryStream(Stream stream, bool closeOriginal = true)
        {
            MemoryStream ms = await base.CopyStreamToMemoryStream(stream);

            using (ZipArchive archive = new ZipArchive(ms, ZipArchiveMode.Update, true))
            {
                foreach (ZipArchiveEntry zipEntry in archive.Entries)
                {
                    if (zipEntry.FullName == "synthriderz.meta.json")
                    {
                        return ms;
                    }
                }

                MainViewModel.Log("Creating metadata for " + filename);
                JObject metadata = new JObject(new JProperty("id", id), new JProperty("hash", hash));

                ZipArchiveEntry entry = archive.CreateEntry("synthriderz.meta.json");
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(entry.Open()))
                {
                    writer.Write(metadata.ToString(Formatting.None));
                }

                return ms;
            }
        }
    }

#pragma warning disable 0649
    class MapPage : GenericPage
    {
        public List<MapItem> data;
    }
#pragma warning restore 0649
}
