using NAudio.Wave;
using System;
using System.Linq;
using System.Reflection;
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
        private static int requestCounter = 0;
        private static WaveOutEvent waveout;

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

                        using (var mf = new MediaFoundationReader(streamInfo.Url))
                        using (waveout = new WaveOutEvent())
                        {
                            waveout.Init(mf);
                            waveout.Play();
                            while (waveout.PlaybackState == PlaybackState.Playing)
                            {
                                await Task.Delay(100);
                            }
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

            try
            {
                waveout.Stop();
            }
            catch (Exception e)
            {
                MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
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
