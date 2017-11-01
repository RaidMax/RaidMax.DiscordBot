using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Discord.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Discord;
using RaidMax.DiscordBot.Helpers;

namespace RaidMax.DiscordBot.Plugins
{
    public class InsultGenerator : ModuleBase
    {
        static DateTime lastUpdateInsult = DateTime.MinValue;
        static DateTime lastUpdateCompliments = DateTime.MinValue;
        const int THREAD_COUNT = 30;

        static List<string> Insults = new List<string>();
        static int InsultIndex = 0;
        static List<string> Compliments = new List<string>();
        static int ComplimentIndex = 0;

        [Command("insult", RunMode = RunMode.Async), Summary("Insult yourself or another user")]
        public async Task Insult([Summary("Optional user to insult")] IUser user = null)
        {
            InsultIndex = await InsultCompliment(Insults, "https://www.reddit.com/r/RoastMe/random/.json", InsultIndex, user);
        }

        [Command("compliment", RunMode = RunMode.Async), Summary("Compliment yourself or another user")]
        public async Task Compliment([Summary("Optional user to compliment")] IUser user = null)
        {
            ComplimentIndex = await InsultCompliment(Compliments, "https://www.reddit.com/r/FreeCompliments/random/.json", ComplimentIndex, user);
        }

        [Command("refreshroasts"), Summary("Refresh the list of insult and compliments")]
        public async Task Refresh()
        {
            InsultIndex = Insults.Count;
            ComplimentIndex = Compliments.Count;
            await Context.Channel.SendMessageAsync("Lists will be refreshed at next request");
        }

        private async Task<int> InsultCompliment(List<string> sourceList, string sourceAddress, int sourceIndex, IUser user)
        {
            var directedUser = user ?? Context.Message.Author;
            if (sourceIndex == sourceList.Count)
            {
                sourceIndex = 0;
                await Context.Channel.SendMessageAsync("Refreshing list, this may take a moment...");
                using (var RequestClient = new System.Net.Http.HttpClient())
                {
                    sourceList.Clear();

                    for (int i = 0; i < THREAD_COUNT; i++)
                    {
                        string Response = await RequestClient.GetStringAsync(sourceAddress);
                        await Task.Run(() =>
                        {
                            var JsonArray = JArray.Parse(Response);
                            foreach (var child in JsonArray[1]["data"]["children"])
                            {
                                if (child["data"]["body"] == null)
                                    continue;

                                string Body = child["data"]["body"].ToString();
                                if (Char.ToLowerInvariant(Body[0]) != 'i')
                                    Body = Char.ToLowerInvariant(Body[0]) + Body.Substring(1);

                                if (!Body.Contains("[removed]") && !Body.Contains("[deleted]") && Body.Length < 128)
                                    sourceList.Add(Body);
                            }
                        });
                    }
                    sourceList.Shuffle();
                }
            }
            await Context.Channel.SendMessageAsync($"Hey {directedUser.Mention}, {sourceList[sourceIndex]}");
            return sourceIndex + 1;
        }
    }
}
