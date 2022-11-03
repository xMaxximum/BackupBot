using DisCatSharp.ApplicationCommands.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupBot.Bot.Backups
{
    public interface IRestoreBackup
    {
        void Restore(InteractionContext context, bool guildInfo, bool channels, bool roles, bool assets, Models.Backup backup, bool cleanAll);
    }
}
