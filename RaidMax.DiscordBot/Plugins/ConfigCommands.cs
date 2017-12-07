using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace RaidMax.DiscordBot.Plugins
{

    [Group("config"), Summary("set configuration settings for the bot")]
    public class ConfigCommands : ModuleBase
    {
        [Command("voicenotify"), Summary("set the voice notify config value")]
        public async Task VoiceNotify(string parse = null)
        {
            bool curSetting = Main.DiscordClient.Configurations[Context.Guild.Id].AnnounceVoiceChannels;
            string label = curSetting ? "on" : "off";

            if (parse == null)
            {
                await Context.Channel.SendMessageAsync($"Voice channel notifies are currently {label}");
                return;
            }

            bool enabled = parse == "on" ? true : false;
            Main.DiscordClient.Configurations[Context.Guild.Id].AnnounceVoiceChannels = enabled;
            await Helpers.Config.Write(Context.Guild.Id, Main.DiscordClient.Configurations[Context.Guild.Id]);
            label = enabled ? "on" : "off";
            await Context.Channel.SendMessageAsync($"Voice channel notifies are now {label}");
        }

        [Command("nsfwchannel"), Summary("set the nsfw channel for memes")]
        public async Task NSFWChannel(ulong ChannelId)
        {
            try
            {
                var channel = (await Context.Guild.GetChannelsAsync())
                    .Single(c => c.Id == ChannelId);

                Main.DiscordClient.Configurations[Context.Guild.Id].NSFWChannelId = channel.Id;
                await Helpers.Config.Write(Context.Guild.Id, Main.DiscordClient.Configurations[Context.Guild.Id]);
                await Context.Channel.SendMessageAsync($"NSFW channel is now `{channel.Name}`");
            }

            catch (InvalidOperationException)
            {
                await Context.Channel.SendMessageAsync("Invalid channel");
            }
        }
    }
}
