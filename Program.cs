using System;
using System.Collections.Generic;
//using System.Linq;
//using System.Text;
using System.Threading.Tasks;
using Discord;
//using DiscordBot;
using Discord.WebSocket;
//using Discord.Net;
using Discord.Rest;
//using Discord.Webhook;
using Discord.Commands;
//using Discord.Rpc;
//using Discord.Net.WebSockets;
using System.IO;
//using System.Windows.Forms;
using System.Net;
using System.Diagnostics;
//using System.Web.Services.Description;
using Discord.Audio;
using Microsoft.Extensions.DependencyInjection;
using DiscordExtensions;

namespace DiscordBot
{
    public class Config
    {
        public string path;
        public Property swearResponse = new Property("swear-response");
        public Property swearChannelName = new Property("swear-jar-channel");
        public Property nukedRoleName = new Property("nuked-role-name");
        public Property banishedRoleName = new Property("banished-role-name");
        public Property blacklistedUsersFile = new Property("blacklist-file");
        public Property bannedLinksFile = new Property("banned-links-file");
        public Property helpPath = new Property("help-file");
        public Property swearsPath = new Property("swears-file");
        public Property answersPath = new Property("answers-file");
        public Property questionsPath = new Property("questions-file");
        public Property botToken = new Property("bot-token");
        public Property VClogChannel = new Property("vc-log-channel-name");
        public Property pmSwearMsg = new Property("pm-swear-message");
        public Property enableAnswerback = new Property("enable-snark");
        public Property enableSwearjar = new Property("enable-swearjar");
        public string helpMessage;
        public string dirSeperator;
        public Property[] properties;
        public Config(string path)
        {
            this.path = path;
            properties = new Property[] {
                swearResponse,
                swearChannelName,
                nukedRoleName,
                banishedRoleName,
                blacklistedUsersFile,
                bannedLinksFile,
                helpPath,
                swearsPath,
                answersPath,
                questionsPath,
                botToken,
                pmSwearMsg,
                enableAnswerback,
                enableSwearjar,
                VClogChannel
            };
            string[] lines = File.ReadAllLines(path);
            foreach (string line in lines)
            {
                foreach (Property property in properties)
                {
                    if (line.StartsWith(property.name))
                    {
                        property.value = line.Substring((property.name + ": ").Length);
                        property.parent = this;
                    }
                }
            }
            dirSeperator = GetLocalisedSeperator();
            helpMessage = File.ReadAllText(Path.GetDirectoryName(path) + dirSeperator + helpPath.value);
        }
        //determine what form of slash should be used on the current OS - very .NET core
        public static string GetLocalisedSeperator()
        {
            string windir = Environment.GetEnvironmentVariable("windir");
            if (!string.IsNullOrEmpty(windir) && windir.Contains(@"\") && Directory.Exists(windir))
            {
                return "\\";
            }
            else if (File.Exists(@"/proc/sys/kernel/ostype"))
            {
                string osType = File.ReadAllText(@"/proc/sys/kernel/ostype");
                if (osType.StartsWith("Linux", StringComparison.OrdinalIgnoreCase))
                {
                    return "/";
                }
            }
            throw new Exception("Unkown OS");
        }
    }
    public class Property
    {
        public Config parent;
        public string name;
        public string value;
        public Property(string propertyName, string propertyValue = null)
        {
            name = propertyName;
            value = propertyValue;
        }
        public void StoreValue(string value)
        {
            this.value = value;
            string[] lines = File.ReadAllLines(parent.path);
            for (int i = 0; i <= lines.Length; i++)
            {
                if (lines[i].StartsWith(name))
                {
                    lines[i] = name + ": " + value;
                    break;
                }
            }
            File.WriteAllLines(parent.path, lines);
        }
    }
    class Program
    {
        DiscordSocketClient client;
        string[] swears;
        string[] answers;
        string[] bannedLinks = { };
        string[] fuckheads;
        string[] questionStarts;
        //string dirSeperator;
        CommandService commandService;
        private IServiceProvider services;
        Config config = null;
        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        

        //lastArgValue is the number of args after which to ignore spaces
        //public string[] GetArgArray(string command, int argNo = int.MaxValue)
        //{
        //    List<string> list = new List<string>();
        //    int i = 0;
        //    while (i < command.Length)
        //    {
        //        string nextArg = "";
        //        while (i < command.Length)
        //        {
        //            if (list.Count < argNo -1 && command.Substring(i, 1) == " ")
        //            {
        //                break;
        //            }
        //            nextArg += command.Substring(i, 1);
        //            i++;
        //        }
        //        i++;
        //        list.Add(nextArg);
        //    }
        //    return list.ToArray();
        //}
        //checks if a user has a permission in a channel - this should be built in imo
        //permissionName must be the exact name of the property of OverwritePermissions
        //public PermValue CheckUserPermissions(SocketGuildUser user, SocketGuildChannel channel, string permissionName)
        //{
        //    //if the user is admin, then they have all permissions by default
        //    if (user.GuildPermissions.Administrator)
        //    {
        //        return PermValue.Allow;
        //    }
        //    var overwrites = channel.PermissionOverwrites;
        //    var roles = new List<SocketRole>();
        //    foreach (Overwrite overwrite in overwrites)
        //    {
        //        //user overwrites override role overwrites, so if we find a user overwrite then we can just return it
        //        if (overwrite.TargetType == PermissionTarget.User && overwrite.TargetId == user.Id)
        //        {
        //            PermValue value = (PermValue)GetProperty(overwrite.Permissions, permissionName);
        //            //check if it's an actual value or just marked as inherit
        //            if (value != PermValue.Inherit)
        //            {
        //                return value;
        //            }
        //        }
        //        if (overwrite.TargetType == PermissionTarget.Role)
        //        {
        //            foreach(SocketRole role in user.Roles)
        //            {
        //                //check that the user has this role
        //                if (role.Id == overwrite.TargetId)
        //                {
        //                    roles.Add(role);
        //                }
        //            }
        //        }
        //    }
        //    //sort by position
        //    roles.Sort((x, y) => x.Position.CompareTo(y.Position));
        //    //scan down the list so that the higher position roles take precedence
        //    for (int i = 0; i < roles.Count; i++)
        //    {
        //        //I don't even know, the damn thing appears to have decided to become boolean for whatever reason
        //        //this fixes that. I think
        //        int rawValue = Convert.ToInt16(GetProperty(roles[i].Permissions, permissionName));
        //        PermValue value = (PermValue)rawValue;
        //        if (value != PermValue.Inherit)
        //        {
        //            return value;
        //        }
        //    }
        //    //no references or all marked as inherit - just return the default @everyone permission
        //    SocketRole everyone = channel.Guild.EveryoneRole;
        //    return (PermValue)GetProperty(everyone.Permissions, permissionName);
        //}
        //returns the value of the property called name belonging to obj
        //private object GetProperty(object obj, string name)
        //{
        //    var properties = obj.GetType().GetProperties();
        //    foreach(System.Reflection.PropertyInfo property in properties)
        //    {
        //        if (property.Name == name)
        //        {
        //            return property.GetValue(obj);
        //        }
        //    }
        //    throw new Exception("Property not found");
        //}
        public string GetDirectory(string filePath, string directorySeperator)
        {
            for (int i = filePath.Length - 1; i >= 0; i--)
            {
                if (filePath[i] == Convert.ToChar(directorySeperator))
                {
                    return filePath.Substring(0, i);
                }
            }
            throw new Exception("Invalid path");
        }

        //checks whether a string matches the form <start>blahblah"?"blahblah
        public bool IsQuestion(string message)
        {
            foreach (string start in questionStarts)
            {
                if (message.ToLower().StartsWith(start.ToLower()))
                {
                    for (int i = 0; i <= message.Length; i++)
                    {
                        if (message[i] == Convert.ToChar("."))
                        {
                            return false;
                        }
                        else if (message[i] == Convert.ToChar("?"))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
            return false;
        }
        [STAThread]
        public async Task MainAsync()
        {
            //determine what OS we're running on
            string dirSeperator = Config.GetLocalisedSeperator();

            client = new DiscordSocketClient();

            client.Log += Log;
            string configPath = GetDirectory(Environment.GetCommandLineArgs()[0], dirSeperator) + dirSeperator + "config" + dirSeperator;

            //load all the config files at startup
            config = new Config(configPath + "bot-config.txt");
            bannedLinks = File.ReadAllLines(configPath + config.bannedLinksFile.value);
            fuckheads = File.ReadAllLines(configPath + config.blacklistedUsersFile.value);
            swears = File.ReadAllLines(configPath + config.swearsPath.value);
            answers = File.ReadAllLines(configPath + config.answersPath.value);
            questionStarts = File.ReadAllLines(configPath + config.questionsPath.value);

            await Log(new LogMessage(LogSeverity.Info, "Bot", "Finished loading config files"));

            Commands.config = config;
            commandService = new CommandService();

            services = new ServiceCollection().BuildServiceProvider();

            await client.LoginAsync(TokenType.Bot, config.botToken.value);
            await client.StartAsync();

            client.MessageReceived += Client_MessageReceived;
            client.MessageUpdated += Client_MessageUpdated;
            client.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;

            await commandService.AddModulesAsync(System.Reflection.Assembly.GetEntryAssembly());

            await (Task.Delay(-1));
        }

        private async Task Client_MessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
        {
            
        }

        private async Task Client_UserVoiceStateUpdated(SocketUser user, SocketVoiceState from, SocketVoiceState to)
        {
            //await (user as SocketGuildUser).Guild.DefaultChannel.SendMessageAsync(user.Username + " has updated from: " + from + " to " + to + " I think...");
            string msg = "";
            if (to.VoiceChannel == null)
            {
                msg = "*" + user.Username + "*" + " has left " + from.VoiceChannel.Name;
            }
            else if (from.VoiceChannel == null)
            {
                msg = "*" + user.Username + "*" + " has joined " + to.VoiceChannel.Name;
            }
            else
            {
                msg = "*" + user.Username + "*" + " has moved from " + from.VoiceChannel.Name + " to " + to.VoiceChannel.Name;
            }
            var guild = (user as SocketGuildUser).Guild;
            foreach (SocketGuildChannel channel in guild.Channels)
            {
                if (channel.Name == config.VClogChannel.value.ToLower())
                {
                    await (channel as SocketTextChannel).SendMessageAsync(msg, true);
                    return;
                }
            }
            var logChannel = await guild.CreateTextChannelAsync(config.VClogChannel.value);
            var perms = new OverwritePermissions(sendMessages: PermValue.Deny);
            var suppress = logChannel.AddPermissionOverwriteAsync(guild.EveryoneRole, perms);
            await logChannel.SendMessageAsync(msg, true);
        }

        private async Task Client_MessageReceived(SocketMessage message)
        {
            foreach (string fuckead in fuckheads)
            {
                if (message.Author.Id.ToString() == fuckead || message.Author.Id.ToString() == ((SocketGuildUser)message.Author).Guild.CurrentUser.Id.ToString())
                {
                    return;
                }
            }
            int newSwears = CountSwears(message.ToString());
            if (newSwears != 0 && Convert.ToBoolean(config.enableSwearjar.value))
            {
                await UpdateSwearJar(message, newSwears);
            }
            if (IsQuestion(message.Content) && Convert.ToBoolean(config.enableAnswerback.value))
            {
                Random rnd = new Random();
                await message.Channel.SendMessageAsync(answers[rnd.Next(answers.Length)]);
            }
            if (message.Content.Contains("HERESY"))
            {
                await message.Channel.SendMessageAsync(":fire:**BUUUURN HERETICS!**:fire:");
            }
            if (message.Content.StartsWith("!"))
            {
                await HandleCommand(message);
            }
        }
        public async Task HandleCommand(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            var context = new CommandContext(client, message);
            var result = await commandService.ExecuteAsync(context, 1, services);
            if (!result.IsSuccess)
            {
                await context.Channel.SendMessageAsync(result.ErrorReason);
            }
        }
        private int CountSwears(string message)
        {
            int newSwears = 0;
            foreach (string swear in swears)
            {
                if (message.ToLower().Contains(swear.ToLower())) //swear found
                {
                    newSwears++;
                }
            }
            return newSwears;
        }
        private async Task UpdateSwearJar(SocketMessage message, int newSwears)
        {
            const string swearMessage = "Total swears: ";
            if (Convert.ToBoolean(config.pmSwearMsg.value))
            {
                await message.Author.SendMessageAsync(config.swearResponse.value);
            }
            else
            {
                await message.Channel.SendMessageAsync(message.Author.Mention + " " + config.swearResponse.value);
            }
            var chnl = message.Channel as SocketGuildChannel;
            var guild = chnl.Guild as SocketGuild;
            SocketGuildChannel swearChannel = null;
            foreach (SocketGuildChannel channel in guild.Channels) //search through channels in guild
            {
                if (channel.Name == config.swearChannelName.value) //find the swear jar
                {
                    swearChannel = channel;
                    break;
                }
            }
            if (swearChannel == null)
            {
                RestTextChannel newChannel = await guild.CreateTextChannelAsync(config.swearChannelName.value);
                IRole everyone = null;
                foreach (IRole role in guild.Roles)
                {
                    if (role.Name == "@everyone")
                    {
                        everyone = role;
                    }
                }
                //who the fuck wrote this cancerous constructor?! anyways, this just stops users sending messages
                await newChannel.AddPermissionOverwriteAsync(everyone, new OverwritePermissions(PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Deny));
            }
            var messages = await ((ISocketMessageChannel)swearChannel).GetMessagesAsync(50).Flatten(); //get the 50 most recent (and most likely all) messages
            bool foundMain = false, foundSpecific = false; //indicates whether the main message was found and whether the individual player message was found
            foreach (IUserMessage msg in messages)
            {
                if (msg.Author.Id == guild.CurrentUser.Id && msg.Content.StartsWith(swearMessage)) //find the main jar message
                {
                    foundMain = true;
                    int swearNo = Convert.ToInt32(msg.Content.Substring(msg.Content.IndexOf(swearMessage) + swearMessage.Length));
                    await msg.ModifyAsync(x =>
                        x.Content = swearMessage + (swearNo + newSwears).ToString()
                    );
                }
                else if (msg.Author.Id == guild.CurrentUser.Id && msg.Content.StartsWith(message.Author.Id.ToString())) //find the individual player's note
                {
                    foundSpecific = true;
                    //looks awful, but it's just getting the number of swears listed in the message as an int
                    int startsAt = msg.Content.IndexOf(": ") + ": ".Length;
                    int swearNo = Convert.ToInt32(msg.Content.Substring(startsAt));
                    //Console.WriteLine();
                    await msg.ModifyAsync(x =>
                        x.Content = message.Author.Id.ToString() + " " + message.Author.Username + ": " + (swearNo + newSwears).ToString()
                    );
                }
                if (foundMain && foundSpecific)
                {
                    break;
                }
            }
            if (!foundMain)
            {
                await ((ISocketMessageChannel)swearChannel).SendMessageAsync(swearMessage + newSwears.ToString());
            }
            if (!foundSpecific)
            {
                await ((ISocketMessageChannel)swearChannel).SendMessageAsync(message.Author.Id.ToString() + " " + message.Author.Username + ": " + newSwears.ToString());
            }
        }
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}