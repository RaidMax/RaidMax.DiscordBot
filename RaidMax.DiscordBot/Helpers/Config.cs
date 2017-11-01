using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace RaidMax.DiscordBot.Helpers
{
    class Config
    {
        public bool AnnounceVoiceChannels;
        public List<string> SubredditList;

        public Config()
        {
            AnnounceVoiceChannels = true;
            SubredditList = new List<string>();
        }

        public static async Task Write(ulong guildID, Config conf)
        {
            await Task.Run(() =>
            {
                string s = Newtonsoft.Json.JsonConvert.SerializeObject(conf);
                File.WriteAllText($"Config_{guildID}.json", s);
            });
        }

        public static async Task<Config> Read(ulong guildID)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    string s = File.ReadAllText($"Config_{guildID}.json");
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(s);
                }

                catch (Exception)
                {
                    var conf = new Config();
                    await Write(guildID, conf);
                    return conf;
                }
            });
        }
    }
}
