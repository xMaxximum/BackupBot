using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using Microsoft.Extensions.Logging;
using NodaTime.Extensions;
using static BackupBot.Bot.Models;

namespace BackupBot.Bot.Backups
{
    public sealed partial class TakeBackup : ITakeBackup
    {
        public IDatabase Database { private get; init; } = null!;
        private ILogger<TakeBackup> Logger { get; init; }

        public TakeBackup(IDatabase database, ILogger<TakeBackup> logger)
        {
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));
            Database = database;
            Logger = logger;
        }

        public async void Take(InteractionContext context, bool? guildinfo, bool? channelBool, bool? roleBool, bool? assets, string? comment)
        {
            var downloader = new Util.ImageDownloader();
            var startPath = Environment.CurrentDirectory;

            Guild? guild = null;

            if (guildinfo == true)
            {
                ulong afkChannel = context.Guild.AfkChannel != null ? context.Guild.AfkChannel.Id : 0;
                ulong publicUpdatesChannel = context.Guild.PublicUpdatesChannel != null ? context.Guild.PublicUpdatesChannel.Id : 0;
                ulong rulesChannel = context.Guild.RulesChannel != null ? context.Guild.RulesChannel.Id : 0;
                ulong systemChannel = context.Guild.SystemChannel != null ? context.Guild.SystemChannel.Id : 0;


                if (context.Guild.HasWelcomeScreen)
                {
                    WelcomeScreen welcomeScreen;
                    var welcomeChannels = new List<WelcomeScreenChannel>();
                    foreach (var channel in context.Guild.GetWelcomeScreenAsync().Result.WelcomeChannels)
                    {
                        welcomeChannels.Add(new WelcomeScreenChannel(channel.ChannelId, channel.Description, channel.EmojiName != null ? DiscordEmoji.FromName(context.Client, channel.EmojiName) : null));
                    }
                    welcomeScreen = new WelcomeScreen(true, context.Guild.Description, welcomeChannels);

                    guild = new Guild(context.Guild.JoinedAt.UtcDateTime, context.Guild.Name, (short)context.Guild.VerificationLevel,
                                      !Convert.ToBoolean(context.Guild.DefaultMessageNotifications), (short)context.Guild.ExplicitContentFilter,
                                      Convert.ToBoolean(context.Guild.MfaLevel), context.Guild.VanityUrlCode, context.Guild.Description, context.Guild.PremiumProgressBarEnabled, context.Guild.IsCommunity, afkChannel,
                                      context.Guild.AfkTimeout, context.Guild.PreferredLocale, publicUpdatesChannel, rulesChannel, systemChannel, welcomeScreen);
                }

                else
                {
                    guild = new Guild(context.Guild.JoinedAt.UtcDateTime, context.Guild.Name, (short)context.Guild.VerificationLevel,
                                      !Convert.ToBoolean(context.Guild.DefaultMessageNotifications), (short)context.Guild.ExplicitContentFilter,
                                      Convert.ToBoolean(context.Guild.MfaLevel), context.Guild.VanityUrlCode, context.Guild.Description, context.Guild.PremiumProgressBarEnabled, context.Guild.IsCommunity, afkChannel,
                                      context.Guild.AfkTimeout, context.Guild.PreferredLocale, publicUpdatesChannel, rulesChannel, systemChannel, null);
                }
            }

            List<Channel> channels = new();
            List<Role> roles = new();


            if (channelBool == true)
            {
                foreach (var channel in context.Guild.Channels.Values.ToList())
                {
                    var permissionOverwrites = new List<PermissionOverwrite>();

                    foreach (var overwrite in channel.PermissionOverwrites)
                    {
                        permissionOverwrites.Add(new(!Convert.ToBoolean(overwrite.Type), Convert.ToBoolean(overwrite.Type) == false ? overwrite.GetRoleAsync().Result.Id : null,
                                 Convert.ToBoolean(overwrite.Type) == true ? overwrite.GetMemberAsync().Result.Id : null, (ulong)overwrite.Allowed, (ulong)overwrite.Denied));
                    }
                    channels.Add(new(channel.Id, (short)channel.Type, channel.Name, channel.Topic, channel.IsNsfw, (short)channel.Position, channel.Bitrate, channel.UserLimit.HasValue ? (short)channel.UserLimit.Value : null,
                                channel.PerUserRateLimit, permissionOverwrites));
                }
            }

            if (roleBool == true)
            {
                foreach (var role in context.Guild.Roles.Values.Where(role => role.IsManaged != true))
                {
                    roles.Add(new(role.Name, role.Id, role.Color.Value, role.IsHoisted, role.UnicodeEmoji?.ToString(), (short)role.Position, (ulong)role.Permissions, role.IsMentionable));
                }
            }

            await Database.InsertGuild(context.Guild.Id, context.Guild.OwnerId, context.Guild.GetMemberAsync(context.Client.CurrentUser.Id).Result.JoinedAt.ToInstant());

            var backup = new Backup(0, context.Guild.Id, context.Guild.OwnerId, context.Guild.Name, guild, channels.OrderBy(entry => entry.Position).ToList(), roles.OrderBy(entry => entry.Position).ToList(), DateTime.UtcNow.ToInstant(), comment);
            await Database.InsertBackup(backup);
            var backupId = await Database.GetGuildBackups(context.Guild.Id);

            if (assets == true)
            {
                if (context.Guild.IconUrl != null) await downloader.DownloadImageAsync(Path.Combine(startPath, "download", "icons", $"{context.Guild.Id}"), $"{context.Guild.Id}_{backupId}", new Uri(context.Guild.IconUrl));

                var emotes = await context.Guild.GetEmojisAsync();
                var stickers = await context.Guild.GetStickersAsync();
                var roleIcons = context.Guild.Roles.Values.Where(role => role.IconUrl != null);
                var banner = context.Guild.BannerUrl;
                var splash = context.Guild.SplashUrl;
                var discoverySplash = context.Guild.DiscoverySplashUrl;

                if (emotes.Any())
                {
                    foreach (var (emote, i) in emotes.Select((value, i) => (value, i)))
                    {
                        await downloader.DownloadImageAsync(Path.Combine(startPath, "download", "emotes", $"{context.Guild.Id}"), $"{context.Guild.Id}_{backupId}_{i}_{emote.Name}", new Uri(emote.Url));
                    }
                }


                if (stickers.Any())
                {
                    foreach (var (sticker, i) in stickers.Select((value, i) => (value, i)))
                    {
                        await downloader.DownloadImageAsync(Path.Combine(startPath, "download", "stickers", $"{context.Guild.Id}"), $"{context.Guild.Id}_{backupId}_{i}_{sticker.Name}", new Uri(sticker.Url));
                        Console.WriteLine($"{sticker.Name}, {sticker.Description}, {sticker.FormatType}, {sticker.Type}, {sticker.Asset}");
                    }
                }

                if (roleIcons.Any())
                {
                    foreach (var (role, i) in roleIcons.Select((value, i) => (value, i)))
                    {
                        await downloader.DownloadImageAsync(Path.Combine(startPath, "download", "roleicons", $"{context.Guild.Id}"), $"{context.Guild.Id}_{backupId}_{role.Id}_{i}", new Uri(role.IconUrl));
                    }
                }

                if (banner != null)
                {
                    await downloader.DownloadImageAsync(Path.Combine(startPath, "download", "banners", $"{context.Guild.Id}"), $"{context.Guild.Id}_{backupId}", new Uri(banner));
                }

                if (splash != null)
                {
                    await downloader.DownloadImageAsync(Path.Combine(startPath, "download", "splash", $"{context.Guild.Id}"), $"{context.Guild.Id}_{backupId}", new Uri(splash));
                }

                if (discoverySplash != null)
                {
                    await downloader.DownloadImageAsync(Path.Combine(startPath, "download", "discoverysplash", $"{context.Guild.Id}"), $"{context.Guild.Id}_{backupId}", new Uri(discoverySplash));
                }
            }
        }
    }
}
