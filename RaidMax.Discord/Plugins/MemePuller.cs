using Discord.Commands;
using Newtonsoft.Json.Linq;
using Discord;
using RaidMax.DiscordBot.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Text.RegularExpressions;

namespace RaidMax.DiscordBot.Plugins
{
    public class MemePuller : ModuleBase
    {
        private static string[] SubredditList; //= { "dankmemes", "surrealmemes", "me_irl", "trippinthroughtime", "anime_irl", "blackpeopletwitter", "hmmm" };

        [Command("meme", RunMode = RunMode.Async), Summary("Posts a random meme")]
        public async Task Meme(string subreddit = null)
        {
            if (SubredditList == null)
            {
                var config = await Config.Read(Context.Guild.Id);
                if (config.SubredditList.Count == 0)
                {
                    await Context.Channel.SendMessageAsync("There are no subreddits add for this guild");
                    return;
                }

                SubredditList = config.SubredditList.ToArray();
            }

            if (subreddit == null)
            {
                int subredditIndex = new Random().Next(0, SubredditList.Length - 1);
                subreddit = SubredditList[subredditIndex];
            }

            using (var RequestClient = new System.Net.Http.HttpClient())
            {
                string Response = await RequestClient.GetStringAsync($"https://reddit.com/r/{subreddit}/random/.json");

                try
                {
                    var JsonArray = JArray.Parse(Response);
                    bool? isNSFW = JsonArray[0]["data"]["whitelist_status"]?.ToString().Contains("nsfw");

                    if (isNSFW.Value && Context.Channel.Id != 312059644195635210)
                    {
                        await Context.Channel.SendMessageAsync(":closed_lock_with_key: **nsfw** memes are only allowed in the **nsfw** channel");
                        return;
                    }

                    var postData = JsonArray[0]["data"]["children"][0]["data"];
                    bool isVideo = postData["secure_media_embed"].Count() > 0;
                    bool hasPreview = postData.Where(c => (c as JProperty).Name == "preview").Count() > 0;

                    if (isVideo)
                    {
                        await Context.Channel.SendMessageAsync(postData["url"].ToString());
                    }

                    else if (hasPreview)
                    {
                        string imagePath = postData["preview"]["images"][0]["source"]["url"].ToString();
                        var match = Regex.Match(imagePath, @"\.([a-z]|[A-Z])+\?");
                        string fileExtension = match.Value.Substring(1, match.Value.Length - 2);

                        var imgStream = await RequestClient.GetStreamAsync(imagePath);
                        await Context.Channel.SendFileAsync(imgStream, $"dailydose.{fileExtension}");
                    }

                    else
                        await Context.Channel.SendMessageAsync(":warning: The meme was not a picture or video");
                }

                catch (Newtonsoft.Json.JsonReaderException)
                {
                    await Context.Channel.SendMessageAsync($":no_entry: No memes were found. Is **{subreddit}** a real subreddit?");
                }

                catch (NullReferenceException)
                {
                    await Context.Channel.SendMessageAsync($":no_entry: Failed to meme! Is **{subreddit}** a real subreddit?");
                }

                catch (Exception e)
                {
                    await Context.Channel.SendMessageAsync(":warning: The meme was too powerful and broke the server!");
                    await Context.Channel.SendMessageAsync($"For RaidMax ```{e.Message}```");
                }
            }
        }

        [Group("memes")]
        public class MemeSubModule : ModuleBase
        {
            [Command("add")]
            public async Task AddSubreddit(string subreddit)
            {
                subreddit = subreddit.ToLower();
                if (await ValidSubreddit(subreddit))
                {
                    var config = await Config.Read(Context.Guild.Id);
                    config.SubredditList.Add(subreddit);
                    await Config.Write(Context.Guild.Id, config);
                    SubredditList = config.SubredditList.ToArray();

                    await Context.Channel.SendMessageAsync("Subreddit added");
                }

                else
                    await Context.Channel.SendMessageAsync($"\"{subreddit}\" does not appear to be a valid subreddit");
            }

            [Command("list")]
            public async Task ListSubreddit()
            {
                if (SubredditList == null || SubredditList.Length == 0)
                    await Context.Channel.SendMessageAsync("There are no subreddits added for this guild");

                else
                    await Context.Channel.SendMessageAsync($"Subscribed To```{String.Join(",", SubredditList)}```");
            }

            [Command("delete")]
            public async Task RemoveSubreddit(string subreddit)
            {
                var config = await Config.Read(Context.Guild.Id);
                int subredditIndex = config.SubredditList.IndexOf(subreddit.ToLower());

                if (subredditIndex > 0)
                {
                    config.SubredditList.RemoveAt(subredditIndex);
                    await Config.Write(Context.Guild.Id, config);
                    SubredditList = config.SubredditList.ToArray();
                    await Context.Channel.SendMessageAsync("Subreddit removed");
                }

                else
                    await Context.Channel.SendMessageAsync($"\"{subreddit}\" is not added to the subreddit list");
            }

            private async Task<bool> ValidSubreddit(string subreddit)
            {
                using (var RequestClient = new System.Net.Http.HttpClient())
                {
                    string Response = await RequestClient.GetStringAsync($"https://reddit.com/r/{subreddit}/random/.json");
                    try
                    {
                        var JsonArray = JArray.Parse(Response);
                        var imagePath = JsonArray[0]["data"]["children"][0]["data"]["preview"]["images"][0]["source"]["url"].ToString();
                        return true;
                    }

                    catch (Exception)
                    {
                        return false;
                    }
                }
            }
        }


    }
}
