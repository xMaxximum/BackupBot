namespace BackupBot.Bot;
public interface IBot : IDiscordHostedShardService
{
    Task<List<Models.ApiGuildModel>> GetGuilds(string json);
    Task<Models.ApiFullGuildModel> GetGuild(ulong guildId, ulong userId);
    Task<bool> CheckGuild(ulong guildId, ulong userId);
}
