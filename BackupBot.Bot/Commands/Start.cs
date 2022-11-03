using NodaTime.Extensions;

namespace BackupBot.Bot.Commands
{
    public class Start : ApplicationCommandsModule
    {
        public IDatabase Database { private get; init; } = null!;

        [SlashCommand("start", "Use this command to start using the bot in this server.")]
        public async Task Register(InteractionContext context)
        {
            await context.CreateResponseAsync(DisCatSharp.Enums.InteractionResponseType.DeferredChannelMessageWithSource);

            await Database.InsertUser(new Models.User(context.User.Id, NodaTime.Instant.FromDateTimeUtc(DateTime.UtcNow)));
            await Database.InsertGuild(context.Guild.Id, context.Guild.OwnerId, DateTime.UtcNow.ToInstant());

            await context.EditResponseAsync(new DiscordWebhookBuilder()
            {
                Content = "tried"
            });

        }
    }
}
