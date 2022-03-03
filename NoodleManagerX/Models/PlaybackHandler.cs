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
        public static MapItem currentlyPlaying;
        private static YoutubeClient youtubeClient = new YoutubeClient();
        private const string regex = @"^.*((youtu\.be\/)|(v\/)|(\/u\/\w\/)|(embed\/)|(watch\?))\??v?=?([^#&?]*).*";
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
                        var streamManifest = await youtubeClient.Videos.Streams.GetManifestAsync(id);

                        var streamInfo = streamManifest.GetAudioOnlyStreams().OrderBy(x => x.Bitrate).FirstOrDefault();

                        using (var mf = new MediaFoundationReader(streamInfo.Url))
                        using (waveout = new WaveOutEvent())
                        {
                            waveout.Init(mf);
                            SetVolume(MainViewModel.s_instance.previewVolume);
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

        public static void SetVolume(int volume)
        {
            if (waveout != null)
            {
                waveout.Volume = volume / 100f;
            }
        }

        public static void Stop()
        {

            try
            {
                if (waveout != null)
                {
                    waveout.Stop();
                }
            }
            catch (Exception e)
            {
                MainViewModel.Log(MethodBase.GetCurrentMethod(), e);
            }
            finally
            {
                StopPlaying(currentlyPlaying);
            }
        }

        private static void StopPlaying(MapItem playing)
        {
            if (playing != null) playing.playing = false;
            if (currentlyPlaying == playing)
            {
                currentlyPlaying = null;
            }
        }
    }
}
