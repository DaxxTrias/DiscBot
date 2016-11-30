using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Discord;
using Discord.Modules;
using Newtonsoft.Json;

namespace DiscBot
{
    public class ListPlaylist
    {
        public string title { get; set; }
        public string user { get; set; }
        public string url { get; set; }
        public string[] like { get; set; }
        public string[] skips { get; set; }
    }

    class Playlist
    {
        public static List<ListPlaylist> listLibrary = new List<ListPlaylist>();
        public static List<ListPlaylist> listBlacklist = new List<ListPlaylist>();
        public static List<ListPlaylist> listQueue = new List<ListPlaylist>();
        public static List<ListPlaylist> listSubmitted = new List<ListPlaylist>();
        public static List<ListPlaylist> listBeenPlayed = new List<ListPlaylist>();
        public static string npTitle { get; set; }
        public static string npUser { get; set; }
        public static string npUrl { get; set; }
        public static string npSource { get; set; }
        public static string[] npLike { get; set; }
        public static string[] npSkip { get; set; }

        public static bool libraryLoop { get; set; }

        private DiscordClient _client;
        private ModuleManager _manager;


        configuration _config = new configuration();
        Downloader _downloader = new Downloader();
        Player _player = new Player();

        public async Task<string> cmd_play(string url, string user)
        {
            try
            {
                string title = await _downloader.returnYoutubeTitle(url);

                listSubmitted.Add(new ListPlaylist
                {
                    title = title,
                    url = url,
                    user = user
                });

                int total = listSubmitted.Count;

                int position = listSubmitted.FindIndex(x => x.url == url);

                string value = $"Your request is song number {position + 1}/{total}";

                return value;
            }
            catch (Exception e)
            {
                //got a erro
                Console.WriteLine($"Error with playlist.cmd_play.  Dump: {e}");
                return null;
            }
        }

        public async Task startAutoPlayList(Channel voiceChannel, DiscordClient _client)
        {
            loadPlaylist();
            loadBlacklist();

            libraryLoop = true;
            while (libraryLoop == true)
            {
                bool result = getTrack();

                if (result == false)
                {
                    //reroll
                }
                else
                {
                    //pass off to download the file for cache
                    string[] file = await _downloader.download_audio(npUrl);

                    _client.SetGame(npTitle);

                    await _player.SendAudio(file[2], voiceChannel, _client);

                    //if a user submitted the song remove it from the disk
                    if (npSource == "Submitted")
                    {
                        File.Delete(file[2]);
                        removeTrackSubmitted(npUrl);
                    }

                    addBeenPlayed(npTitle, npUrl);

                }
            }
        }
        private bool getTrack()
        {
            string title = null;
            string user = null;
            string url = null;
            string source = null;

            //1. check if something has been submitted by a user
            string[] trackSubmitted = getTrackSubmitted();

            if (trackSubmitted != null)
            {
                //we have a user file to play
                title = trackSubmitted[0];
                user = trackSubmitted[1];
                url = trackSubmitted[2];
                source = trackSubmitted[3];
            }
            else
            {
                //2. Pick from the Library
                string[] trackLibrary = getTrackLibrary();
                title = trackLibrary[0];
                user = trackLibrary[1];
                url = trackLibrary[2];
                source = trackLibrary[3];
            }

            //3. Check to see if it was blacklisted
            bool blacklist = checkBlacklist(title, url);
            if (blacklist == true)
            {
                //we found a match in the blacklist, need to reroll
                return false;
            }

            //4. Check to see if has been played already
            bool beenPlayed = checkBeenPlayed(title, url);
            if (beenPlayed == true)
            {
                //found a match in the beenPlayed list, need to reroll
                return false;
            }

            //5 Need to check Likes

            //6 Need to check skips

            //7. Return the value back to be submiited to queue
            npTitle = title;
            npUser = user;
            npUrl = url;
            npSource = source;
            return true;
        }

