using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using Microsoft.Extensions.Logging;
using static BackupBot.Bot.Models;

namespace BackupBot.Bot.Backups
{
    public class RestoreBackup : IRestoreBackup
    {
        public IDatabase Database { private get; init; } = null!;
        private ILogger<IRestoreBackup> Logger { get; init; }

        public RestoreBackup(IDatabase database, ILogger<IRestoreBackup> logger)
        {
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));
            Database = database;
            Logger = logger;
        }

        public async void Restore(InteractionContext context, bool guildInfo, bool channels, bool roles, bool assets, Backup backup, bool cleanAll)
        {
            if (backup.ServerId != 0)
            {
                List<RoleDummy> roleDummies = new();

                DiscordChannel? afkChannel = null;
                DiscordChannel? rulesChannel = null;
                DiscordChannel? publicUpdatesChannel = null;
                DiscordChannel? systemChannel = null;

                if (roles && backup.Roles!.Any())
                {
                    if (cleanAll)
                    {
                        try
                        {
                            foreach (var role in context.Guild.Roles.Values)
                            {
                                if (role != context.Guild.EveryoneRole && !role.IsManaged) await role.DeleteAsync("Restoring process");
                            }
                        }
                        catch
                        {

                        }
                    }

                    var roleResults = Util.CompareRoles(context.Guild.Roles.Values.ToList(), backup.Roles!);
                    foreach (var role in roleResults)
                    {
                        if (role.RoleExists)
                        {
                            await context.Guild.GetRole(role.OldRole).ModifyAsync(role.Name, (Permissions?)role.Permissions, role.Color, role.Hoisted, role.Mentionable);
                        }
                        else
                        {
                            var createdRole = await context.Guild.CreateRoleAsync(role.Name, (Permissions?)role.Permissions, role.Color, role.Hoisted, role.Mentionable);
                            roleDummies.Add(new(true, role.OldRole, createdRole.Id, role.Name, role.Color, role.Hoisted, role.UnicodeEmoji, role.Position, role.Permissions, role.Mentionable));
                        }
                    }
                }

                if (channels && backup.Channels!.Any())
                {
                    List<Channel> channels1 = new();

                    if (cleanAll)
                    {
                        await context.Guild.ModifyCommunitySettingsAsync(false, null, null);
                        foreach (var channel in context.Guild.Channels.Values)
                        {
                            await channel.DeleteAsync();
                        }
                    }

                    var channelDummyResults = Util.CompareChannels(context.Guild, context.Guild.Channels.Values, backup.Channels!);
                    List<DiscordChannel> channelResults = new();

                    foreach (var channel in channelDummyResults)
                    {
                        if (channel.ChannelExists)
                        {
                            var discordChannel = context.Guild.GetChannel(channel.ChannelId);
                            await discordChannel.ModifyAsync(async guildChannel =>
                            {
                                guildChannel.Nsfw = channel.Nsfw;
                                guildChannel.Name = channel.Name;
                                guildChannel.Topic = channel.Topic;
                                guildChannel.Bitrate = channel.Bitrate;
                                guildChannel.UserLimit = channel.UserLimit;
                                guildChannel.PerUserRateLimit = channel.Ratelimit;
                                if (channel.PermissionOverwrites is not null)
                                {
                                    List<DiscordOverwriteBuilder> overwriteBuilders = new();

                                    foreach (var item in channel.PermissionOverwrites)
                                    {
                                        if (item.Role)
                                        {
                                            if (context.Guild.Roles.TryGetValue((ulong)item.RoleId!, out _))
                                            {
                                                overwriteBuilders.Add(new()
                                                {
                                                    Allowed = item.Allow != null ? (Permissions)item.Allow : Permissions.None,
                                                    Denied = item.Allow != null ? (Permissions)item.Allow : Permissions.None,
                                                    Target = context.Guild.GetRole((ulong)item.RoleId!),
                                                    Type = OverwriteType.Role
                                                });
                                            }
                                            else
                                            {
                                                if (roleDummies.Where(entry => entry.OldRole == item.RoleId).First().Name != string.Empty)
                                                {
                                                    overwriteBuilders.Add(new()
                                                    {
                                                        Allowed = item.Allow != null ? (Permissions)item.Allow : Permissions.None,
                                                        Denied = item.Allow != null ? (Permissions)item.Allow : Permissions.None,
                                                        Target = context.Guild.GetRole(roleDummies.First(role => role.OldRole == item.RoleId).NewRole),
                                                        Type = OverwriteType.Role
                                                    });
                                                }
                                                else continue;
                                            }
                                        }
                                        else
                                        {
                                            DiscordMember? member = await context.Guild.GetMemberAsync((ulong)item.UserId!);

                                            if (member.UsernameWithDiscriminator == string.Empty) continue;

                                            overwriteBuilders.Add(new()
                                            {
                                                Allowed = item.Allow != null ? (Permissions)item.Allow : Permissions.None,
                                                Denied = item.Allow != null ? (Permissions)item.Allow : Permissions.None,
                                                Target = member,
                                                Type = OverwriteType.Member
                                            });
                                        }
                                    }
                                    guildChannel.PermissionOverwrites = overwriteBuilders;
                                }
                            });

                            if (backup.ServerInfo != null)
                            {
                                if (discordChannel.Id == backup.ServerInfo.AfkChannel)
                                    afkChannel = discordChannel;
                                if (discordChannel.Id == backup.ServerInfo.SystemChannel)
                                    systemChannel = discordChannel;
                                if (discordChannel.Id == backup.ServerInfo.RulesChannel)
                                    rulesChannel = discordChannel;
                                if (discordChannel.Id == backup.ServerInfo.PublicUpdatesChannel)
                                    publicUpdatesChannel = discordChannel;
                            }
                        }
                        else
                        {
                            List<DiscordOverwriteBuilder> overwriteBuilders = new();

                            if (channel.PermissionOverwrites is not null)
                            {
                                foreach (var item in channel.PermissionOverwrites)
                                {
                                    if (item.Role)
                                    {
                                        if (context.Guild.Roles.TryGetValue((ulong)item.RoleId!, out _))
                                        {
                                            overwriteBuilders.Add(new()
                                            {
                                                Allowed = item.Allow != null ? (Permissions)item.Allow : Permissions.None,
                                                Denied = item.Allow != null ? (Permissions)item.Allow : Permissions.None,
                                                Target = context.Guild.GetRole((ulong)item.RoleId!),
                                                Type = OverwriteType.Role
                                            });
                                        }
                                        else
                                        {
                                            var role1 = roleDummies.Where(entry => entry.OldRole == item.RoleId).First();
                                            if (role1.Name != string.Empty)
                                            {
                                                overwriteBuilders.Add(new()
                                                {
                                                    Allowed = item.Allow != null ? (Permissions)item.Allow : Permissions.None,
                                                    Denied = item.Allow != null ? (Permissions)item.Allow : Permissions.None,
                                                    Target = context.Guild.GetRole(role1.NewRole),
                                                    Type = OverwriteType.Role
                                                });
                                            }
                                            else continue;
                                        }
                                    }
                                    else
                                    {
                                        DiscordMember? member = await context.Guild.GetMemberAsync((ulong)item.UserId!);

                                        if (member.UsernameWithDiscriminator == string.Empty) continue;

                                        overwriteBuilders.Add(new()
                                        {
                                            Allowed = item.Allow != null ? (Permissions)item.Allow : Permissions.None,
                                            Denied = item.Allow != null ? (Permissions)item.Allow : Permissions.None,
                                            Target = member,
                                            Type = OverwriteType.Member
                                        });
                                    }
                                }
                            }
                            var discordChannel = await context.Guild.CreateChannelAsync(channel.Name, (ChannelType)channel.ChannelType, topic: channel.Topic, bitrate: channel.Bitrate, userLimit: channel.UserLimit,
                                                                   perUserRateLimit: channel.Ratelimit, overwrites: overwriteBuilders.Where(entry => entry.Target != null).Any() ?
                                                                   overwriteBuilders.Where(entry => entry.Target != null) : null, nsfw: channel.Nsfw);

                            if (backup.ServerInfo != null)
                            {
                                if (discordChannel.Id == backup.ServerInfo.AfkChannel)
                                    afkChannel = discordChannel;
                                if (discordChannel.Id == backup.ServerInfo.SystemChannel)
                                    systemChannel = discordChannel;
                                if (discordChannel.Id == backup.ServerInfo.RulesChannel)
                                    rulesChannel = discordChannel;
                                if (discordChannel.Id == backup.ServerInfo.PublicUpdatesChannel)
                                    publicUpdatesChannel = discordChannel;
                            }
                        }
                    }
                }

                var backupId = backup.BackupId;
                if (guildInfo && backup.ServerInfo != null)
                {
                    await context.Guild.ModifyAsync(guild =>
                    {
                        guild.MfaLevel = backup.ServerInfo.MfaRequired ? MfaLevel.Enabled : MfaLevel.Disabled;
                        guild.VerificationLevel = (VerificationLevel)backup.ServerInfo.VerificationLevel;
                        guild.DefaultMessageNotifications = backup.ServerInfo.AllMessages ? DefaultMessageNotifications.AllMessages : DefaultMessageNotifications.MentionsOnly;
                        guild.ExplicitContentFilter = (ExplicitContentFilter)backup.ServerInfo.ContentFilterLevel;
                        guild.Description = backup.ServerInfo.Description;
                        guild.PremiumProgressBarEnabled = backup.ServerInfo.BoostBar;
                        guild.AfkTimeout = backup.ServerInfo.AfkTimeout;
                        guild.PreferredLocale = backup.ServerInfo.PreferredLocale;

                        if (afkChannel != null)
                            guild.AfkChannel = afkChannel;
                        if (systemChannel != null)
                            guild.SystemChannel = systemChannel;
                    });


                    if (backup.ServerInfo.IsCommunity)
                    {

                        if (publicUpdatesChannel != null && rulesChannel != null)
                        {
                            await context.Guild.ModifyCommunitySettingsAsync(true, rulesChannel, publicUpdatesChannel, backup.ServerInfo.PreferredLocale != string.Empty ? null : backup.ServerInfo.PreferredLocale,
                                                                            backup.ServerInfo.Description != string.Empty ? null : backup.ServerInfo.Description);
                        }


                        if (context.Guild.IsCommunity)
                        {

                            if (backup.ServerInfo.WelcomeScreen != null && backup.ServerInfo.WelcomeScreen.Enabled)
                            {
                                await context.Guild.ModifyWelcomeScreenAsync(screen =>
                                {
                                    screen.Enabled = true;
                                    screen.Description = backup.ServerInfo.Description;

                                    List<DiscordGuildWelcomeScreenChannel> channels = new();

                                    foreach (var channel in backup.ServerInfo.WelcomeScreen.ScreenChannels)
                                    {
                                        if (channel.EmojiName != null)
                                        {
                                            var emote = DiscordEmoji.FromName(context.Client, channel.EmojiName);
                                            channels.Add(new DiscordGuildWelcomeScreenChannel(channel.ChannelId, channel.Description, emote));
                                        }
                                        else
                                        {
                                            channels.Add(new DiscordGuildWelcomeScreenChannel(channel.ChannelId, channel.Description));
                                        }
                                    }

                                    screen.WelcomeChannels = channels;
                                });
                            }
                        }
                    }
                    else await context.Guild.ModifyCommunitySettingsAsync(false, null, null);
                }

                if (assets)
                {
                    var startPath = Path.Combine(Environment.CurrentDirectory, "download");

                    await context.Guild.ModifyAsync(guild =>
                    {
                        if (Directory.Exists(Path.Combine(startPath, "icons", $"{backup.ServerId}")))
                        {
                            var icon = Directory.GetFiles(Path.Combine(startPath, "icons", $"{backup.ServerId}"), $"{backup.ServerId}_{backupId}*").FirstOrDefault();
                            if (icon != string.Empty && icon != null)
                            {
                                guild.Icon = File.OpenRead(icon!);

                            }
                        }
                        if (Directory.Exists(Path.Combine(startPath, "banners", $"{backup.ServerId}")))
                        {
                            var banner = Directory.GetFiles(Path.Combine(startPath, "banners", $"{backup.ServerId}"), $"{backup.ServerId}_{backupId}*").FirstOrDefault();
                            if (banner != string.Empty && banner != null)
                            {
                                guild.Banner = File.OpenRead(banner!);
                            }
                        }
                        if (Directory.Exists(Path.Combine(startPath, "splash", $"{backup.ServerId}")))
                        {
                            var splash = Directory.GetFiles(Path.Combine(startPath, "splash", $"{backup.ServerId}"), $"{backup.ServerId}_{backupId}*").FirstOrDefault();
                            if (splash != string.Empty && splash != null)
                            {
                                guild.Splash = File.OpenRead(splash!);
                            }
                        }
                        if (Directory.Exists(Path.Combine(startPath, "discoverysplash", $"{backup.ServerId}")))
                        {
                            var discoverysplash = Directory.GetFiles(Path.Combine(startPath, "discoverysplash", $"{backup.ServerId}"), $"{backup.ServerId}_{backupId}*").FirstOrDefault();
                            if (discoverysplash != string.Empty && discoverysplash != null)
                            {
                                guild.DiscoverySplash = File.OpenRead(discoverysplash!);
                            }
                        }
                    });

                    if (Directory.Exists(Path.Combine(startPath, "emotes", $"{backup.ServerId}")))
                    {
                        string[] files = Directory.GetFiles(Path.Combine(startPath, "emotes", $"{backup.ServerId}"), $"{context.Guild.Id}_{backup.BackupId}_*_*");

                        foreach (var file in files)
                        {
                            var fileName = Path.GetFileNameWithoutExtension(file);

                            var emote = File.OpenRead(file);
                            try
                            {
                                await context.Guild.CreateEmojiAsync(fileName.Split('_')[3], emote);
                            }
                            catch
                            {

                            }
                        }
                    }
                    /*
                    if (Directory.Exists(Path.Combine(startPath, "stickers", $"{backup.ServerId}")))
                    {
                        string[] files = Directory.GetFiles(Path.Combine(startPath, "stickers", $"{backup.ServerId}"), $"{context.Guild.Id}_{backup.BackupId}_*_*");

                        foreach (var file in files)
                        {
                            var fileName = Path.GetFileNameWithoutExtension(file);

                            using Stream fs = File.OpenRead(file);
                            try
                            {
                                await context.Guild.CreateStickerAsync(fileName.Split('_')[3], null, null);
                            }
                            catch
                            {

                            }
                        }
                    }
                    */
                }
            }
            else
            {
                await context.FollowUpAsync(new DiscordFollowupMessageBuilder()
                {
                    Content = "Either the backup wasn't made by you or it doesn't exist!"
                });
            }
        }
    }
}
