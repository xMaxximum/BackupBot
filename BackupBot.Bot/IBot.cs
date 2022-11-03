namespace BackupBot.Bot;
public interface IBot : IDiscordHostedShardService
{
    Task<List<Models.ApiGuildModel>> GetGuilds(string json);
}
