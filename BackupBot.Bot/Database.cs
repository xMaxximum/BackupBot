using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NodaTime;
using Npgsql;
using System.Text.Json;
using System.Text.Json.Serialization;
using static BackupBot.Bot.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace BackupBot.Bot
{
    public sealed partial class Database : IDatabase
    {
        private ILogger<Database> Logger { get; init; }
        private readonly IConfiguration Configuration;

        public Database(ILogger<Database> logger, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
            Logger = logger;
            Configuration = configuration;
        }

        public async Task CreateTables()
        {
            using NpgsqlConnection con = GetConnection();

            await using var cmd = new NpgsqlBatch(con)
            {
                BatchCommands =
                {
                    new ("CREATE TABLE IF NOT EXISTS public.users ( userid bigint NOT NULL, joindate timestamp with time zone NOT NULL, CONSTRAINT users_pkey PRIMARY KEY (userid) )"),
                    new ("CREATE TABLE IF NOT EXISTS public.guilds ( guildid bigint NOT NULL, ownerid bigint NOT NULL, joindate timestamp with time zone NOT NULL, CONSTRAINT guilds_pkey PRIMARY KEY (guildid), CONSTRAINT \"guids_ownerid_FKEY\" FOREIGN KEY (ownerid) REFERENCES public.users (userid) MATCH SIMPLE ON UPDATE NO ACTION ON DELETE CASCADE NOT VALID ) "),
                    new("CREATE TABLE IF NOT EXISTS public.backups ( backupid integer NOT NULL DEFAULT nextval('backups_backupid_seq'::regclass), guildid bigint NOT NULL, creatorid bigint NOT NULL, guildname text COLLATE pg_catalog.\"default\" NOT NULL, guildinfo jsonb, channels jsonb, roles jsonb, creationdate timestamp with time zone NOT NULL, comment text COLLATE pg_catalog.\"default\", CONSTRAINT backups_pkey PRIMARY KEY (backupid), CONSTRAINT \"guilds_guildid_FKEY\" FOREIGN KEY (guildid) REFERENCES public.guilds (guildid) MATCH SIMPLE ON UPDATE NO ACTION ON DELETE CASCADE, CONSTRAINT \"users_creatorid_FKEY\" FOREIGN KEY (creatorid) REFERENCES public.users (userid) MATCH SIMPLE ON UPDATE NO ACTION ON DELETE CASCADE )")
                }
            };

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
            await con.CloseAsync();
        }

        public async Task<bool> GuildExists(ulong guildId)
        {
            using NpgsqlConnection con = GetConnection();
            string sql = "select exists (select 1 from guilds where guildid = $1);";

            await using var cmd = new NpgsqlCommand(sql, con)
            {
                Parameters =
                {
                    new() { Value = (long)guildId }
                }
            };

            await con.OpenAsync();
            await cmd.PrepareAsync();
            var value = await cmd.ExecuteScalarAsync();
            await con.CloseAsync();
            return Convert.ToBoolean(value!.ToString());
        }

        public async Task<Backup> GetBackup(int backupId, ulong userId)
        {
            using NpgsqlConnection con = GetConnection();
            string sql = "select * from backups where backupid = $1 and creatorid = $2;";

            await using var cmd = new NpgsqlCommand(sql, con)
            {
                Parameters =
                {
                    new() { Value = backupId },
                    new() { Value = (long)userId }
                }
            };

            await con.OpenAsync();
            await cmd.PrepareAsync();

            await using var reader = await cmd.ExecuteReaderAsync();

            try
            {
                Backup backup = new();

                while (await reader.ReadAsync())
                {
                    //backup = new(Convert.ToInt32(reader["backupid"]), Convert.ToUInt64(reader["guildid"]),
                    //             Convert.ToUInt64(reader["creatorid"]), (string)reader["guildname"], JsonConvert.DeserializeObject<Guild>((string)reader["guildinfo"]),
                    //             JsonConvert.DeserializeObject<Channels>((string)reader["channels"]),
                    //             JsonConvert.DeserializeObject<List<Channel>>((string)reader["channels"]), 
                    //             JsonConvert.DeserializeObject<List<Role>>((string)reader["roles"]), (Instant)reader["creationdate"], reader["comment"] == DBNull.Value ? string.Empty : (string)reader["comment"]);

                    backup = new(Convert.ToInt32(reader["backupid"]), Convert.ToUInt64(reader["guildid"]), Convert.ToUInt64(reader["creatorid"]), (string)reader["guildname"],
                                 JsonConvert.DeserializeObject<Guild>((string)reader["guildinfo"]), JsonConvert.DeserializeObject<List<Channel>>((string)reader["channels"]),
                                 JsonConvert.DeserializeObject<List<Role>>((string)reader["roles"]), (Instant)reader["creationdate"], reader["comment"] == DBNull.Value ? string.Empty : (string)reader["comment"]);
                }

                return backup;
            }
            catch
            {
                return new Backup();
            }
            finally
            {
                await con.CloseAsync();
            }
        }

        public async Task<int> GetGuildBackups(ulong serverid)
        {
            using NpgsqlConnection con = GetConnection();
            string sql = "select MAX(backupid) from public.backups where guildid = $1";

            await using var cmd = new NpgsqlCommand(sql, con)
            {
                Parameters =
                {
                    new() { Value = (long)serverid }
                }
            };

            await con.OpenAsync();
            await cmd.PrepareAsync();
            var reader = await cmd.ExecuteScalarAsync();
            await con.CloseAsync();
            return Convert.ToInt32(reader);
        }

        public async Task InsertUser(User user)
        {
            using NpgsqlConnection con = GetConnection();
            string sql = "insert into users (userid, joindate) values ($1, $2) on conflict do nothing;";

            await using var cmd = new NpgsqlCommand(sql, con)
            {
                Parameters =
                {
                    new() { Value = (long)user.UserId },
                    new() { Value = user.JoinDate }
                }
            };
            await con.OpenAsync();
            await cmd.PrepareAsync();
            await cmd.ExecuteNonQueryAsync();
            await con.CloseAsync();
        }

        public async Task InsertBackup(Backup backup)
        {
            using NpgsqlConnection con = GetConnection();
            string sql = "insert into backups (guildid, creatorid, guildname, guildinfo, channels, roles, creationdate, comment) values ($1, $2, $3, $4, $5, $6, $7, $8);";

            await using var cmd = new NpgsqlCommand(sql, con)
            {
                Parameters =
                {
                    new() { Value = (long)backup.ServerId },
                    new() { Value = (long)backup.CreatorId },
                    new() { Value = backup.ServerName },
                    new() { Value = JsonSerializer.Serialize(backup.ServerInfo), NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb },
                    new() { Value = JsonSerializer.Serialize(backup.Channels), NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb },
                    new() { Value = JsonSerializer.Serialize(backup.Roles), NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb  },
                    new() { Value = backup.CreationDate },
                    new() { Value = backup.Comment != null ? backup.Comment : DBNull.Value }
                }
            };

            await con.OpenAsync();
            await cmd.PrepareAsync();
            await cmd.ExecuteNonQueryAsync();
            await con.CloseAsync();
        }

        public async Task InsertGuild(ulong guildId, ulong ownerId, Instant joinDate)
        {
            using NpgsqlConnection con = GetConnection();
            string sql = "insert into guilds (guildid, ownerid, joindate) values ($1, $2, $3) on conflict do nothing;";

            await using var cmd = new NpgsqlCommand(sql, con)
            {
                Parameters =
                {
                    new() { Value = (long)guildId },
                    new() { Value = (long)ownerId },
                    new() { Value = joinDate.ToDateTimeUtc() }
                }
            };

            await con.OpenAsync();
            await cmd.PrepareAsync();
            await cmd.ExecuteNonQueryAsync();
            await con.CloseAsync();
        }

        public async Task DeleteGuild(ulong guildId)
        {
            using NpgsqlConnection con = GetConnection();
            string sql = "delete from guilds where guildid = $1;";

            await using var cmd = new NpgsqlCommand(sql, con)
            {
                Parameters =
                {
                    new() { Value = (long)guildId }
                }
            };

            await con.OpenAsync();
            await cmd.PrepareAsync();
            await cmd.ExecuteNonQueryAsync();
            await con.CloseAsync();
        }

        public async Task<User> GetUser(ulong userId)
        {
            using NpgsqlConnection con = GetConnection();
            string sql = "select * from users where users.userid = $1;";

            await using var cmd = new NpgsqlCommand(sql, con)
            {
                Parameters =
                {
                    new() { Value = (long)userId }
                }
            };

            await con.OpenAsync();
            await cmd.PrepareAsync();

            await using var reader = await cmd.ExecuteReaderAsync();

            try
            {
                User user = new();

                while (await reader.ReadAsync())
                {
                    user = new(Convert.ToUInt64(reader["userid"]), (Instant)reader["joindate"]);
                }

                return user;
            }
            catch
            {
                return new User();
            }
            finally
            {
                await con.CloseAsync();
            }

        }

        public async Task<List<Backup>> GetBackups(ulong serverId)
        {
            using NpgsqlConnection con = GetConnection();
            string sql = "SELECT * FROM backups WHERE backups.guildid = $1";

            await using var cmd = new NpgsqlCommand(sql, con)
            {
                Parameters =
                    {
                        new() { Value = (long)serverId }
                    }
            };

            List<Backup> backups = new();

            await con.OpenAsync();
            await cmd.PrepareAsync();
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                backups.Add(new Backup((int)reader["backupid"], Convert.ToUInt64(reader["guildid"]), Convert.ToUInt64(reader["creatorid"]), string.Empty, null, null, null, (Instant)reader["creationdate"], null));
            }

            await con.CloseAsync();

            return backups;
        }

        public NpgsqlConnection GetConnection()
        {
            var config = Configuration.GetSection(nameof(DiscordConfig));
            return new NpgsqlConnection(config.GetValue<string>("DbString"));
        }
    }
}
