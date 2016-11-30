using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using NAudio;
using NAudio.Wave;
using System.Threading.Tasks;
using System.IO;

namespace DiscBot
{
    class Player
    {
        private IAudioClient _nAudio;
        private configuration _config;

        public static bool playingSong { get; set; }
        private float volume = .3f;

        public async Task SendAudio(string filepath, Channel voiceChannel, DiscordClient _client)
        {
            _nAudio = await _client.GetService<AudioService>().Join(voiceChannel);

            playingSong = true;

            try
            {
                using (_client.GetService<AudioService>().Join(voiceChannel))
                {
                    var channelCount = _client.GetService<AudioService>().Config.Channels;
                    var outFormat = new WaveFormat(48000, 16, channelCount);

                    using (var MP3Reader = new MediaFoundationReader(filepath))
                    {
                        using (var resampler = new MediaFoundationResampler(MP3Reader, outFormat))
                        {
                            resampler.ResamplerQuality = 60;
                            int blockSize = outFormat.AverageBytesPerSecond/50;
                            byte[] buffer = new byte[blockSize];
                            int byteCount;

                            while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0 && playingSong == true)
                            {
                                if (playingSong == false)
                                    _nAudio.Clear();

                                else
                                {
                                    if (byteCount < blockSize)
                                    {
                                        for (int i = byteCount; i < blockSize; i++)
                                            buffer[i] = 0;
                                    }
                                    _nAudio.Send(buffer, 0, blockSize);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Somethings broken spencer! dump: " + e);
                throw;
            }

            _client.Dispose(); // This might be a bad idea, we'll see.
        }


        public float VolumeReturn()
        {
            return _config.volume;
        }
        public bool cmd_skip()
        {
            if (playingSong)
            {
                playingSong = false;
                return true;
            }
            else
            {
                return false;
            }
        }
        public async Task<bool> cmd_stop()
        {
            if (Playlist.libraryLoop == true)
            {

                //breaks the loop
                Playlist.libraryLoop = false;

                //forces the current track playing to send the stop command.
                playingSong = false;

                return true;
            }
            else
            {
                //not doing anything for a reason.
                return false;
            }
        }
        public bool cmd_resume()
        {
            //the autoplayer is turned off
            try
            {
                if (Playlist.libraryLoop == false)
                {
                    //turn it back on.
                    //playlist _playlist = new playlist();
                    Playlist.libraryLoop = true;
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: player.cmd_resume generated a error.  Dump: {e}");
                return false;
            }


            return false;
        }
    }
}
