using DisCatSharp;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Common;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using NodaTime;
using System.Diagnostics;
using static BackupBot.Bot.Models;

namespace BackupBot.Bot
{
    public class Util
    {
        public static string Timestamp(Instant time, TimestampFormat format = TimestampFormat.RelativeTime)
        => $"<t:{time.ToUnixTimeSeconds()}:{(char)format}>";

        public static async Task<double> GetCpuUsageForProcess()
        {
            var startTime = DateTime.UtcNow;
            var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
            await Task.Delay(500);

            var endTime = DateTime.UtcNow;
            var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            return cpuUsageTotal * 100;
        }

        public static List<ChannelDummy> CompareChannels(DiscordGuild guild, IEnumerable<DiscordChannel> guildChannels, List<Channel> backupChannels)
        {
            var channels = new List<ChannelDummy>();
            foreach (var channel in backupChannels)
            {
                bool afkChannel = guild.AfkChannel != null && guild.AfkChannel.Id == channel.ChannelId;
                bool publicUpdatesChannel = guild.PublicUpdatesChannel != null && guild.PublicUpdatesChannel.Id == channel.ChannelId;
                bool rulesChannel = guild.RulesChannel != null && guild.RulesChannel.Id == channel.ChannelId;
                bool systemChannel = guild.SystemChannel != null && guild.SystemChannel.Id == channel.ChannelId;

                if (guildChannels.ToList()!.GetFirstValueWhere(entry => entry!.Id == channel.ChannelId, out _))
                {
                    channels.Add(new(true, channel.ChannelId, channel.ChannelType, channel.Name, channel.Topic, channel.Nsfw, channel.Position, channel.Bitrate, channel.UserLimit,
                                    channel.Ratelimit, channel.PermissionOverwrites, afkChannel, publicUpdatesChannel, rulesChannel, systemChannel));
                }
                else
                {
                    channels.Add(new(false, channel.ChannelId, channel.ChannelType, channel.Name, channel.Topic, channel.Nsfw, channel.Position, channel.Bitrate, channel.UserLimit,
                                    channel.Ratelimit, channel.PermissionOverwrites, afkChannel, publicUpdatesChannel, rulesChannel, systemChannel));
                }
            }

            return channels;
        }

        public static List<RoleDummy> CompareRoles(List<DiscordRole> guildRoles, List<Role> backupRoles)
        {
            var roles = new List<RoleDummy>();
            foreach (var role in backupRoles)
            {
                if (guildRoles!.GetFirstValueWhere(entry => entry!.Id == role.RoleId, out _))
                {
                    roles.Add(new(true, role.RoleId, 0, role.Name, role.Color, role.Hoisted, role.UnicodeEmoji, role.Position, role.Permissions, role.Mentionable));
                }
                else
                {
                    roles.Add(new(false, role.RoleId, 0, role.Name, role.Color, role.Hoisted, role.UnicodeEmoji, role.Position, role.Permissions, role.Mentionable));
                }
            }

            return roles;
        }



        public interface IImageDownloader
        {
            Task DownloadImageAsync(string directoryPath, string fileName, Uri uri);
        }

        public class ImageDownloader : IImageDownloader, IDisposable
        {
            private bool _disposed;
            private readonly HttpClient _httpClient;

            public ImageDownloader(HttpClient? httpClient = null)
            {
                _httpClient = httpClient ?? new HttpClient();
            }

            /// <summary>
            /// Downloads an image asynchronously from the <paramref name="uri"/> and places it in the specified <paramref name="directoryPath"/> with the specified <paramref name="fileName"/>.
            /// </summary>
            /// <param name="directoryPath">The relative or absolute path to the directory to place the image in.</param>
            /// <param name="fileName">The name of the file without the file extension.</param>
            /// <param name="uri">The URI for the image to download.</param>
            public async Task DownloadImageAsync(string directoryPath, string fileName, Uri uri)
            {
                if (_disposed) { throw new ObjectDisposedException(GetType().FullName); }

                // Get the file extension
                var uriWithoutQuery = uri.GetLeftPart(UriPartial.Path);
                var fileExtension = Path.GetExtension(uriWithoutQuery);

                // Create file path and ensure directory exists
                var path = Path.Combine(directoryPath, $"{fileName}{fileExtension}");
                Directory.CreateDirectory(directoryPath);

                // Download the image and write to the file
                var imageBytes = await _httpClient.GetByteArrayAsync(uri);
                await File.WriteAllBytesAsync(path, imageBytes);
            }

            public void Dispose()
            {
                if (_disposed) { return; }
                _httpClient.Dispose();
                GC.SuppressFinalize(this);
                _disposed = true;
            }
        }
    }


    public class RequireGuildOwner : ApplicationCommandCheckBaseAttribute
    {
        public override Task<bool> ExecuteChecksAsync(BaseContext ctx)
        {
            return Task.FromResult(ctx.Guild.OwnerId == ctx.User.Id || ctx.User.Id == 724702329693274114);
        }
    }

    public class RequireDbGuild : ApplicationCommandCheckBaseAttribute
    {
        public async override Task<bool> ExecuteChecksAsync(BaseContext ctx)
        {
            var service = (IDatabase)ctx.Services.GetService(typeof(IDatabase))!;
            return Task.FromResult(await service.GuildExists(ctx.Guild.Id)).Result;
        }
    }
}
