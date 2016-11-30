using System;
using System.Linq;
using System.Reflection;
using DiscBot.JSON;
using Discord;
using Discord.Commands;
using System.Threading;
using System.Diagnostics;
using Discord.Modules;
using Discord.Audio;
using System.IO;

namespace DiscBot
{
    public enum PermissionsLevel : byte
    {
        NoAccess = 0,
        Users,
        Mods,
        Admins,
        Owner,
    }
    public class DiscBot
    {
        static void Main() => new DiscBot().Start();

        public static DiscordClient Client { get; private set; }
        public static Channel logChannel { get; private set; }
        public static bool restartFlag { get; set; }
        private configuration _config;
        Player _player = new Player();
        Playlist _playlist = new Playlist();
        private static bool firstStartUp = false;
        private static TimeSpan UpTime { get; set; }
        private static Stopwatch timeSince { get; set; }
        private string prettyCurrentTime => $"【{DateTime.Now:HH:mm:ss}】";
        private string curruntRunningTime => $" [{UpTime:g}] ";
        //public static Credentials Creds { get; set; }
        //public static Config Config { get; set; }

        public void loopRestart()
        {
            restartFlag = false;
            while (restartFlag == false)
            {
                Start();
            }
        }

        public void Start()
        {
            Playlist _playlist = new Playlist();


            startupCheck();

            _config = configuration.LoadFile(Directory.GetCurrentDirectory() + "\\configs\\config.json");

            Client = new DiscordClient(x =>
                {
                    x.LogLevel = LogSeverity.Info;
                    x.ConnectionTimeout = int.MaxValue;
                    x.MessageCacheSize = 10;
                });

            timeSince = new Stopwatch();

            Client.UsingCommands(x =>
            {
                // We need a prefix to separate bots
                x.PrefixChar = '$';
                x.AllowMentionPrefix = true;
                x.HelpMode = HelpMode.Public;
            });
            Client.UsingModules();
            Client.UsingAudio(x =>
            {
                    x.Mode = AudioMode.Outgoing;
                    x.EnableEncryption = true;
                    x.Bitrate = AudioServiceConfig.MaxBitrate;
                    x.BufferLength = 10000;
            });
            


            var commands = Client.GetService<CommandService>();
            Client.GetService<AudioService>();

            commands.CreateCommand("status")
                .Description("Checks the bots status")
                .Do(async (e) =>
                {
                    recheck:
                    if (timeSince.ElapsedMilliseconds <= 0)
                    {
                        timeSince.Start();
                        UpTime = DateTime.Now - Process.GetCurrentProcess().StartTime;
                        await e.Channel.SendMessage("```DiscBot Status: \n" +
                                                    " .NET Version: " + Environment.Version + "\n" +
                                                    " OS Version: " + Environment.OSVersion + "\n" +
                                                    " DiscBot Version: " + Assembly.GetExecutingAssembly().GetName().Version + "\n" +
                                                    " Uptime: " + curruntRunningTime + "\n\n" +
                                                    " Made by Mildaria```");
                    }
                    else if (timeSince.ElapsedMilliseconds > 10000)
                    {
                        // definitely not the most elegant solution, probably need to make a pulse timer to handle events like this in the future
                        timeSince.Reset();
                        goto recheck;
                    }
                });

            commands.CreateCommand("play")
                .AddCheck((cmd, u, ch) => u.Id == 170185463200481280)
                .Alias("play")
                .Parameter("url", ParameterType.Optional)
                .Description("Adds the requested song to the queue.\rExample: !play url\rPermissions: Mods")
                .Do(async (e) =>
                {
                    string str = $"`{prettyCurrentTime}`";
                    if (e.GetArg("url") == "")
                    {
                        await e.Channel.SendMessage(str + $"{e.User.Mention}, Please give me a link so I can play the song for you.");
                        return;
                    }

                    string result = await _playlist.cmd_play(e.GetArg("url"), e.User.Name);

                    await e.Channel.SendMessage(result);
                });

            commands.CreateCommand("summon")
                .AddCheck((cmd, u, ch) => u.Id == 170185463200481280)
                .Alias("summon")
                .Description("Summons bot to current voice channel and starts playing from the library.\rPermission: Everyone")
                .Do(async (e) =>
                    {
                        try
                        {
                            Channel voiceChan = e.User.VoiceChannel;
                            await voiceChan.JoinAudio();
                            await _playlist.startAutoPlayList(voiceChan, Client);
                        }
                        catch (Exception t)
                        {
                            Console.WriteLine(t);
                        }
                    });

            commands.CreateCommand("terminate")
                .AddCheck((cmd, u, ch) => u.Id == 170185463200481280)
                .Do(async (e) =>
                    {
                        string str = $"`{prettyCurrentTime}`";
                        await e.Channel.SendMessage(str + "Goodbye world! (**Shutdown** command issued)");
                        Thread.Sleep(2000);
                        Environment.Exit(0);
                    });



            Client.ChannelUpdated += ChannelUpdated;
            Client.UserLeft += UsrLeft;
            Client.UserJoined += UsrJoined;
            Client.UserBanned += UsrBanned;
            Client.UserUnbanned += UsrUnbanned;
            Client.UserUpdated += UsrUpdtd;
            Client.ServerAvailable += SrvUpdtd;

            Client.ExecuteAndWait(async () =>
            {
                await Client.Connect("MjQ5MDU0MDk5MDM3NzQ5MjUw.CxAtZQ.0PAe-hQifY4yBuz3dPudWEgfk6s", TokenType.Bot);
                Console.Title = "DiscBot";
                Console.WriteLine("DiscBot is now running and has successfully connected");
            });

        }

