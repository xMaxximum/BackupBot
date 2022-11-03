using DisCatSharp.Entities;
using NodaTime;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupBot.Bot
{
    public interface IDatabase
    {
        NpgsqlConnection GetConnection();
        Task CreateTables();
        Task<bool> GuildExists(ulong guildId);
        Task<List<Models.Backup>> GetBackups(ulong serverid);
        Task<int> GetGuildBackups(ulong serverid);
        Task InsertGuild(ulong guildId, ulong ownerId, Instant joinDate);
        Task InsertUser(Models.User user);
        Task InsertBackup(Models.Backup backup);
        Task<Models.User> GetUser(ulong userId);
        Task<Models.Backup> GetBackup(int backupId, ulong userId);
        Task DeleteGuild(ulong guildId);
    }
}
