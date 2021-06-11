using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
        private static WaveOut waveOut;

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

                    try
                    {
                        var streamManifest = await youtubeClient.Videos.Streams.GetManifestAsync(id);
                        var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                        using (Stream youtubeStream = await youtubeClient.Videos.Streams.GetAsync(streamInfo))
                        using (Stream ms = new MemoryStream())
                        {
                            byte[] buffer = new byte[32768];
                            int read;
                            while ((read = youtubeStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                ms.Write(buffer, 0, read);
                            }

                            ms.Position = 0;
                            using (WaveStream blockAlignedStream =
                                new BlockAlignReductionStream(
                                    WaveFormatConversionStream.CreatePcmStream(
                                        new StreamMediaFoundationReader(ms))))
                            {
                                using (WaveOut audioOut = new WaveOut(WaveCallbackInfo.FunctionCallback()))
                                {
                                    waveOut = audioOut;
                                    audioOut.Init(blockAlignedStream);
                                    audioOut.PlaybackStopped += (sender, e) =>
                                    {
                                        Stop();
                                    };

                                    audioOut.Play();
                                    while (audioOut.PlaybackState == PlaybackState.Playing)
                                    {
                                        await Task.Delay(100);
                                    }
                                }
                            }
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
            if (waveOut != null)
            {
                try
                {
                    waveOut.Stop();
                }
                catch (Exception e)
                {
                    MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
                }
            }
            if (currentlyPlaying != null) currentlyPlaying.playing = false;
            currentlyPlaying = null;
        }
    }
}
