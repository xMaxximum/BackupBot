using BackupBot.Bot.Backups;
using DisCatSharp.Configuration;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity.Enums;
using DisCatSharp.Interactivity.EventHandling;
using DisCatSharp.Interactivity.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Npgsql;
using Serilog;
using static BackupBot.Bot.Models;

namespace BackupBot.Bot;
internal class Bot : DiscordShardedHostedService, IBot
{
    private ServiceProvider serviceProvider;

    public Bot(IConfiguration config,
        ILogger<Bot> logger,
        IServiceProvider provider,
        IHostApplicationLifetime lifetime) : base(config, logger, provider, lifetime, "BackupBot") { }

    /// <summary>
    /// Attempts to register commands from our BakupBot.Bot Project
    /// </summary>
    void RegisterCommands(ApplicationCommandsExtension commandsExtension)
    {
        var registeredTypes = commandsExtension.RegisterApplicationCommandsFromAssembly();
        commandsExtension.RegisterApplicationCommandsFromAssembly(1021312414182023208);

        if (registeredTypes.Count == 0)
            Logger.LogInformation($"Could not locate any commands to register...");
        else
            Logger.LogInformation($"Registered the following command classes: \n\t{string.Join("\n\t", registeredTypes)}");
    }

    protected override Task ConfigureAsync()
    {
        NpgsqlConnection.GlobalTypeMapper.UseNodaTime();

        Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "download"));
        Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "download", "icons"));
        Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "download", "emotes"));
        Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "download", "stickers"));
        Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "download", "banners"));
        Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "download", "roleicons"));
        Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "download", "splash"));
        Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "download", "discoverysplash"));

        try
        {
            var config = new ConfigurationBuilder().SetBasePath(AppDomain.CurrentDomain.BaseDirectory).AddJsonFile("appsettings.json").Build();
            var section = config.GetSection(nameof(DiscordConfig));
            var discordConfig = section.Get<DiscordConfig>();

            Log.Logger = new LoggerConfiguration().WriteTo.Console(outputTemplate: "[{Timestamp:dd-mm, HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}").CreateLogger();

            ServiceCollection services = new();
            services.AddLogging(logger =>
            {
                LoggerConfiguration loggerConfiguration = new();
                loggerConfiguration.WriteTo.Console(outputTemplate: "[{Timestamp:dd-mm, HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}");
                Log.Logger = loggerConfiguration.CreateLogger();
                logger.AddSerilog(Log.Logger);
            });

            services.AddScoped<IConfiguration>(_ => config);
            services.AddSingleton<IDatabase, Database>();
            services.AddSingleton<ITakeBackup, TakeBackup>();
            services.AddSingleton<IRestoreBackup, RestoreBackup>();

            serviceProvider = services.BuildServiceProvider();

            ShardedClient = new DiscordShardedClient(new DiscordConfiguration()
            {
                Token = discordConfig.Token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers | DiscordIntents.MessageContent | DiscordIntents.GuildMessages,
                MinimumLogLevel = LogLevel.Information,
                LoggerFactory = new LoggerFactory().AddSerilog(Log.Logger),
                ServiceProvider = serviceProvider
            });

            var events = new Events();

            foreach (var client in ShardedClient.ShardClients.Values)
            {
                client.GetApplicationCommands().SlashCommandErrored += events.SlashCommandErrored;
            }

            ShardedClient.GuildDownloadCompleted += events.GuildDownloadCompleted;
            ShardedClient.GuildCreated += events.GuildCreated;
            ShardedClient.GuildDeleted += events.GuildDeleted;
        }
        catch (Exception ex)
        {
            this.Logger.LogError($"Was unable to build {nameof(DiscordShardedClient)} for {this.GetType().Name}");
            this.OnInitializationError(ex);
        }

        return Task.CompletedTask;
    }

    protected override async Task ConfigureExtensionsAsync()
    {
        var commandsExtension = ShardedClient.UseApplicationCommandsAsync(new() { ServiceProvider = serviceProvider, EnableDefaultHelp = true }).Result;
        foreach (var client in commandsExtension)
        {
            RegisterCommands(client.Value);
        }

        await ShardedClient.UseInteractivityAsync(new()
        {
            Timeout = TimeSpan.FromMinutes(2),
            PollBehaviour = PollBehaviour.DeleteEmojis,
            AckPaginationButtons = true,
            ButtonBehavior = ButtonPaginationBehavior.Disable,

            PaginationButtons = new PaginationButtons()
            {
                SkipLeft = new DiscordButtonComponent(ButtonStyle.Primary, "pgb-skip-left", "First", false, new DiscordComponentEmoji("⏮️")),
                Left = new DiscordButtonComponent(ButtonStyle.Primary, "pgb-left", "Previous", false, new DiscordComponentEmoji("◀️")),
                Stop = new DiscordButtonComponent(ButtonStyle.Danger, "pgb-stop", "Cancel", false, new DiscordComponentEmoji("⏹️")),
                Right = new DiscordButtonComponent(ButtonStyle.Primary, "pgb-right", "Next", false, new DiscordComponentEmoji("▶️")),
                SkipRight = new DiscordButtonComponent(ButtonStyle.Primary, "pgb-skip-right", "Last", false, new DiscordComponentEmoji("⏭️"))
            },
            ResponseMessage = "Something went wrong.",
            ResponseBehavior = InteractionResponseBehavior.Ignore
        });
    }

    protected override void OnInitializationError(Exception ex)
    {
        base.OnInitializationError(ex);
    }

    public Task<List<ApiGuildModel>> GetGuilds(string json)
    {
        var guilds = JsonConvert.DeserializeObject<List<ApiNormalGuildModel>>(json);

        var finishedGuilds = new List<ApiGuildModel>();

        foreach (var guild in guilds)
        {
            var shard = ShardedClient.GetShard(Convert.ToUInt64(guild.Id));
            var perms = (Permissions)guild.Permissions;

            if (shard != null && shard.Guilds.TryGetValue(Convert.ToUInt64(guild.Id), out _))
            {
                if (perms.HasPermission(Permissions.ManageGuild))
                {
                    finishedGuilds.Add(new ApiGuildModel { BotJoined = true, Icon = guild.Icon, Id = guild.Id, IsAdmin = true, Name = guild.Name });
                }
            }
            else
            {
                if (perms.HasPermission(Permissions.ManageGuild))
                {
                    finishedGuilds.Add(new ApiGuildModel { BotJoined = false, Icon = guild.Icon, Id = guild.Id, IsAdmin = true, Name = guild.Name });
                }
            }
        }

        return Task.FromResult(finishedGuilds);
    }

    public Task<ApiFullGuildModel> GetGuild(ulong guildId, ulong userId)
    {
        try
        {
            var guild = ShardedClient.GetShard(guildId).TryGetGuildAsync(guildId).Result;

            if (guild != null)
            {
                if (guild.GetMemberAsync(userId).Result.Permissions.HasFlag(Permissions.ManageGuild))
                    return Task.FromResult(new ApiFullGuildModel(guild));
                else return Task.FromResult(new ApiFullGuildModel());
            }

            else return Task.FromResult(new ApiFullGuildModel());
        }
        catch
        {
            return Task.FromResult(new ApiFullGuildModel());
        }
    }

    public Task<bool> CheckGuild(ulong guildId, ulong userId)
    {
        try
        {
            var guild = ShardedClient.GetShard(guildId).TryGetGuildAsync(guildId).Result;
            if (guild != null)
            {
                return Task.FromResult(guild.GetMemberAsync(userId).Result.Permissions.HasFlag(Permissions.ManageGuild));
            }
            else return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}