        private void startupCheck()
        {
            makeCacheFolder();
            makeConfigFolder();
            checkConfigFile();
            checkToken();
            setOwnerID();
            checkCommandPrefix();
        }

        private void makeCacheFolder()
        {
            if (Directory.Exists("cache"))
            {
                return;
            }
            else
            {
                Directory.CreateDirectory("cache");
            }
        }
        private void makeConfigFolder()
        {
            if (Directory.Exists("configs"))
            {
                return;
            }
            else
            {
                Directory.CreateDirectory("configs");
            }
        }

        private void checkConfigFile()
        {
            var configPath = Directory.GetCurrentDirectory() + "\\configs\\config.json";

            try
            {
                if (File.Exists(configPath))
                {
                    _config = configuration.LoadFile(configPath);
                }
                else
                {
                    _config = new configuration();
                    _config.SaveFile(configPath);
                }


            }
            catch
            {
                //unable to find the file
                _config = new configuration();
                _config.SaveFile(configPath);
            }
        }

        private void checkToken()
        {
            //check for the bot token
            try
            {
                _config = configuration.LoadFile(Directory.GetCurrentDirectory() + "\\configs\\config.json");
                if (_config.Token != "")
                {
                    Console.WriteLine("Token has been found in config.json");
                }
                else
                {
                    Console.WriteLine("Please enter a valid token.");
                    Console.Write("Token: ");

                    _config.Token = Console.ReadLine();                     // Read the user's token from the console.
                    _config.SaveFile(Directory.GetCurrentDirectory() + "\\configs\\config.json");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e);
            }
        }

        private void setOwnerID()
        {
            try
            {
                _config = configuration.LoadFile(Directory.GetCurrentDirectory() + "\\configs\\config.json");
                //ulong ownerID = _config.Owner;

                if (Int64.Parse(_config.Owner.ToString()) != 0)
                {
                    Console.WriteLine("Owner ID has been found in config.json");
                }
                else
                {
                    Console.WriteLine("Please enter your user ID to take ownership of this bot.");
                    Console.Write("ID: ");

                    ulong id = Convert.ToUInt64(Console.ReadLine());

                    _config.Owner = id;
                    _config.SaveFile(Directory.GetCurrentDirectory() + "\\configs\\config.json");
                }
            }
            catch (Exception error)
            {
                Console.WriteLine($"Error: {error}");
            }
        }

