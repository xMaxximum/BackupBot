using BackupBot.Bot.Backups;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Enums;
using DisCatSharp.Interactivity.Extensions;
using System.Text;

namespace BackupBot.Bot.Commands
{
    public class BackupCommand : ApplicationCommandsModule
    {
        [SlashCommandGroup("backup", "Different commands related to backups")]
        public class Back : ApplicationCommandsModule
        {
            public IDatabase Database { private get; init; } = null!;
            public ITakeBackup BackupMaker { private get; init; } = null!;
            public IRestoreBackup RestoreMaker { private get; init; } = null!;

            [SlashCommand("info", "Get information about a backup"), RequireGuildOwner, RequireDbGuild]
            public async Task Info(InteractionContext context, [Option("backupid", "The id of the backup you want info about")] int backupId)
            {
                await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                var backup = await Database.GetBackup(backupId, context.User.Id);

                if (backup.BackupId != 0)
                {
                    string guildName = $"Server: {backup.ServerName}\nServer ID: {backup.ServerId}\n";
                    string guildInfo = backup.ServerInfo != null ? "Server Info: ✅\n" : "Server Info: ❌\n";
                    string channels = backup.Channels != null ? $"Channels: {backup.Channels.Count}\n" : "Channels: ❌";
                    string roles = backup.Roles != null ? $"Roles: {backup.Roles.Count}\n" : "Roles: ❌\n";
                    string comment = backup.Comment!.Any() ? backup.Comment + '\n' : "*No comment*\n";

                    var startPath = Path.Combine(Environment.CurrentDirectory, "download");
                    StringBuilder sb = new("Assets: ");

                    if (Directory.Exists(Path.Combine(startPath, "icons", $"{backup.ServerId}")))
                    {
                        if (Directory.GetFiles(Path.Combine(startPath, "icons", $"{backup.ServerId}"), $"{backup.ServerId}_{backupId}*").Any()) sb.Append("Icon, ");
                    }
                    if (Directory.Exists(Path.Combine(startPath, "emotes", $"{backup.ServerId}")))
                    {
                        string[] files = Directory.GetFiles(Path.Combine(startPath, "emotes", $"{backup.ServerId}"), $"{backup.ServerId}_{backup.BackupId}_*_*");
                        if (files.Length > 0) sb.Append($"Emotes ({files.Length}), ");
                    }
                    if (Directory.Exists(Path.Combine(startPath, "stickers", $"{backup.ServerId}")))
                    {
                        string[] files = Directory.GetFiles(Path.Combine(startPath, "stickers", $"{backup.ServerId}"), $"{backup.ServerId}_{backup.BackupId}_*_*");
                        if (files.Length > 0) sb.Append($"Stickers ({files.Length}), ");
                    }
                    if (Directory.Exists(Path.Combine(startPath, "banners", $"{backup.ServerId}")))
                    {
                        if (Directory.GetFiles(Path.Combine(startPath, "banners", $"{backup.ServerId}"), $"{backup.ServerId}_{backupId}*").Any()) sb.Append("Banner, ");
                    }
                    if (Directory.Exists(Path.Combine(startPath, "splash", $"{backup.ServerId}")))
                    {
                        if (Directory.GetFiles(Path.Combine(startPath, "splash", $"{backup.ServerId}"), $"{backup.ServerId}_{backupId}*").Any()) sb.Append("Splash, ");
                    }
                    if (Directory.Exists(Path.Combine(startPath, "discoverysplash", $"{backup.ServerId}")))
                    {
                        if (Directory.GetFiles(Path.Combine(startPath, "discoverysplash", $"{backup.ServerId}"), $"{backup.ServerId}_{backupId}*").Any()) sb.Append("Discovery Splash");
                    }
                    if (sb.Length == 8) sb.Append('❌');

                    var interactivity = context.Client.GetInteractivity();
                    List<Page> pages = new();
                    var firstPage = (List<Page>)interactivity.GeneratePagesInEmbed($"{guildName}Taken: {Util.Timestamp(backup.CreationDate)}\nComment: {comment}{guildInfo}{channels}{roles}{sb.ToString().Trim().TrimEnd(',')}",
                                    embedBase: new() { Title = $"Backup `{backup.BackupId}`" });
                    List<Page> rolePages = new();
                    List<Page> channelPages = new();
                    pages.AddRange(firstPage);

                    if (backup.Channels!.Any())
                    {
                        channelPages = (List<Page>)interactivity.GeneratePagesInEmbed(string.Join("\n", backup.Channels!.Select(entry => entry.Name)), embedBase: new() { Title = $"Backup `{backup.BackupId}` - Channels" }, splitType: SplitType.Line);
                        pages.AddRange(channelPages);
                    }
                    if (backup.Roles!.Any())
                    {
                        rolePages = (List<Page>)interactivity.GeneratePagesInEmbed(string.Join("\n", backup.Roles!.Select(entry => entry.Name)), embedBase: new() { Title = $"Backup `{backup.BackupId}` - Roles" }, splitType: SplitType.Line);
                        pages.AddRange(rolePages);
                    }

                    await context.DeleteResponseAsync();
                    await context.Channel.SendPaginatedMessageAsync(context.User, pages.Recalculate());
                }
                else
                {
                    await context.EditResponseAsync(new DiscordWebhookBuilder()
                    {
                        Content = "The specified backup doesn't exist or wasn't made by you!"
                    });
                }
            }

