using DisCatSharp.Entities;
using NodaTime;
using NodaTime.Extensions;

namespace BackupBot.Bot
{
    public class Models
    {
        public class Backup
        {
            public int BackupId { get; set; }
            public ulong ServerId { get; set; }
            public ulong CreatorId { get; set; }
            public string ServerName { get; set; }
            public Guild? ServerInfo { get; set; }
            public List<Channel>? Channels { get; set; }
            public List<Role>? Roles { get; set; }
            public Instant CreationDate { get; set; }
            public string? Comment { get; set; }

            public Backup(int backupId, ulong serverId, ulong creatorId, string serverName, Guild? serverInfo, List<Channel>? channels, List<Role>? roles, Instant creationDate, string? comment)
            {
                BackupId = backupId;
                ServerId = serverId;
                CreatorId = creatorId;
                ServerName = serverName;
                ServerInfo = serverInfo;
                Channels = channels;
                Roles = roles;
                CreationDate = creationDate;
                Comment = comment;
            }

            public Backup()
            {
            }
        }

        public class Guild
        {
            public DateTime JoinDate { get; set; }
            public string Name { get; set; }
            public short VerificationLevel { get; set; }
            public bool AllMessages { get; set; }
            public short ContentFilterLevel { get; set; }
            public bool MfaRequired { get; set; }
            public string? Vanity { get; set; }
            public string? Description { get; set; }
            public bool BoostBar { get; set; }
            public bool IsCommunity { get; set; }
            public ulong AfkChannel { get; set; }
            public int AfkTimeout { get; set; }
            public string PreferredLocale { get; set; }
            public ulong PublicUpdatesChannel { get; set; } // moderator-only channel
            public ulong RulesChannel { get; set; }
            public ulong SystemChannel { get; set; } // stuff like welcome msgs
            public WelcomeScreen? WelcomeScreen { get; set; }

            public Guild(DateTime joinDate, string name, short verificationLevel, bool allMessages, short contentFilterLevel, bool mfaRequired, string? vanity, string? description, bool boostBar, bool isCommunity,
                         ulong afkChannel, int afkTimeout, string preferredLocale, ulong publicUpdatesChannel, ulong rulesChannel, ulong systemChannel, WelcomeScreen? welcomeScreen)
            {
                JoinDate = joinDate;
                Name = name;
                VerificationLevel = verificationLevel;
                AllMessages = allMessages;
                ContentFilterLevel = contentFilterLevel;
                MfaRequired = mfaRequired;
                Vanity = vanity;
                Description = description;
                BoostBar = boostBar;
                IsCommunity = isCommunity;
                AfkChannel = afkChannel;
                AfkTimeout = afkTimeout;
                PreferredLocale = preferredLocale;
                PublicUpdatesChannel = publicUpdatesChannel;
                RulesChannel = rulesChannel;
                SystemChannel = systemChannel;
                WelcomeScreen = welcomeScreen;
            }
        }

        public class WelcomeScreen
        {
            public bool Enabled { get; set; }
            public string Description { get; set; }
            public List<WelcomeScreenChannel> ScreenChannels { get; set; }

            public WelcomeScreen(bool enabled, string description, List<WelcomeScreenChannel> screenChannels)
            {
                Enabled = enabled;
                Description = description;
                ScreenChannels = screenChannels;
            }
        }

        public class WelcomeScreenChannel
        {
            public ulong ChannelId { get; set; }
            public string Description { get; set; }
            public ulong? EmojiId { get; set; }
            public string EmojiName { get; set; }

            public WelcomeScreenChannel(ulong channelId, string description, DiscordEmoji? emoji = null)
            {
                ChannelId = channelId;
                Description = description;
                if (emoji != null)
                    EmojiName = emoji.Name;
                else if (emoji == null)
                {

                }
                else
                    EmojiId = emoji!.Id;
            }
        }

        public class Channel
        {
            public ulong ChannelId { get; set; }
            public short ChannelType { get; set; }
            public string Name { get; set; }
            public string? Topic { get; set; }
            public bool? Nsfw { get; set; }
            public short Position { get; set; }
            public int? Bitrate { get; set; }
            public short? UserLimit { get; set; }
            public int? Ratelimit { get; set; }
            public List<PermissionOverwrite>? PermissionOverwrites { get; set; }

            public Channel(ulong channelId, short channelType, string name, string? topic, bool? nsfw, short position, int? bitrate, short? userLimit, int? ratelimit,
                            List<PermissionOverwrite>? permissionOverwrites)
            {
                ChannelId = channelId;
                ChannelType = channelType;
                Name = name;
                Topic = topic;
                Nsfw = nsfw;
                Position = position;
                Bitrate = bitrate;
                UserLimit = userLimit;
                Ratelimit = ratelimit;
                PermissionOverwrites = permissionOverwrites;
            }
        }