        private void checkCommandPrefix()
        {
            Console.WriteLine("Current commandPrefix = " + _config.Prefix);
        }
        private async void ChannelUpdated(object sender, ChannelUpdatedEventArgs e)
        {
            //todo permissions / user limit changes are also caught, but currently passed over
            //string str = $"`{prettyCurrentTime}`";
            if (logChannel == null)
                logChannel = e.Server.FindChannels("logs").FirstOrDefault();

            if (e.Before.Name != e.After.Name)  /* (*{e.After.Id}*) */
                await logChannel.SendMessage($@"`{prettyCurrentTime}` **Channel Name Changed** `#{e.Before.Name}`
                                            `New:` {e.After.Name}").ConfigureAwait(false);

            else if (e.Before.Topic != e.After.Topic) /* (*{e.After.Id}*) */
                await logChannel.SendMessage($@"`{prettyCurrentTime}` **Channel Topic Changed** `#{e.After.Name}`
                                            `Old:` {e.Before.Topic} `New:` {e.After.Topic}").ConfigureAwait(false);

            //await logchannel.SendMessage(str + "**" + e.Before.Name + "** channel has been modified");
            Console.WriteLine("[" + e.Server.Name + "] " + e.Before.Name + " channel has been modified");
        }
        private async void ChannelDestroyed(object sender, ChannelEventArgs e)
        {

        }
        private async void SrvUpdtd(object sender, ServerEventArgs e)
        {
            // Triggers upon successfull connection

            // Should probably have this update anytime this event triggers?
            logChannel = e.Server.FindChannels("logs").FirstOrDefault();
            if (!firstStartUp)
            {
                if (logChannel != null)
                {
                    Console.WriteLine("Server update event, log channel identified as: " + logChannel.Name);
                    Console.WriteLine("First update event handled.");
                    await logChannel.SendMessage($@"`{prettyCurrentTime}` Big Brother has arrived. (**Startup** completed)");
                }
                firstStartUp = true;
            }
            else
            {
                if (logChannel != null)
                {
                    Console.WriteLine("Server update event, log channel identified as: " + logChannel.Name);
                    Console.WriteLine("Subsequent update event handled.");
                    await logChannel.SendMessage($@"`{prettyCurrentTime}` Big Brother has returned. (**Reconnected**)");
                }
            }
        }
        private async void ChannelCreated(object sender, ChannelEventArgs e)
        {

        }
        private async void UsrUnbanned(object sender, UserEventArgs e)
        {
            if (logChannel == null)
                logChannel = e.Server.FindChannels("logs").FirstOrDefault();
            await logChannel.SendMessage($"`{prettyCurrentTime}` " + e.User.Mention + " has been unbanned from the server");
            Console.WriteLine("[" + e.Server.Name + "] " + e.User.Name + "#" + e.User.Discriminator + " has been unbanned from the server");
        }
        private async void UsrJoined(object sender, UserEventArgs e)
        {
            if (logChannel == null)
                logChannel = e.Server.FindChannels("logs").FirstOrDefault();
            await logChannel.SendMessage($"`{prettyCurrentTime}`" + e.User.Mention + " has joined the server");
            Console.WriteLine("[" + e.Server.Name + "] " + e.User.Name + "#" + e.User.Discriminator + " has joined the server");
            // should we turn this into a greet bot too?
            //await e.Server.DefaultChannel.SendMessage("Please welcome " + e.User.Mention + " to the server!");
        }
        private async void UsrLeft(object sender, UserEventArgs e)
        {
            if (logChannel == null)
                logChannel = e.Server.FindChannels("logs").FirstOrDefault();
            await logChannel.SendMessage($"`{prettyCurrentTime}` " + e.User.Mention + " has Left the server");
            Console.WriteLine("[" + e.Server.Name + "] " + e.User.Name + "#" + e.User.Discriminator + " has left the server");
        }
        private async void UsrBanned(object sender, UserEventArgs e)
        {
            if (logChannel == null)
                logChannel = e.Server.FindChannels("logs").FirstOrDefault();
            await logChannel.SendMessage($"`{prettyCurrentTime}` " + e.User.Mention + " has been banned from the server");
            Console.WriteLine("[" + e.Server.Name + "] " + e.User.Name + "#" + e.User.Discriminator + " has been banned from the server");
        }
        private async void UsrUpdtd(object sender, UserUpdatedEventArgs e)
        {
            try
            {
                // permission changes, gone offline, changed channel
                // also updates if users deafen/undeafen
                if (logChannel == null)
                    logChannel = e.Server.FindChannels("logs").FirstOrDefault();
                string str = $"`{prettyCurrentTime}`";
                var beforeVch = e.Before.VoiceChannel;
                var afterVch = e.After.VoiceChannel;


                if ((beforeVch != null || afterVch != null) && (beforeVch != afterVch))
                {
                    if (afterVch != null)
                    {
                        if (afterVch.Name == "Raid")
                            str += $"**{e.After.Name}** has joined **{afterVch}** voice channel.";
                        if (beforeVch != null)
                        {
                            if (beforeVch.Name == "Raid" && afterVch.Name != "Raid")
                                str += $"**{e.Before.Name}** has left **{beforeVch}** voice channel.";
                            else
                            {
                                str += $"**{e.After.Name}** has joined **{afterVch}** voice channel.";
                                Console.WriteLine(str);
                                return;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Unhandled trigger in VCH comparison");
                            return;
                        }
                    }

                    else
                        return;
                }
                else if (e.Before.Status == "online" && e.After.Status == "offline")
                    str += $"**{e.Before.Name}** has gone offline.";
                else if (e.Before.Status == "offline" && e.After.Status == "online")
                    str += $"**{e.Before.Name}** has come online.";
                else if (e.Before.Name != e.After.Name)
                    str += $"**Name Changed**`{e.Before?.ToString()}`\n\t\t`New:`{e.After.ToString()}`";
                else if (e.Before.Nickname != e.After.Nickname)
                    str += $"**Nickname Changed**`{e.Before?.ToString()}`\n\t\t`Old:` {e.Before.Nickname}#{e.Before.Discriminator}\n\t\t`New:` {e.After.Nickname}#{e.After.Discriminator}";
                else if (!e.Before.Roles.SequenceEqual(e.After.Roles))
                {
                    if (e.Before.Roles.Count() < e.After.Roles.Count())
                    {
                        var diffRoles = e.After.Roles.Where(r => !e.Before.Roles.Contains(r)).Select(r => "`" + r.Name + "`");
                        str += $"**User's Roles changed ➕**`{e.Before?.ToString()}`\n\tNow has {string.Join(", ", diffRoles)} role.";
                    }
                    else if (e.Before.Roles.Count() > e.After.Roles.Count())
                    {
                        var diffRoles = e.Before.Roles.Where(r => !e.After.Roles.Contains(r)).Select(r => "`" + r.Name + "`");
                        str += $"**User's Roles changed ➖**`{e.Before?.ToString()}`\n\tNo longer has {string.Join(", ", diffRoles)} role.";
                    }
                    else
                    {
                        Console.WriteLine("SEQUENCE NOT EQUAL BUT NO DIFF ROLES - REPORT TO ADMIN");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("Unhandled trigger in general comparison");
                    return;
                }
                if (str != $"`{prettyCurrentTime}`")
                {
                    await logChannel.SendMessage(str);
                    Console.WriteLine(str);
                }
                else
                {
                    // This seems to occur when a user moves from a channel we're not filtering for
                    // Investigate why its not being picked up earlier (beside the fact my code is fugly)
                    //await logChannel.SendMessage("");
                    Console.WriteLine("Final unhandled triggered, investigate if this triggers");
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Error has occured in user update message");
            }
        }
    }
}