        public bool removeTrackSubmitted(string url)
        {
            try
            {
                var urlResult = listSubmitted.FindIndex(x => x.url == url);
                if (urlResult != -1)
                {
                    //remove the track from the list
                    listSubmitted.RemoveAt(urlResult);
                    return true;
                }
                else
                {
                    // shouldnt even hit this but you know do nothing
                    return false;
                }
            }
            catch
            {
                // something broke removing a track
                return false;
            }

        }

        public string[] getTrackLibrary()
        {
            Random rng = new Random();
            int counter = rng.Next(0, listLibrary.Count);

            string[] value = { listLibrary[counter].title, listLibrary[counter].user, listLibrary[counter].url, "Library" };

            return value;
        }

        public string[] getTrackSubmitted()
        {
            // extract the values
            if (listSubmitted.Count >= 1)
            {
                string[] value = { listSubmitted[0].title, listSubmitted[0].user, listSubmitted[0].url, "Submitted" };
                return value;
            }
            else
            {
                return null;
            }

        }

        private void addBeenPlayed(string title, string url)
        {
            //get the 10% of the library
            double threshold = listLibrary.Count * 0.1;

            Console.WriteLine("Debug: beenPlayed threshhold " + threshold);

            //get the count of items in listBeenPlayed
            if (listBeenPlayed.Count >= threshold)
            {
                //delete the first object
                listBeenPlayed.RemoveAt(0);
            }

            listBeenPlayed.Add(new ListPlaylist
            {
                title = title,
                url = url
            });

        }

        private bool checkBlacklist(string title, string url)
        {
            //check to make sure it wasnt in the blacklist
            //var titleResult = listBlacklist.Find(x => x.title == title);
            var urlResult = listBlacklist.Find(x => x.url == url);

            //if not null, we found a match on the name or the url
            //using try catch given when it parses a null value it hard errors, this catches it and returns the value
            try
            {
                if (urlResult.url != null)
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }

        }

        public void savePlaylist()
        {
            try
            {
                string loc = Directory.GetCurrentDirectory() + "\\configs\\playlist.json";
                string json = JsonConvert.SerializeObject(listLibrary);

                if (!File.Exists(loc))
                    File.Create(loc).Close();

                File.WriteAllText(loc, json);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error saving playlist.json.  Error: " + e);
            }
        }

        public void loadPlaylist()
        {
            try
            {
                if (File.Exists(Directory.GetCurrentDirectory() + "\\configs\\playlist.json"))
                {
                    string json = File.ReadAllText(Directory.GetCurrentDirectory() + "\\configs\\playlist.json");

                    listLibrary = JsonConvert.DeserializeObject<List<ListPlaylist>>(json);
                }
                else
                {
                    savePlaylist();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading playlist.json.  Error: " + e);
            }
        }

        public void saveBlacklist()
        {
            try
            {
                string loc = Directory.GetCurrentDirectory() + "\\configs\\blacklist.json";
                string json = JsonConvert.SerializeObject(listBlacklist, Formatting.Indented);

                if (!File.Exists(loc))
                    File.Create(loc).Close();

                File.WriteAllText(loc, json);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error saving blacklist.json. Error: " + e);
            }

        }

        public void loadBlacklist()
        {
            try
            {
                if (File.Exists(Directory.GetCurrentDirectory() + "\\configs\\blacklist.json"))
                {
                    string json = File.ReadAllText(Directory.GetCurrentDirectory() + "\\configs\\blacklist.json");

                    listBlacklist = JsonConvert.DeserializeObject<List<ListPlaylist>>(json);
                }
                else
                {
                    saveBlacklist();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading blacklist.json.  Error: " + e);
            }
        }

        private bool checkBeenPlayed(string title, string url)
        {
            //check to make sure it wasnt in the beenPlayed list
            var urlResult = listBeenPlayed.Find(x => x.url == url);

            //if not null, we found a match on the name or the url
            //using try catch given when it parses a null value it hard errors, this catches it and returns the value
            try
            {
                if (urlResult.url != null)
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }

        }

    }
}
