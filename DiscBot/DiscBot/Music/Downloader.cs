using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoLibrary;
using System.IO;
using NAudio.Wave;

namespace DiscBot
{
    class Downloader
    {
        public async Task<string[]> download_audio(string url)
        {
            var youtube = YouTube.Default;
            var video = youtube.GetAllVideos(url);

            var audio = video
                .Where(e => e.AudioFormat == AudioFormat.Aac && e.AdaptiveKind == AdaptiveKind.Audio)
                .ToList();

            string workingDir = Directory.GetCurrentDirectory();
            string cachedir = workingDir + "\\cache\\";

            string fileAAC = audio[0].FullName + ".aac";

            if (File.Exists(cachedir + fileAAC))
            {
                Console.WriteLine("Info: URL " + url + " was already downloaded, ignoring");
            }
            else
            {
                if (audio.Count > 0)
                {
                    File.WriteAllBytes(cachedir + fileAAC, audio[0].GetBytes());
                    Console.WriteLine("Info: Downloaded " + url);
                }
            }
            string[] returnVar =
            {
                audio[0].Title,
                fileAAC,
                cachedir + fileAAC,
                audio[0].AudioBitrate.ToString()
            };
            return returnVar;
        }
        public string ConvertAACToWAV(string songTitle, string cacheDir)
        {

            using (MediaFoundationReader reader = new MediaFoundationReader(cacheDir + songTitle + ".aac"))
            using (ResamplerDmoStream resampledReader = new ResamplerDmoStream(reader,
                new WaveFormat(reader.WaveFormat.SampleRate, reader.WaveFormat.BitsPerSample, reader.WaveFormat.Channels)))
            using (WaveFileWriter waveWriter = new WaveFileWriter(cacheDir + songTitle + ".wav", resampledReader.WaveFormat))
            {
                resampledReader.CopyTo(waveWriter);
            }

            return cacheDir + songTitle + ".wav";

        }
        public async Task<string> returnYoutubeTitle(string url)
        {
            var youtube = YouTube.Default;
            var video = youtube.GetAllVideos(url);
            var videoList = video.ToList();

            return videoList[0].Title;
        }
    }
}
