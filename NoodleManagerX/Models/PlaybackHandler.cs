//using NAudio.Wave;
using ManagedBass;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoutubeExplode;

namespace NoodleManagerX.Models
{
    static class PlaybackHandler
    {
        private static MapItem currentlyPlaying;
        private static YoutubeClient youtubeClient = new YoutubeClient();
        private const string regex = @"^.*((youtu\.be\/)|(v\/)|(\/u\/\w\/)|(embed\/)|(watch\?))\??v?=?([^#&?]*).*";
        private static Int32 channel = -1;
        private static int requestCounter = 0;
        private static string[] plugins = new string[] { };

        static PlaybackHandler()
        {
            Bass.Init();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                foreach (string plugin in plugins)
                {
                    string location = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), plugin);
                    Console.WriteLine("Loading Plugin from: " + location);
                    int res = Bass.PluginLoad(location);
                    if (res == 0)
                    {
                        Console.WriteLine("Error loading Plugin: " + Bass.LastError);
                    }
                    else
                    {
                        foreach (var format in Bass.PluginGetInfo(res).Formats)
                        {
                            Console.Write(format.ChannelType + " " + format.Name);
                        }
                    }
                }
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
                    string id = matches[matches.Count - 1].Value;

                    if (currentlyPlaying != null) Stop();
                    currentlyPlaying = item;
                    item.playing = true;
                    MapItem playing = currentlyPlaying;

                    try
                    {
                        requestCounter++;
                        int requestID = requestCounter;

                        var streamManifest = await youtubeClient.Videos.Streams.GetManifestAsync(id);

                        var streamInfo = streamManifest.GetAudioOnlyStreams().OrderBy(x => x.Bitrate).FirstOrDefault();

                        //This makes no fcking sense but it doesn't work without it. Thanks Bass
                        DownloadProcedure Procedure = (IntPtr a, int b, IntPtr c) => { };

                        channel = Bass.CreateStream(streamInfo.Url, 0, BassFlags.Default, Procedure);

                        if (channel == 0)
                        {
                            Console.WriteLine("Failed to create channel: " + Bass.LastError);
                        }

                        Bass.ChannelPlay(channel);

                        while (Bass.ChannelIsActive(channel) == PlaybackState.Playing)
                        {
                            await Task.Delay(100);
                        }
                        StopPlaying(playing);
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
