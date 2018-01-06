using System.Threading.Tasks;
using System.Diagnostics;
using Discord.Audio;
using Discord;
namespace Audio
{
    public static class Audio
    {
        private static Process CreateFFMPEGStream(string path)
        {
            var ffmpeg = new ProcessStartInfo
            {
                FileName = "C:\\Users\\Typhon\\Documents\\Visual Studio 2017\\Projects\\DiscordBot-core2.0\\bin\\Debug\\netcoreapp2.0\\ffmpeg.exe",
                Arguments = $"-i {path} -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };
            return Process.Start(ffmpeg);
        }

        public async static Task PlayAudio(IVoiceChannel channel, string filePath)
        {
            try
            {
                IAudioClient audioClient = await channel.ConnectAsync();
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
            }
            //await SendAsync(audioClient, filePath);
            //await audioClient.StopAsync();
        }

        private async static Task SendAsync(IAudioClient client, string path)
        {
            // Create FFmpeg using the previous example
            var ffmpeg = CreateFFMPEGStream(path);
            var output = ffmpeg.StandardOutput.BaseStream;
            var discord = client.CreatePCMStream(AudioApplication.Mixed);
            await output.CopyToAsync(discord);
            await discord.FlushAsync();
        }
    }
}