            [SlashCommand("list", "List backups for this server"), RequireGuildOwner, RequireDbGuild]
            public async Task List(InteractionContext context)
            {
                await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                var backups = Database.GetBackups(context.Guild.Id).Result.OrderByDescending(entry => entry.CreationDate);

                if (backups.Any())
                {
                    StringBuilder sb = new();

                    foreach (var backup in backups)
                    {
                        sb.AppendLine($":arrow_forward:  `{backup.BackupId}` {Util.Timestamp(backup.CreationDate)}");
                    }

                    if (sb.Length > 600)
                    {
                        var interactivity = context.Client.GetInteractivity();
                        var pages = interactivity.GeneratePagesInEmbed(sb.ToString(), embedBase: new DiscordEmbedBuilder()
                        {
                            Title = $"{context.Guild.Name}'s Backups"
                        }, splitType: SplitType.Line);


                        await context.DeleteResponseAsync();
                        await context.Channel.SendPaginatedMessageAsync(context.Member, pages);
                    }
                    else
                    {
                        await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                        {
                            Title = $"{context.Guild.Name}'s Backups",
                            Description = sb.ToString(),
                            Footer = new DiscordEmbedBuilder.EmbedFooter() { Text = "Page 1/1 " }
                        }));
                    }
                }
                else
                {
                    await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                    {
                        Title = $"{context.Guild.Name}'s Backups",
                        Description = "*No backups found!*"
                    }));
                }
            }

            [SlashCommand("take", "Take a backup"), RequireGuildOwner, RequireDbGuild]
            public async Task Take(InteractionContext context, [Option("comment", "Include a comment for the backup."), MaximumLength(100)] string? comment = null)
            {
                await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                if (Database.GetUser(context.User.Id).Result.UserId.ToString().Any())
                {
                    var options = new DiscordStringSelectComponentOption[]
                    {
                        new DiscordStringSelectComponentOption("Guild info", "guild_info", "Should the guild info be backed up?"),
                        new DiscordStringSelectComponentOption("Channels", "channels", "Should channels be backed up?"),
                        new DiscordStringSelectComponentOption("Roles", "roles", "Should roles be backed up?"),
                        new DiscordStringSelectComponentOption("Assets", "assets", "Should assets be backed up?")
                    };

                    var selectMenu = new DiscordStringSelectComponent("Select what you want to backup", options, "backup_select", 1, 4);
                    var button = new DiscordButtonComponent(ButtonStyle.Primary, "backups_all", "Backup everything", false, null!);

                    var msg = await context.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Select the things you'd like to backup in the select menu below!")
                                    .AddComponents(selectMenu).AddComponents(button));


                    _ = Task.Run(async () =>
                    {
                        var selectResponse = await context.Client.GetInteractivity().WaitForSelectAsync(msg, "backup_select", ComponentType.StringSelect, TimeSpan.FromSeconds(30));
                        if (!selectResponse.TimedOut)
                        {
                            await selectResponse.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                            bool guildInfo = false;
                            bool channels = false;
                            bool roles = false;
                            bool assets = false;

                            foreach (var item in selectResponse.Result.Values)
                            {
                                switch (item)
                                {
                                    case "guild_info":
                                        guildInfo = true;
                                        break;
                                    case "channels":
                                        channels = true;
                                        break;
                                    case "roles":
                                        roles = true;
                                        break;
                                    case "assets":
                                        assets = true;
                                        break;
                                    default:
                                        break;
                                }
                            }

                            BackupMaker.Take(context, guildInfo, channels, roles, assets, comment);
                            await context.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder()
                            {
                                Content = "Successfully created a backup!"
                            });
                        }
                    });

                    _ = Task.Run(async () =>
                    {
                        var buttonResponse = await context.Client.GetInteractivity().WaitForButtonAsync(msg, context.User, TimeSpan.FromSeconds(30));
                        if (!buttonResponse.TimedOut)
                        {
                            await buttonResponse.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                            BackupMaker.Take(context, true, true, true, true, comment);
                            await context.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder()
                            {
                                Content = "Successfully created a backup!"
                            });
                        }
                    });
                }
            }

            [SlashCommand("restore", "Restore a previously made backup"), RequireGuildOwner, RequireDbGuild]
            public async Task Restore(InteractionContext context, [Option("id", "The backup ID you'd like to restore.")] int backupId,
                                      [Option("cleanall", "Do you want to delete everything before restoring?")] bool cleanAll)
            {
                await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                if (Database.GetUser(context.User.Id).Result.UserId.ToString().Any())
                {
                    var options = new List<DiscordStringSelectComponentOption>();
                    var startPath = Path.Combine(Environment.CurrentDirectory, "download");

                    var backup = await Database.GetBackup(backupId, context.User.Id);

                    if (backup.ServerInfo != null) options.Add(new DiscordStringSelectComponentOption("Guild info", "guild_info", "Should the guild info be restored?"));
                    if (backup.Channels != null) options.Add(new DiscordStringSelectComponentOption("Channels", "channels", "Should the channels be restored?"));
                    if (backup.Roles != null) options.Add(new DiscordStringSelectComponentOption("Roles", "roles", "Should the roles be restored?"));
                    #region checkAssets
                    if (Directory.Exists(Path.Combine(startPath, "icons", $"{backup.ServerId}")))
                    {
                        if (Directory.GetFiles(Path.Combine(startPath, "icons", $"{backup.ServerId}"), $"{backup.ServerId}_{backupId}*").Any())
                            options.Add(new DiscordStringSelectComponentOption("Assets", "assets", "Should the assets be restored?"));
                    }
                    else if (Directory.Exists(Path.Combine(startPath, "emotes", $"{backup.ServerId}")))
                    {
                        if (Directory.GetFiles(Path.Combine(startPath, "emotes", $"{backup.ServerId}"), $"{backup.ServerId}_{backupId}*").Any())
                            options.Add(new DiscordStringSelectComponentOption("Assets", "assets"));
                    }
                    else if (Directory.Exists(Path.Combine(startPath, "stickers", $"{backup.ServerId}")))
                    {
                        if (Directory.GetFiles(Path.Combine(startPath, "stickers", $"{backup.ServerId}"), $"{backup.ServerId}_{backupId}*").Any())
                            options.Add(new DiscordStringSelectComponentOption("Assets", "assets"));
                    }
                    else if (Directory.Exists(Path.Combine(startPath, "banners", $"{backup.ServerId}")))
                    {
                        if (Directory.GetFiles(Path.Combine(startPath, "banners", $"{backup.ServerId}"), $"{backup.ServerId}_{backupId}*").Any())
                            options.Add(new DiscordStringSelectComponentOption("Assets", "assets"));
                    }
                    else if (Directory.Exists(Path.Combine(startPath, "splash", $"{backup.ServerId}")))
                    {
                        if (Directory.GetFiles(Path.Combine(startPath, "splash", $"{backup.ServerId}"), $"{backup.ServerId}_{backupId}*").Any())
                            options.Add(new DiscordStringSelectComponentOption("Assets", "assets"));
                    }
                    else if (Directory.Exists(Path.Combine(startPath, "discoverysplash", $"{backup.ServerId}")))
                    {
                        if (Directory.GetFiles(Path.Combine(startPath, "discoverysplash", $"{backup.ServerId}"), $"{backup.ServerId}_{backupId}*").Any())
                            options.Add(new DiscordStringSelectComponentOption("Assets", "assets"));
                    }
                    #endregion checkAssets

                    var selectMenu = new DiscordStringSelectComponent("Select what you'd like to restore", options, "restore_select", 1, options.Count);
                    var button = new DiscordButtonComponent(ButtonStyle.Primary, "restore_all", "Restore everything", false, null!);

                    var msg = await context.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Select the things you'd like to restore in the select menu below!")
                                    .AddComponents(selectMenu).AddComponents(button));

                    _ = Task.Run(async () =>
                    {
                        var selectResponse = await context.Client.GetInteractivity().WaitForSelectAsync(msg, "restore_select", ComponentType.StringSelect, TimeSpan.FromSeconds(30));
                        if (!selectResponse.TimedOut)
                        {
                            await selectResponse.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                            bool guildInfo = false;
                            bool channels = false;
                            bool roles = false;
                            bool assets = false;

                            foreach (var item in selectResponse.Result.Values)
                            {
                                switch (item)
                                {
                                    case "guild_info":
                                        guildInfo = true;
                                        break;
                                    case "channels":
                                        channels = true;
                                        break;
                                    case "roles":
                                        roles = true;
                                        break;
                                    case "assets":
                                        assets = true;
                                        break;
                                    default:
                                        break;
                                }
                            }

                            button.Disable();
                            selectMenu.Disable();
                            await context.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Restoring....").AddComponents(selectMenu).AddComponents(button));
                            RestoreMaker.Restore(context, guildInfo, channels, roles, assets, backup, cleanAll);
                            return;
                        }
                        else
                        {
                            button.Disable();
                            selectMenu.Disable();
                            await context.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Timed out!").AddComponents(selectMenu).AddComponents(button));
                        }
                    });

                    _ = Task.Run(async () =>
                    {
                        var buttonResponse = await context.Client.GetInteractivity().WaitForButtonAsync(msg, context.User, TimeSpan.FromSeconds(30));
                        if (!buttonResponse.TimedOut)
                        {
                            await buttonResponse.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                            button.Disable();
                            selectMenu.Disable();
                            await context.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Restoring...").AddComponents(selectMenu).AddComponents(button));
                            RestoreMaker.Restore(context, true, true, true, true, backup, cleanAll);
                            return;
                        }
                        else
                        {
                            button.Disable();
                            selectMenu.Disable();
                            await context.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Timed out!").AddComponents(selectMenu).AddComponents(button));
                        }
                    });
                }
            }
        }
    }
}
