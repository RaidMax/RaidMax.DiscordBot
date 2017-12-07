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
        private static string[] SubredditList;

        [Command("meme", RunMode = RunMode.Async), Summary("Posts a random meme")]
        public async Task Meme([Summary("Subreddit to pull the meme from")] string subreddit = null)
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

            else if (subreddit == null)
            {
                int subredditIndex = new Random().Next(0, SubredditList.Length - 1);
                subreddit = SubredditList[subredditIndex];
            }

            using (var RequestClient = new System.Net.Http.HttpClient())
            {

                for (int count = 0; count < 5; count++)
                {
                    string Response = await RequestClient.GetStringAsync($"https://reddit.com/r/{subreddit}/random/.json");

                    try
                    {
                        bool array = false;
                        JContainer JsonArray = null;
                        try
                        {
                            JsonArray = JArray.Parse(Response);
                            array = true;
                        }

                        catch (Newtonsoft.Json.JsonReaderException e)
                        {
                            JsonArray = JObject.Parse(Response);
                            array = false;
                        }

                        JToken postData = null;
                        bool isNSFW;

                        if (array)
                        {
                            postData = JsonArray[0]["data"]["children"][0]["data"];
                            isNSFW = JsonArray[0]["data"]["whitelist_status"].ToString().Contains("nsfw") || Boolean.Parse(postData["over_18"].ToString());
                        }

                        else
                        {
                            postData = JsonArray["data"]["children"][0]["data"];
                            isNSFW = JsonArray["data"]["whitelist_status"].ToString().Contains("nsfw") || Boolean.Parse(postData["over_18"].ToString());
                            throw new Newtonsoft.Json.JsonReaderException();
                        }

                        if (isNSFW && Context.Channel.Id != Main.DiscordClient.Configurations[Context.Guild.Id].NSFWChannelId)
                        {
                            await Context.Channel.SendMessageAsync(":closed_lock_with_key: **nsfw** memes are only allowed in the **nsfw** channel");
                            return;
                        }

                        bool isVideo = postData["secure_media_embed"].Count() > 0;
                        bool hasPreview = postData.Where(c => (c as JProperty).Name == "preview").Count() > 0;

                        if (isVideo)
                        {
                            await Context.Channel.SendMessageAsync(postData["url"].ToString());
                            break;
                        }

                        else if (hasPreview)
                        {
                            string imagePath = postData["preview"]["images"][0]["source"]["url"].ToString();
                            var match = Regex.Match(imagePath, @"\.([a-z]|[A-Z])+\?");
                            string fileExtension = match.Value.Substring(1, match.Value.Length - 2);

                            var imgStream = await RequestClient.GetStreamAsync(imagePath);
                            await Context.Channel.SendFileAsync(imgStream, $"dailydose.{fileExtension}");
                            break;
                        }

                        else if (count < 5)
                            continue;
                        await Context.Channel.SendMessageAsync(":warning: The meme was not a picture or video");
                    }

                    catch (Newtonsoft.Json.JsonReaderException e)
                    {
                        await Context.Channel.SendMessageAsync($":no_entry: **{subreddit}** does not have random posts enabled, sorry!");
                        break;
                    }

                    catch (NullReferenceException e)
                    {
                        Console.WriteLine(e);
                        await Context.Channel.SendMessageAsync($":no_entry: Failed to meme! Is **{subreddit}** a real subreddit?");
                        break;
                    }

                    catch (Exception e)
                    {
                        if (count >= 4)
                        {
                            await Context.Channel.SendMessageAsync(":warning: The meme was too powerful and broke the server!");
                            await Context.Channel.SendMessageAsync($"For RaidMax ```{e.Message}```");
                            break;
                        }
                    }
                }
            }
        }

        [Group("memes")]
        public class MemeSubModule : ModuleBase
        {
            [Command("add"), Summary("Add a new subreddit to pull memes from")]
            public async Task AddSubreddit([Summary("Subreddit to add")] string subreddit)
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
                    await Context.Channel.SendMessageAsync($"**{subreddit}** does not appear to be a valid subreddit, or it is empty");
            }

            [Command("list"), Summary("List all subreddits memes are pulled from")]
            public async Task ListSubreddit()
            {
                if (SubredditList == null || SubredditList.Length == 0)
                    await Context.Channel.SendMessageAsync("There are no subreddits added for this guild");

                else
                    await Context.Channel.SendMessageAsync($"Subscribed To```{String.Join(",", SubredditList)}```");
            }

            [Command("delete"), Summary("Delete a subreddit that memes are pulled from")]
            public async Task RemoveSubreddit([Summary("Subreddit to delete")] string subreddit)
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
                        return JsonArray[0]["data"]["children"].Count() > 0 || JsonArray["data"]["children"].Count() > 0;
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
