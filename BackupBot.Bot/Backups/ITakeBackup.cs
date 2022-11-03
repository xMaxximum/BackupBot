using DisCatSharp.ApplicationCommands.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupBot.Bot.Backups
{
    public interface ITakeBackup
    {
        void Take(InteractionContext context, bool? guildinfo, bool? channels, bool? roles, bool? assets, string? comment);
    }
}
