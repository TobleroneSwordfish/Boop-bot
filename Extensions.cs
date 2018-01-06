using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
namespace DiscordExtensions
{
    public static class Extensions
    {
        /// <summary>
        /// Gives a role to a user from role
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="userMention">Without "!"</param>
        /// <returns></returns>
        public async static Task GiveRole(this IGuild guild, IRole role, string userMention)
        {
            foreach (SocketGuildUser dude in ((SocketGuild)guild).Users)
            {
                if (dude.Mention == userMention)
                {
                    await dude.AddRoleAsync(role);
                }
            }
        }
        /// <summary>
        /// Gives a role to a user from string name
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="userMention">Without "!"</param>
        /// <returns></returns>
        public async static Task GiveRole(this IGuild guild, string roleName, string userMention)
        {
            //IRole soughtRole = null;
            foreach (IRole role in guild.Roles)
            {
                if (role.Name == roleName)
                {
                    await GiveRole(guild, role, userMention);
                    break;
                }
            }
        }
        /// <summary>
        /// Removes a role from a user from IRole
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="userMention">Without "!"</param>
        /// <returns></returns>
        public async static Task RemoveRole(this IGuild guild, IRole role, string userMention)
        {
            foreach (SocketGuildUser dude in ((SocketGuild)guild).Users)
            {
                if (dude.Mention == userMention)
                {
                    await dude.RemoveRoleAsync(role);
                }
            }
        }
        /// <summary>
        /// Removes a role from a user from string name
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="userMention">Without "!"</param>
        /// <returns></returns>
        public async static Task RemoveRole(this IGuild guild, string roleName, string userMention)
        {
            //IRole soughtRole = null;
            foreach (IRole role in guild.Roles)
            {
                if (role.Name == roleName)
                {
                    await RemoveRole(guild, role, userMention);
                    break;
                }
            }
        }
        public static async Task<IRole> FindOrCreateRole(this IGuild guild, string roleName)
        {
            //try to find the role
            var roles = guild.Roles;
            foreach (IRole role in roles)
            {
                if (role.Name == roleName)
                {
                    return role;
                }
            }
            //role doesn't exist
            return await guild.CreateRoleAsync(roleName);
        }
        //public static 
    }
}