        public class Role
        {
            public string Name { get; set; }
            public ulong RoleId { get; set; }
            public int Color { get; set; }
            public bool Hoisted { get; set; }
            public string? UnicodeEmoji { get; set; }
            public short Position { get; set; }
            public ulong Permissions { get; set; }
            public bool Mentionable { get; set; }

            public Role(string name, ulong roleId, int color, bool hoisted, string? unicodeEmoji, short position, ulong permissions, bool mentionable)
            {
                Name = name;
                RoleId = roleId;
                Color = color;
                Hoisted = hoisted;
                UnicodeEmoji = unicodeEmoji;
                Position = position;
                Permissions = permissions;
                Mentionable = mentionable;
            }
        }

        public class PermissionOverwrite
        {
            public bool Role { get; set; }
            public ulong? RoleId { get; set; }
            public ulong? UserId { get; set; }
            public ulong? Allow { get; set; }
            public ulong? Deny { get; set; }

            public PermissionOverwrite(bool role, ulong? roleId, ulong? userId, ulong? allow, ulong? deny)
            {
                Role = role;
                RoleId = roleId;
                UserId = userId;
                Allow = allow;
                Deny = deny;
            }
        }

        public class User
        {
            public ulong UserId { get; set; }
            public Instant JoinDate { get; set; }

            public User(ulong userId, Instant joinDate)
            {
                UserId = userId;
                JoinDate = joinDate;
            }

            public User()
            {
            }
        }

        public class RoleDummy
        {
            public bool RoleExists { get; set; }
            public ulong OldRole { get; set; }
            public ulong NewRole { get; set; }
            public string Name { get; set; }
            public int Color { get; set; }
            public bool Hoisted { get; set; }
            public string? UnicodeEmoji { get; set; }
            public short Position { get; set; }
            public ulong Permissions { get; set; }
            public bool Mentionable { get; set; }

            public RoleDummy(bool roleExists, ulong oldRole, ulong newRole, string name, int color, bool hoisted, string? unicodeEmoji, short position, ulong permissions, bool mentionable)
            {
                RoleExists = roleExists;
                OldRole = oldRole;
                NewRole = newRole;
                Name = name;
                Color = color;
                Hoisted = hoisted;
                UnicodeEmoji = unicodeEmoji;
                Position = position;
                Permissions = permissions;
                Mentionable = mentionable;
            }
        }

        public class ChannelDummy
        {
            public bool ChannelExists { get; set; }
            public ulong ChannelId { get; set; }
            public short ChannelType { get; set; }
            public string? Name { get; set; }
            public string? Topic { get; set; }
            public bool? Nsfw { get; set; }
            public short Position { get; set; }
            public int? Bitrate { get; set; }
            public short? UserLimit { get; set; }
            public int? Ratelimit { get; set; }
            public List<PermissionOverwrite>? PermissionOverwrites { get; set; }
            public bool AfkChannel { get; set; }
            public bool PublicUpdatesChannel { get; set; }
            public bool RulesChannel { get; set; }
            public bool SystemChannel { get; set; }

            public ChannelDummy(bool channelExists, ulong channelId, short channelType, string? name, string? topic, bool? nsfw, short position, int? bitrate, short? userLimit, int? ratelimit,
                                List<PermissionOverwrite>? permissionOverwrites, bool afkChannel, bool publicUpdatesChannel, bool rulesChannel, bool systemChannel)
            {
                ChannelExists = channelExists;
                ChannelId = channelId;
                ChannelType = channelType;
                Name = name;
                Topic = topic;
                Nsfw = nsfw;
                Position = position;
                Bitrate = bitrate;
                UserLimit = userLimit;
                Ratelimit = ratelimit;
                PermissionOverwrites = permissionOverwrites;
                AfkChannel = afkChannel;
                PublicUpdatesChannel = publicUpdatesChannel;
                RulesChannel = rulesChannel;
                SystemChannel = systemChannel;
            }
        }

        public class DiscordConfig
        {
            public string Token { get; set; }
            public string DbString { get; set; }
        }

        public class ApiGuildModel
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public bool IsAdmin { get; set; }
            public bool BotJoined { get; set; }
        }

        public class ApiNormalGuildModel
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public long Permissions { get; set; }
        }

        public class ApiFullGuildModel
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public string BotJoined { get; set; } = string.Empty;
            public string GuildCreated { get; set; } = string.Empty;
            public string MemberCount { get; set; } = string.Empty;
            // maybe add channels for later?

            public ApiFullGuildModel(DiscordGuild guild)
            {
                this.Id = guild.Id.ToString();
                this.Name = guild.Name;
                this.Icon = guild.IconHash;
                this.BotJoined = guild.JoinedAt.ToString();
                this.GuildCreated = guild.CreationTimestamp.ToString();
                this.MemberCount = guild.MemberCount.ToString();
            }
            
            public ApiFullGuildModel()
            {

            }
        }

        /*
        public class ApiSmallChannelModel
        {
            public string Name { get; set; } = string.Empty;
            public short ChannelType { get; set; }
            public bool Nsfw { get; set; }
        }
        */
    }
}
