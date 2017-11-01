using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord.Commands;

namespace RaidMax.DiscordBot.Commands
{
    class OsuApiStats
    {
        public int user_id;
        public string username;
        public int count300;
        public int count100;
        public int count50;
        public int playcount;
        public long ranked_score;
        public long total_score;
        public int pp_rank;
        public float level;
        public float pp_raw;
        public float accuracy;
        public int count_rank_ss;
        public int count_rank_s;
        public int count_rank_a;
        public string country;
        public int pp_country_rank;
        public List<OsuApiEvent> events;
    }

    class OsuApiUserBest
    {
        public int beatmap_id;
        public long score;
        public string username;
        public int maxcombo;
        public int count300;
        public int count100;
        public int count50;
        public int countmiss;
        public int countkatu;
        public int countgeki;
        public int perfect;
        public Mods enabled_mods;
        public int user_id;
        public DateTime date;
        public string rank;
        public float pp;
    }

    class OsuApiBeatmap
    {
        public short approved;
        public DateTime approved_date;
        public DateTime last_update;
        public string artist;
        public int beatmap_id;
        public int beatmapset_id;
        public float bpm;
        public string creator;
        public float difficultyrating;
        public float diff_size;
        public float diff_overall;
        public float diff_approach;
        public float diff_drain;
        public short hit_length;
        public string source;
        public short genre_id;
        public short language_id;
        public string title;
        public int total_length;
        public string version;
        public string file_md5;

        public short mode;
        public string tags;
        public int favourite_count;
        public int playcount;
        public int passcount;
        public int max_combo;
    }

    enum Mods
    {
        None = 0,
        NoFail = 1,
        Easy = 2,
        //NoVideo      = 4,
        Hidden = 8,
        HardRock = 16,
        SuddenDeath = 32,
        DoubleTime = 64,
        Relax = 128,
        HalfTime = 256,
        Nightcore = 512, // Only set along with DoubleTime. i.e: NC only gives 576
        Flashlight = 1024,
        Autoplay = 2048,
        SpunOut = 4096,
        Relax2 = 8192,  // Autopilot?
        Perfect = 16384,
        Key4 = 32768,
        Key5 = 65536,
        Key6 = 131072,
        Key7 = 262144,
        Key8 = 524288,
        keyMod = Key4 | Key5 | Key6 | Key7 | Key8,
        FadeIn = 1048576,
        Random = 2097152,
        LastMod = 4194304,
        FreeModAllowed = NoFail | Easy | Hidden | HardRock | SuddenDeath | Flashlight | FadeIn | Relax | Relax2 | SpunOut | keyMod,
        Key9 = 16777216,
        Key10 = 33554432,
        Key1 = 67108864,
        Key3 = 134217728,
        Key2 = 268435456
    }

    class OsuApiEvent
    {
        public string display_html;
        public int beatmap_id;
        public int beatmapset_id;
        public DateTime date;
        public short epicfactor;
    }

    [Group("osu"), Summary("Get info related to osu! and osu! accounts")]
    public class OsuStats : ModuleBase
    {
        [Command("stats"), Summary("Get statistics for specified user")]
        public async Task Stats([Remainder] string username)
        {
            using (var webClient = new System.Net.Http.HttpClient())
            {
                string response = await webClient.GetStringAsync($"https://osu.ppy.sh/api/get_user?k=c6a3eca7568f5370ee345dd8ec0cd488099a5eae&u={username}&event_days=5");
                string bestMapsResponse = await webClient.GetStringAsync($"https://osu.ppy.sh/api/get_user_best?k=c6a3eca7568f5370ee345dd8ec0cd488099a5eae&u={username}&limit=3");
                var StatObject = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OsuApiStats>>(response);
                var BestMapsObject = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OsuApiUserBest>>(bestMapsResponse);

                List<int> ids = new List<int>();
                foreach (var b in BestMapsObject)
                    ids.Add(b.beatmap_id);

                var beatmapsInfo = await GetOsuBeatmaps(ids, webClient);

                var User = StatObject.FirstOrDefault();
                if (User == null)
                {
                    await Context.Channel.SendMessageAsync("No user with that username found");
                    return;
                }

                StringBuilder s = new StringBuilder();
                s.Append($"{Environment.NewLine}```{Environment.NewLine}Stats for {User.username}{Environment.NewLine}");
                s.Append($"--------------------------------{Environment.NewLine}");
                s.Append($"Rank       --- #{User.pp_rank:n0}{Environment.NewLine}");
                s.Append($"Play Count --- {User.playcount:n0}{Environment.NewLine}");
                s.Append($"Accuracy   --- {Math.Round(User.accuracy, 2)}{Environment.NewLine}");
                s.Append($"Level      --- {User.level:n}{Environment.NewLine}");
                s.Append($"PP         --- {User.pp_raw:n0}{Environment.NewLine}");
                s.Append($"--------------------------------{Environment.NewLine}");

                int i = 0;
                foreach(var bm in beatmapsInfo)
                {
                    s.Append($"[{i + 1}] - {bm.title} | {bm.creator} | {bm.version} | { GetModsString(BestMapsObject[i].enabled_mods) } {Environment.NewLine}");
                    s.Append($"\t\t\"{ BestMapsObject[i].rank}\" - {Math.Round(BestMapsObject[i].pp, 2)}pp - ({BestMapsObject[i].count300}, {BestMapsObject[i].count100}, {BestMapsObject[i].count50}, {BestMapsObject[i].countmiss}) - {BestMapsObject[i].maxcombo} combo{Environment.NewLine}");
                    i++;
                }

                s.Append("```");

                
                await Context.Channel.SendMessageAsync(s.ToString());
            }
        }

        private string GetModsString(Mods m)
        {
            StringBuilder modString = new StringBuilder();

            if ((m & Mods.NoFail) == Mods.NoFail)
                modString.Append(" +NF");
            if ((m & Mods.Easy) == Mods.Easy)
                modString.Append(" +EZ");
            if ((m & Mods.Hidden) == Mods.Hidden)
                modString.Append(" +HD");
            if ((m & Mods.HardRock) == Mods.HardRock)
                modString.Append(" +HR");
            if ((m & Mods.SuddenDeath) == Mods.SuddenDeath)
                modString.Append(" +SD");
            if ((m & Mods.DoubleTime) == Mods.DoubleTime)
                modString.Append(" +DT");
            if ((m & Mods.Relax) == Mods.Relax)
                modString.Append(" +RX");
            if ((m & Mods.HalfTime) == Mods.HalfTime)
                modString.Append(" +HT");
            if ((m & Mods.Nightcore) == Mods.Nightcore)
                modString.Append(" +NC");
            if ((m & Mods.Flashlight) == Mods.Flashlight)
                modString.Append(" +FL");
            if ((m & Mods.SpunOut) == Mods.SpunOut)
                modString.Append(" +SO");

            string result = modString.ToString();
            return (result.Length != 0) ? "[" + result.Substring(1) + "]" : "";
        }

        private async Task<List<OsuApiBeatmap>> GetOsuBeatmaps(List<int> ids, System.Net.Http.HttpClient cl)
        {
            List<OsuApiBeatmap> beatmaps = new List<OsuApiBeatmap>();
            foreach (int i in ids)
            {
                string beatmapText = await cl.GetStringAsync($"https://osu.ppy.sh/api/get_beatmaps?k=c6a3eca7568f5370ee345dd8ec0cd488099a5eae&b={i}&limit=1");
                var beatmapObject = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OsuApiBeatmap>>(beatmapText);
                beatmaps.Add(beatmapObject.FirstOrDefault());
            }
            return beatmaps;
        }
    }
}
