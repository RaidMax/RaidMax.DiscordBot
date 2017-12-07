using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;


using Discord.Net;
using Discord.WebSocket;
using Discord.Commands;
using Discord;
using Discord.Net.Providers.WS4Net;

namespace RaidMax.DiscordBot.Main
{
    class DiscordClient
    {
        DiscordSocketClient _client;
        public static Dictionary<ulong, Helpers.Config> Configurations = new Dictionary<ulong, Helpers.Config>();
        Dictionary<ulong, DiscordUser> ConnectedVoiceClients;
        CommandService Commands;

        class DiscordUser
        {
            public SocketGuildUser Self;
            public DateTime LastEvent;
            public int EvaluatedState;
        }

        public DiscordClient()
        {
            ConnectedVoiceClients = new Dictionary<ulong, DiscordUser>();
            Commands = new CommandService();
            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                WebSocketProvider = WS4NetProvider.Instance
            });
        }

        ~DiscordClient()
        {
            _client.LogoutAsync();
            _client.StopAsync();
        }

        public async Task Initialize(string token)
        {
            _client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated;
            _client.MessageReceived += _client_MessageReceived;
            _client.GuildAvailable += _client_GuildAvailable;
            _client.UserJoined += _client_UserJoined;
            _client.LatencyUpdated += _client_LatencyUpdated;

            await Commands.AddModulesAsync(Assembly.GetEntryAssembly());

            try
            {
                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();
                Console.WriteLine("Initialized...");
            }

            catch (HttpException ex)
            {
                Console.WriteLine("Error connecting to server: " + ex.Message);
            }

            catch (Exception e)
            {
                Console.WriteLine($"Could not initalize {e.Message}");
            }
        }

        private Task _client_LatencyUpdated(int arg1, int arg2)
        {
            return Task.Run(async () =>
            {
                var apiEvent = await IW4MAdminEvents.GetLastestEvent();
                do
                {
#if DEBUG
                    Console.WriteLine(apiEvent.Message);
#endif
                    if (apiEvent.Type == IW4MAdminEvents.RestEvent.EventType.ALERT)
                    {
                        var guildIt = _client.Guilds.GetEnumerator();
                        while (guildIt.MoveNext())
                        {
                            if (guildIt.Current.Id == 121782130740035586)
                            {
                                var roleIt = guildIt.Current.Roles.GetEnumerator();
                                IRole adminRole = null;
                                while (roleIt.MoveNext())
                                {
                                    if (roleIt.Current.Name == "Administrator")
                                        adminRole = roleIt.Current;
                                }
                                await guildIt.Current.DefaultChannel.SendMessageAsync($":warning: {adminRole.Mention}, {apiEvent.Message}");
                            }
                        }
                    }

                } while ((apiEvent = await IW4MAdminEvents.GetLastestEvent()).ID != 0);
            });
        }

        private Task _client_UserJoined(SocketGuildUser arg)
        {
            return arg.Guild.DefaultChannel.SendMessageAsync($"**{arg.Username}** has joined for the first time!");
        }

        private Task _client_GuildAvailable(SocketGuild arg)
        {
            return Task.Run(async () => { Configurations.Add(arg.Id, await Helpers.Config.Read(arg.Id)); });
        }

        private async Task _client_MessageReceived(SocketMessage socketMessage)
        {
            var Message = socketMessage as SocketUserMessage;

            if (Message == null || Message.Author.Id == _client.CurrentUser.Id)
                return;

            int argPos = 0;
            var Context = new CommandContext(_client, Message);

            if (Message.Content[0] != '!')
            {
                if (Context.Guild == null)
                    return;

                ulong guildID = Context.Guild.Id;
                try
                {
                    foreach (string match in DiscordBot.Commands.AutoResponder.Responses[guildID].Keys)
                    {
                        if (Message.Content.ToLower().Contains($"{match}"))
                            await Message.Channel.SendMessageAsync(DiscordBot.Commands.AutoResponder.Responses[guildID][match][new Random().Next(0, DiscordBot.Commands.AutoResponder.Responses[guildID][match].Count)]);
                    }
                }

                catch (IndexOutOfRangeException)
                {
                    await Message.Channel.SendMessageAsync("No autoresponders have been added yet");
                }
                return;
            }

            if (!Message.HasCharPrefix('!', ref argPos) || Message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                return;

            var Result = await Commands.ExecuteAsync(Context, argPos, null);

            if (!Result.IsSuccess)
                await Context.Channel.SendMessageAsync(Result.ErrorReason);
        }

        private Task _client_UserVoiceStateUpdated(SocketUser user, SocketVoiceState state, SocketVoiceState state2)
        {
            if (user.Status == UserStatus.Idle)
                return null;

            DiscordUser GuildUser;
            bool newConnection = false;
            string verbage, channel;

            if (ConnectedVoiceClients.ContainsKey(user.Id))
            {
                GuildUser = ConnectedVoiceClients[user.Id];
                var stateUser = user as SocketGuildUser;
                int newStateEvaluation = (stateUser.IsDeafened ? 1 : 0) | (stateUser.IsMuted ? 2 : 0);// | (stateUser.IsSelfDeafened ? 4 : 0) | (stateUser.IsSelfMuted ? 8 : 0) | (stateUser.IsSuppressed ? 16 : 0);
                // we need to account for state changes where the user isn't disconnecting
                if (newStateEvaluation != GuildUser.EvaluatedState)
                {
                    GuildUser.EvaluatedState = newStateEvaluation;
                    return null;
                }
            }

            else
            {
                GuildUser = new DiscordUser()
                {
                    Self = user as SocketGuildUser,
                    LastEvent = DateTime.MinValue,
                    EvaluatedState = (state.IsDeafened ? 1 : 0) | (state.IsMuted ? 2 : 0) //| (state.IsSelfDeafened ? 4 : 0) | (state.IsSelfMuted ? 8 : 0) | (state.IsSuppressed ? 16 : 0)
                };

                ConnectedVoiceClients.Add(GuildUser.Self.Id, GuildUser);
                newConnection = true;
            }

            if (!Configurations[GuildUser.Self.Guild.Id].AnnounceVoiceChannels)
                return null;

            if (GuildUser.Self.VoiceChannel == null)
            {
                verbage = "disconnected from";
                channel = state.VoiceChannel.ToString();
                ConnectedVoiceClients.Remove(GuildUser.Self.Id);
            }

            else
            {
                verbage = newConnection ? "connected to" : "switched to";
                channel = GuildUser.Self.VoiceChannel.ToString();
            }

            if ((DateTime.Now - GuildUser.LastEvent).TotalSeconds > 2.0)
            {
                GuildUser.LastEvent = DateTime.Now;
                return GuildUser.Self.Guild.DefaultChannel.SendMessageAsync(String.Format("**{0}** {1} **{2}**", user.Username, verbage, channel));
            }

            return null;
        }
    }
}
