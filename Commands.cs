using System;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
//using Discord.Rest;
using System.Threading.Tasks;
using DiscordExtensions;
using System.Collections.Generic;
using System.Net;
using Audio;
public class Commands : ModuleBase
{
    public static DiscordBot.Config config;
    //commands that require no specific permissions
    public class Misc : ModuleBase
    {
        [Command("ping"), Summary("Pongs")]
        public async Task Ping()
        {
            await ReplyAsync("Pong");
        }
        [Command("edu"), Summary("Usage: !edu <query> - retrieves the wikipedia article for <query>")]
        public async Task Educate([Remainder] string query)
        {
            string url = GetURLRedirect("https://en.wikipedia.org/w/index.php?search=" + query + "&title=Special:Search&go=Go");
            await Context.Channel.SendMessageAsync(url);
        }
        [Command("help"), Summary("Displays help message")]
        public async Task Help()
        {
            await ReplyAsync(config.helpMessage);
        }
    }
    [RequireUserPermission(GuildPermission.Administrator)]
    public class GuildAdmin : ModuleBase
    {
        [Command("nuke"), Summary("Usage: !nuke @user - prevents the user from accessing voice channels"), RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task Nuke([Summary("The @mention of the user to nuke")] string mention)
        {
            IGuild guild = Context.Guild;
            IRole nukedRole = await guild.FindOrCreateRole(config.nukedRoleName.value);
            var supress = nukedRole.ModifyAsync(x =>
            {
                x.Color = Color.Green;
                x.Position = 1;
                x.Hoist = true;
                var perms = guild.EveryoneRole.Permissions;
                x.Permissions = perms;
                x.Permissions.Value.Modify(connect: false, speak: false);
            });
            await guild.GiveRole(nukedRole, mention);
        }
        [Command("nuke"),
            Summary("Usage: !banish @user - prevents the user from accessing text channels"),
            Remarks("The banished role can be configured to be whatever you like, provided the name matches the name given in the config"),
            RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task Banish([Summary("The @mention of the user to banish")] string mention)
        {
            IGuild guild = Context.Guild;
            IRole banishedRole = await guild.FindOrCreateRole(config.banishedRoleName.value);
            var supress = banishedRole.ModifyAsync(x =>
            {
                x.Color = Color.DarkOrange;
                x.Position = 1;
                x.Hoist = true;
                var perms = guild.EveryoneRole.Permissions;
                x.Permissions = perms;
                x.Permissions.Value.Modify(sendMessages: false);
            });
            await guild.GiveRole(banishedRole, mention);
        }
        [Command("punish"), Summary("Usage: !punish @user - adds effects of both nuke and banish")]
        public async Task Punish([Summary("The @mention of the user to punish")] string mention)
        {
            await Banish(mention);
            await Nuke(mention);
        }
        [Command("pardon"), Summary("Usage: !pardon @user - removes effects of both nuke and banish")]
        public async Task Pardon([Summary("The @mention of the user to pardon")] string mention)
        {
            IGuild guild = Context.Guild;
            await guild.RemoveRole(config.banishedRoleName.value, mention);
            await guild.RemoveRole(config.nukedRoleName.value, mention);
        }
        [Command("play", RunMode = RunMode.Async), Summary("test command please ignore")]
        public async Task PlayLocal([Remainder] string path)
        {
            var channel = (Context.Message.Author as SocketGuildUser).VoiceChannel;
            if (channel == null)
            {
                var blob = Context.Channel.SendMessageAsync("You're not in a voice channel, whaddoyahwantfrommeeee?");
                return;
            }
            await Audio.Audio.PlayAudio(channel, path);
        }
    }
    [RequireOwner]
    public class BotAdmin : ModuleBase
    {
        [Command("set")]
        public async Task Set([Summary("Name of config property to set")] string propertyName,[Remainder] string value)
        {
            bool found = false;
            foreach (DiscordBot.Property prop in config.properties)
            {
                if (prop.name == propertyName)
                {
                    found = true;
                    prop.StoreValue(value);
                    break;
                }
            }
            if (!found)
            {
                await ReplyAsync("Property name not found, dingus");
            }
        }
    }
    [RequireUserPermission(ChannelPermission.ManageMessages), RequireBotPermission(ChannelPermission.ManageMessages)]
    public class ManageMessages : ModuleBase
    {
        [Command("purge")]
        public async Task Purge(int msgsToPurge)
        {
            var messages = await (((ISocketMessageChannel)(Context.Channel)).GetMessagesAsync(msgsToPurge + 1)).Flatten();
            await Context.Channel.DeleteMessagesAsync(messages);
        }
        [Command("purge")]
        public async Task Purge(int time, string multiplier)
        {
            switch (multiplier)
            {
                case "sec":
                    break;
                case "min":
                    time *= 60;
                    break;
                case "hr":
                    time *= (60 * 60);
                    break;
                default:
                    await ReplyAsync(multiplier + " is not a thing, you're drunk");
                    return;
            }
            //the time to delete up to
            DateTimeOffset pastTime = DateTime.Now.AddSeconds(-time);
            //the time of the current message - defaults to now
            DateTimeOffset currentMsgTime = DateTime.Now;
            while (currentMsgTime.CompareTo(pastTime) >= 0)
            {
                //we compile a list of messages to delete instead of individually deleting to save on network overhead
                List<IMessage> toDelete = new List<IMessage>();
                //download messages in chunks of 50 to save bandwidth
                var messages = await ((ISocketMessageChannel)(Context.Channel)).GetMessagesAsync(50).Flatten();
                foreach (IUserMessage msg in messages)
                {
                    currentMsgTime = msg.Timestamp;
                    if (currentMsgTime.CompareTo(pastTime) >= 0)
                    {
                        toDelete.Add(msg);
                    }
                    else
                    {
                        break;
                    }
                }
                await Context.Channel.DeleteMessagesAsync(toDelete);
            }
        }
    }
    public static string GetURLRedirect(string url)
    {
        HttpWebRequest request = HttpWebRequest.CreateHttp(url);
        request.AllowAutoRedirect = true;
        WebResponse response = request.GetResponse();
        string responseUrl = response.ResponseUri.ToString();
        response.Dispose();
        return responseUrl;
    }
}