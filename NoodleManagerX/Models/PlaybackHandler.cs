//using NAudio.Wave;
using ManagedBass;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace NoodleManagerX.Models
{
    static class PlaybackHandler
    {
        public static MapItem currentlyPlaying;
        private static YoutubeClient youtubeClient = new YoutubeClient();
        private const string regex = @"^.*((youtu\.be\/)|(v\/)|(\/u\/\w\/)|(embed\/)|(watch\?))\??v?=?([^#&?]*).*";
        private static Int32 channel = -1;
        public static int requestCounter = 0;

        static PlaybackHandler()
        {
            Bass.Init();
            string plugin = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "libbass_aac.so");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Bass.PluginLoad(plugin);
            }
        }

        public static void Play(MapItem item)
        {
            Task.Run(async () =>
            {
                if (currentlyPlaying == item)
                {
                    Stop();
                    return;
                }
                string url = item.youtube_url;
                if (!String.IsNullOrEmpty(url))
                {
                    GroupCollection matches = Regex.Match(url, regex).Groups;
                    string id = "RGCdPICdwko";//matches[matches.Count - 1].Value;

                    if (currentlyPlaying != null) Stop();
                    currentlyPlaying = item;
                    item.playing = true;
                    MapItem playing = currentlyPlaying;

                    try
                    {
                        requestCounter++;
                        int requestID = requestCounter;
                        var streamManifest = await youtubeClient.Videos.Streams.GetManifestAsync(id); ;
                        var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                        using (Stream youtubeStream = await youtubeClient.Videos.Streams.GetAsync(streamInfo))
                        {
                            if (requestID == requestCounter)
                            {
                                byte[] b;
                                using (BinaryReader br = new BinaryReader(youtubeStream))
                                {
                                    b = br.ReadBytes((int)youtubeStream.Length);
                                }
                                channel = Bass.CreateStream(b, 0, b.Length, BassFlags.Default);
                                if (channel == 0)
                                {
                                    Console.WriteLine("Failed to create channel: " + Bass.LastError);
                                }
                                Bass.ChannelPlay(channel);
                                long length = Bass.ChannelGetLength(channel);
                                while (Bass.ChannelIsActive(channel) == PlaybackState.Playing)
                                {
                                    await Task.Delay(100);
                                }
                            }
                            StopPlaying(playing);
                        }
                    }
                    catch (Exception e)
                    {
                        MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
                        Stop();
                    }
                }
            });
        }

        public static void Stop()
        {
            if (channel != -1)
            {
                try
                {
                    Bass.ChannelPause(channel);
                }
                catch (Exception e)
                {
                    MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
                }
            }
            StopPlaying(currentlyPlaying);
        }

        private static void StopPlaying(MapItem playing)
        {
            if (playing != null) playing.playing = false;
            if (currentlyPlaying == playing)
            {
                currentlyPlaying = null;
                requestCounter++;
            }
        }
    }
}
