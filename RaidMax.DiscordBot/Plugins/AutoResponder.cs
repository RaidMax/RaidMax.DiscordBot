using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

using Discord.Commands;

namespace RaidMax.DiscordBot.Commands
{
    [Group("autorespond"), Summary("Auto responds to message if matching phrase is found")]
    public class AutoResponder : ModuleBase
    {
        public static Dictionary<ulong, Dictionary<string, List<string>>> Responses = new Dictionary<ulong, Dictionary<string, List<string>>>();

        [Command("add"), Summary("Add a new response to matching phrase")]
        public async Task Add([Remainder] string text)
        {
            var split = text.Split(new string[] { "::" }, StringSplitOptions.None);

            if (split.Length == 2 && split[0].Length > 2)
            {
                if (split[0].Length > 16)
                {
                    await Context.Channel.SendMessageAsync("That autorespond trigger is too long");
                    return;
                }

                if (split[1].Length > 48)
                {
                    await Context.Channel.SendMessageAsync("That autorespond response is too long");
                    return;
                }

                await AddResponsesAsync(new string[] { split[0].ToLower() }, split[1].Split('|'));
                await Context.Channel.SendMessageAsync($"Added response for phrase {split[0]}");
            }
        }

        [Command("delete"), Summary("Delete an auto response by index")]
        public async Task Delete(int phrase, int response = -1)
        {
            if (Context.Guild == null)
                return;

            ulong guildID = Context.Guild.Id;
            await Task.Run(() =>
            {
                try
                {
                    if (phrase > -1 && response < 0)
                    {
                        Responses[guildID].Remove(Responses[guildID].ElementAt(phrase).Key);
                        Context.Channel.SendMessageAsync($"Deleted phrase at index {phrase}");
                    }
                    if (response > -1 && phrase > -1)
                    {
                        Responses[guildID].ElementAt(phrase).Value.RemoveAt(response);
                        Context.Channel.SendMessageAsync($"Deleted response at index {response}");
                    }
                }

                catch (ArgumentOutOfRangeException)
                {
                    Context.Channel.SendMessageAsync("Invalid phrase or response index");
                }
                SerializeResponses(Context.Guild.Id);
            });
        }

        [Command("list"), Summary("List all matching phrases and responses")]
        public async Task List()
        {
            if (Context.Guild == null)
                return;

            ulong guildID = Context.Guild.Id;
            await Task.Run(() =>
            {
                if (!Responses.ContainsKey(guildID))
                    Responses.Add(guildID, DeserializeResponses(guildID));

                if (Responses[guildID].Count == 0)
                {
                    Context.Channel.SendMessageAsync("No auto responders added");
                    return;
                }

                int i = 0;
                int j = 0;
                StringBuilder s = new StringBuilder();
                s.Append($"{Environment.NewLine}```{Environment.NewLine}");
                foreach (string key in Responses[guildID].Keys)
                {
                    s.Append($"[{i}] - {key}{Environment.NewLine}");
                    i++;
                    j = 0;
                    foreach (string response in Responses[guildID][key])
                    {
                        s.Append($"\t[{j}] => {response}{Environment.NewLine}");
                        j++;
                    }
                }
                s.Append("```");

                Context.Channel.SendMessageAsync(s.ToString());
            });
        }

        private async Task AddResponsesAsync(string[] matches, string[] responses)
        {
            if (Context.Guild == null)
                return;

            ulong guildID = Context.Guild.Id;
            await Task.Run(() =>
            {
                if (!Responses.ContainsKey(guildID))
                    Responses.Add(guildID, DeserializeResponses(guildID));

                foreach (string match in matches)
                {
                    if (Responses[guildID].ContainsKey(match))
                    {
                        foreach (string response in responses)
                            Responses[guildID][match].Add(response);
                    }

                    else
                    {
                        Responses[guildID].Add(match.ToLower(), responses.ToList());
                    }
                }

                SerializeResponses(Context.Guild.Id);
            });
        }

        private static void SerializeResponses(ulong guildID)
        {
            string s = JsonConvert.SerializeObject(Responses);
            File.WriteAllText($"AutoResponder_{guildID}.json", s);
        }

        public static Dictionary<string, List<string>> DeserializeResponses(ulong guildID)
        {
            try
            {
                string s = File.ReadAllText($"AutoResponder_{guildID}.json");
                return JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(s);
            }

            catch(Exception)
            {
                return new Dictionary<string, List<string>>();
            }
        }
    }
}
