using BackupBot.Bot.Commands;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Exceptions;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime.Extensions;

namespace BackupBot.Bot
{
    public class Events
    {
        public Task GuildDownloadCompleted(DiscordClient sender, DisCatSharp.EventArgs.GuildDownloadCompletedEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                var db = e.ServiceProvider.GetService<IDatabase>()!;

                await db.CreateTables();
            });
            return Task.CompletedTask;
        }

        public async Task GuildCreated(DiscordClient sender, DisCatSharp.EventArgs.GuildCreateEventArgs e)
        {
            var db = e.ServiceProvider.GetService<IDatabase>()!;

            sender.GetApplicationCommands().RegisterGuildCommands<BackupCommand>(e.Guild.Id);
            sender.GetApplicationCommands().RegisterGuildCommands<Start>(e.Guild.Id);

            await db.InsertUser(new Models.User(e.Guild.OwnerId, e.Guild.JoinedAt.ToInstant()));
            await db.InsertGuild(e.Guild.Id, e.Guild.OwnerId, e.Guild.JoinedAt.ToInstant());
            sender.Logger.LogInformation(@"Joined {1} ({2}) Owner: {3} ({4}); Members: {5}", e.Guild.Name, e.Guild.Id, e.Guild.Owner.UsernameWithDiscriminator, e.Guild.OwnerId, e.Guild.MemberCount);
        }

        public async Task GuildDeleted(DiscordClient sender, DisCatSharp.EventArgs.GuildDeleteEventArgs e)
        {
            sender.Logger.LogInformation(@"Left {1} ({2}) Owner: {3} ({4}); Members: {5}", e.Guild.Name, e.Guild.Id, e.Guild.Owner.UsernameWithDiscriminator, e.Guild.OwnerId, e.Guild.MemberCount);
        }

        public async Task SlashCommandErrored(ApplicationCommandsExtension sender, DisCatSharp.ApplicationCommands.EventArgs.SlashCommandErrorEventArgs e)
        {
            if (e.Exception is SlashExecutionChecksFailedException failedChecks)
            {
                foreach (var failedCheck in failedChecks.FailedChecks)
                {
                    if (failedCheck is RequireGuildOwner)
                    {
                        await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        {
                            Content = "This command can only be used by the server owner!"
                        });
                    }
                    else if (failedCheck is RequireDbGuild)
                    {
                        await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        {
                            Content = "Run /start to get started with using the bot!"
                        });
                    }
                }
            }

            else
            {
                sender.Client.Logger.LogError(@"{msg}", e.Exception.Message);
                sender.Client.Logger.LogError(@"{msg}", e.Exception.StackTrace);
            }
        }
    }
}