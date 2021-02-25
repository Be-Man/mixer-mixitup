﻿using MixItUp.Base.Model;
using MixItUp.Base.Model.Currency;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.Chat;
using MixItUp.Base.ViewModel.Chat.Twitch;
using MixItUp.Base.ViewModel.User;
using Newtonsoft.Json.Linq;
using StreamingClient.Base.Util;
using StreamingClient.Base.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Twitch.Base.Clients;
using Twitch.Base.Models.Clients.Chat;
using Twitch.Base.Models.NewAPI.Bits;
using Twitch.Base.Models.NewAPI.Chat;
using Twitch.Base.Models.NewAPI.Users;
using Twitch.Base.Models.V5.Emotes;
using TwitchNewAPI = Twitch.Base.Models.NewAPI;

namespace MixItUp.Base.Services.Twitch
{
    public class BetterTTVEmoteModel
    {
        public string id { get; set; }
        public string channel { get; set; }
        public string code { get; set; }
        public string imageType { get; set; }

        public string url { get { return string.Format("https://cdn.betterttv.net/emote/{0}/1x", this.id); } }
    }

    public class FrankerFaceZEmoteModel
    {
        public string id { get; set; }
        public string name { get; set; }
        public JObject urls { get; set; }

        public string url { get { return (this.urls != null && this.urls.ContainsKey("2")) ? "https:" + this.urls["2"].ToString() : string.Empty; } }
    }

    public interface ITwitchChatService
    {
        IDictionary<string, EmoteModel> Emotes { get; }
        IDictionary<string, ChatBadgeSetModel> ChatBadges { get; }
        IDictionary<string, BetterTTVEmoteModel> BetterTTVEmotes { get; }
        IDictionary<string, FrankerFaceZEmoteModel> FrankerFaceZEmotes { get; }
        IEnumerable<TwitchBitsCheermoteViewModel> BitsCheermotes { get; }

        event EventHandler<IEnumerable<UserViewModel>> OnUsersJoinOccurred;
        event EventHandler<IEnumerable<UserViewModel>> OnUsersLeaveOccurred;

        event EventHandler<TwitchChatMessageViewModel> OnMessageOccurred;

        bool IsUserConnected { get; }
        bool IsBotConnected { get; }

        Task<Result> ConnectUser();
        Task DisconnectUser();

        Task<Result> ConnectBot();
        Task DisconnectBot();

        Task Initialize();

        Task SendMessage(string message, bool sendAsStreamer = false);

        Task SendWhisperMessage(UserViewModel user, string message, bool sendAsStreamer = false);

        Task DeleteMessage(ChatMessageViewModel message);

        Task ClearMessages();

        Task ModUser(UserViewModel user);

        Task UnmodUser(UserViewModel user);

        Task TimeoutUser(UserViewModel user, int lengthInSeconds);

        Task BanUser(UserViewModel user);

        Task UnbanUser(UserViewModel user);

        Task RunCommercial(int lengthInSeconds);
    }

    public class TwitchChatService : StreamingPlatformServiceBase, ITwitchChatService
    {
        private static List<string> ExcludedDiagnosticPacketLogging = new List<string>() { "PING", ChatMessagePacketModel.CommandID, ChatUserJoinPacketModel.CommandID, ChatUserLeavePacketModel.CommandID };

        private const string HostChatMessageRegexPattern = "^\\w+ is now hosting you.$";

        private const string RaidUserNoticeMessageTypeID = "raid";
        private const string SubMysteryGiftUserNoticeMessageTypeID = "submysterygift";
        private const string SubGiftPaidUpgradeUserNoticeMessageTypeID = "giftpaidupgrade";

        public IDictionary<string, EmoteModel> Emotes { get { return this.emotes; } }
        private Dictionary<string, EmoteModel> emotes = new Dictionary<string, EmoteModel>();

        public IDictionary<string, BetterTTVEmoteModel> BetterTTVEmotes { get { return this.betterTTVEmotes; } }
        private Dictionary<string, BetterTTVEmoteModel> betterTTVEmotes = new Dictionary<string, BetterTTVEmoteModel>();

        public IDictionary<string, FrankerFaceZEmoteModel> FrankerFaceZEmotes { get { return this.frankerFaceZEmotes; } }
        private Dictionary<string, FrankerFaceZEmoteModel> frankerFaceZEmotes = new Dictionary<string, FrankerFaceZEmoteModel>();

        public IDictionary<string, ChatBadgeSetModel> ChatBadges { get { return this.chatBadges; } }
        private Dictionary<string, ChatBadgeSetModel> chatBadges = new Dictionary<string, ChatBadgeSetModel>();

        public IEnumerable<TwitchBitsCheermoteViewModel> BitsCheermotes { get { return this.bitsCheermotes; } }
        private List<TwitchBitsCheermoteViewModel> bitsCheermotes = new List<TwitchBitsCheermoteViewModel>();

        public event EventHandler<IEnumerable<UserViewModel>> OnUsersJoinOccurred = delegate { };
        public event EventHandler<IEnumerable<UserViewModel>> OnUsersLeaveOccurred = delegate { };

        public event EventHandler<TwitchChatMessageViewModel> OnMessageOccurred = delegate { };

        private ChatClient userClient;
        private ChatClient botClient;

        private CancellationTokenSource cancellationTokenSource;

        private const int userJoinLeaveEventsTotalToProcess = 25;
        private SemaphoreSlim userJoinLeaveEventsSemaphore = new SemaphoreSlim(1);
        private HashSet<string> userJoinEvents = new HashSet<string>();
        private HashSet<string> userLeaveEvents = new HashSet<string>();

        private List<string> initialUserLogins = new List<string>();

        private SemaphoreSlim messageSemaphore = new SemaphoreSlim(1);
        private SemaphoreSlim whisperSemaphore = new SemaphoreSlim(1);

        public TwitchChatService() { }

        public bool IsUserConnected { get { return this.userClient != null && this.userClient.IsOpen(); } }
        public bool IsBotConnected { get { return this.botClient != null && this.botClient.IsOpen(); } }

        public override string Name { get { return "Twitch Chat"; } }

        public async Task<Result> ConnectUser()
        {
            if (ChannelSession.TwitchUserConnection != null)
            {
                return await this.AttemptConnect((Func<Task<Result>>)(async () =>
                {
                    try
                    {
                        this.cancellationTokenSource = new CancellationTokenSource();

                        this.userClient = new ChatClient(ChannelSession.TwitchUserConnection.Connection);

                        if (ChannelSession.AppSettings.DiagnosticLogging)
                        {
                            this.userClient.OnSentOccurred += Client_OnSentOccurred;
                        }

                        this.initialUserLogins.Clear();

                        this.userClient.OnPacketReceived += Client_OnPacketReceived;
                        this.userClient.OnDisconnectOccurred += UserClient_OnDisconnectOccurred;
                        this.userClient.OnPingReceived += UserClient_OnPingReceived;
                        this.userClient.OnUserJoinReceived += UserClient_OnUserJoinReceived;
                        this.userClient.OnUserLeaveReceived += UserClient_OnUserLeaveReceived;
                        this.userClient.OnUserStateReceived += UserClient_OnUserStateReceived;
                        this.userClient.OnUserNoticeReceived += UserClient_OnUserNoticeReceived;
                        this.userClient.OnChatClearReceived += UserClient_OnChatClearReceived;
                        this.userClient.OnMessageReceived += UserClient_OnMessageReceived;

                        this.userClient.OnUserListReceived += UserClient_OnUserListReceived;
                        await this.userClient.Connect();

                        await Task.Delay(1000);

                        await this.userClient.AddCommandsCapability();
                        await this.userClient.AddTagsCapability();
                        await this.userClient.AddMembershipCapability();

                        await Task.Delay(1000);

                        await this.userClient.Join((UserModel)ChannelSession.TwitchUserNewAPI);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        AsyncRunner.RunAsyncBackground(this.ChatterJoinLeaveBackground, this.cancellationTokenSource.Token, 2500);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                        await Task.Delay(3000);

                        return new Result();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                        return new Result(ex);
                    }
                }));
            }
            return new Result("Twitch chat connection has not been established");
        }

        public async Task DisconnectUser()
        {
            try
            {
                if (this.userClient != null)
                {
                    if (ChannelSession.AppSettings.DiagnosticLogging)
                    {
                        this.userClient.OnSentOccurred -= Client_OnSentOccurred;
                    }
                    this.userClient.OnPacketReceived -= Client_OnPacketReceived;
                    this.userClient.OnDisconnectOccurred -= UserClient_OnDisconnectOccurred;
                    this.userClient.OnPingReceived -= UserClient_OnPingReceived;
                    this.userClient.OnUserJoinReceived -= UserClient_OnUserJoinReceived;
                    this.userClient.OnUserLeaveReceived -= UserClient_OnUserLeaveReceived;
                    this.userClient.OnUserStateReceived -= UserClient_OnUserStateReceived;
                    this.userClient.OnUserNoticeReceived -= UserClient_OnUserNoticeReceived;
                    this.userClient.OnChatClearReceived -= UserClient_OnChatClearReceived;
                    this.userClient.OnMessageReceived -= UserClient_OnMessageReceived;

                    await this.userClient.Disconnect();
                }

                if (this.cancellationTokenSource != null)
                {
                    this.cancellationTokenSource.Cancel();
                    this.cancellationTokenSource = null;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            this.userClient = null;
        }

        public async Task<Result> ConnectBot()
        {
            if (ChannelSession.TwitchUserConnection != null)
            {
                return await this.AttemptConnect((Func<Task<Result>>)(async () =>
                {
                    try
                    {
                        this.cancellationTokenSource = new CancellationTokenSource();

                        this.botClient = new ChatClient(ChannelSession.TwitchBotConnection.Connection);

                        if (ChannelSession.AppSettings.DiagnosticLogging)
                        {
                            this.botClient.OnSentOccurred += Client_OnSentOccurred;
                        }
                        this.botClient.OnDisconnectOccurred += BotClient_OnDisconnectOccurred;
                        this.botClient.OnPingReceived += BotClient_OnPingReceived;

                        await this.botClient.Connect();

                        await Task.Delay(1000);

                        await this.botClient.AddCommandsCapability();
                        await this.botClient.AddTagsCapability();
                        await this.botClient.AddMembershipCapability();

                        await Task.Delay(1000);

                        await this.botClient.Join((UserModel)ChannelSession.TwitchUserNewAPI);

                        await Task.Delay(3000);

                        return new Result();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                        return new Result(ex);
                    }
                }));
            }
            return new Result("Twitch connection has not been established");
        }

        public async Task DisconnectBot()
        {
            try
            {
                if (this.botClient != null)
                {
                    if (ChannelSession.AppSettings.DiagnosticLogging)
                    {
                        this.botClient.OnSentOccurred -= Client_OnSentOccurred;
                    }
                    this.botClient.OnDisconnectOccurred -= BotClient_OnDisconnectOccurred;
                    this.botClient.OnPingReceived -= BotClient_OnPingReceived;

                    await this.botClient.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            this.botClient = null;
        }

        public async Task Initialize()
        {
            List<Task> initializationTasks = new List<Task>();

            initializationTasks.Add(Task.Run(async() =>
            {
                foreach (EmoteModel emote in await ChannelSession.TwitchUserConnection.GetEmotesForUserV5(ChannelSession.TwitchUserV5))
                {
                    this.emotes[emote.code] = emote;
                }
            }));

            Task<IEnumerable<ChatBadgeSetModel>> globalChatBadgesTask = ChannelSession.TwitchUserConnection.GetGlobalChatBadges();
            initializationTasks.Add(globalChatBadgesTask);
            Task<IEnumerable<ChatBadgeSetModel>> channelChatBadgesTask = ChannelSession.TwitchUserConnection.GetChannelChatBadges(ChannelSession.TwitchUserNewAPI);
            initializationTasks.Add(channelChatBadgesTask);

            if (ChannelSession.Settings.ShowBetterTTVEmotes)
            {
                initializationTasks.Add(this.DownloadBetterTTVEmotes());
                initializationTasks.Add(this.DownloadBetterTTVEmotes(ChannelSession.TwitchUserNewAPI.login));
            }

            if (ChannelSession.Settings.ShowFrankerFaceZEmotes)
            {
                initializationTasks.Add(this.DownloadFrankerFaceZEmotes());
                initializationTasks.Add(this.DownloadFrankerFaceZEmotes(ChannelSession.TwitchUserNewAPI.login));
            }

            Task<IEnumerable<BitsCheermoteModel>> cheermotesTask = ChannelSession.TwitchUserConnection.GetBitsCheermotes(ChannelSession.TwitchUserNewAPI);
            initializationTasks.Add(cheermotesTask);

            await Task.WhenAll(initializationTasks);

            foreach (ChatBadgeSetModel badgeSet in globalChatBadgesTask.Result)
            {
                this.chatBadges[badgeSet.id] = badgeSet;
            }

            foreach (ChatBadgeSetModel badgeSet in channelChatBadgesTask.Result)
            {
                this.chatBadges[badgeSet.id] = badgeSet;
            }

            List<TwitchBitsCheermoteViewModel> cheermotes = new List<TwitchBitsCheermoteViewModel>();
            foreach (BitsCheermoteModel bitsCheermote in cheermotesTask.Result)
            {
                if (bitsCheermote.tiers.Any(t => t.can_cheer))
                {
                    this.bitsCheermotes.Add(new TwitchBitsCheermoteViewModel(bitsCheermote));
                }
            }

            await this.userJoinLeaveEventsSemaphore.WaitAndRelease(() =>
            {
                foreach (string user in this.initialUserLogins)
                {
                    this.userJoinEvents.Add(user);
                }
                return Task.FromResult(0);
            });
            this.initialUserLogins.Clear();
        }

        public async Task SendMessage(string message, bool sendAsStreamer = false)
        {
            await this.messageSemaphore.WaitAndRelease(async () =>
            {
                ChatClient client = this.GetChatClient(sendAsStreamer);
                if (client != null)
                {
                    string subMessage = null;
                    do
                    {
                        message = ChatService.SplitLargeMessage(message, out subMessage);
                        await client.SendMessage((UserModel)ChannelSession.TwitchUserNewAPI, message);
                        message = subMessage;
                        await Task.Delay(500);
                    }
                    while (!string.IsNullOrEmpty(message));
                }
            });
        }

        public async Task SendWhisperMessage(UserViewModel user, string message, bool sendAsStreamer = false)
        {
            await this.messageSemaphore.WaitAndRelease(async () =>
            {
                ChatClient client = this.GetChatClient(sendAsStreamer);
                if (client != null)
                {
                    string subMessage = null;
                    do
                    {
                        message = ChatService.SplitLargeMessage(message, out subMessage);
                        await client.SendWhisperMessage((UserModel)ChannelSession.TwitchUserNewAPI, user.GetTwitchNewAPIUserModel(), message);
                        message = subMessage;
                        await Task.Delay(500);
                    }
                    while (!string.IsNullOrEmpty(message));
                }
            });
        }

        public async Task DeleteMessage(ChatMessageViewModel message)
        {
            await AsyncRunner.RunAsync(async () =>
            {
                if (this.userClient != null)
                {
                    await this.userClient.DeleteMessage((UserModel)ChannelSession.TwitchUserNewAPI, message.ID);
                }
            });
        }

        public async Task ClearMessages()
        {
            await AsyncRunner.RunAsync(async () =>
            {
                if (this.userClient != null)
                {
                    await this.userClient.ClearChat((UserModel)ChannelSession.TwitchUserNewAPI);
                }
            });
        }

        public async Task ModUser(UserViewModel user)
        {
            await AsyncRunner.RunAsync(async () =>
            {
                if (this.userClient != null)
                {
                    await this.userClient.ModUser((UserModel)ChannelSession.TwitchUserNewAPI, user.GetTwitchNewAPIUserModel());
                }
            });
        }

        public async Task UnmodUser(UserViewModel user)
        {
            await AsyncRunner.RunAsync(async () =>
            {
                if (this.userClient != null)
                {
                    await this.userClient.UnmodUser((UserModel)ChannelSession.TwitchUserNewAPI, user.GetTwitchNewAPIUserModel());
                }
            });
        }

        public async Task TimeoutUser(UserViewModel user, int lengthInSeconds)
        {
            await AsyncRunner.RunAsync(async () =>
            {
                if (this.userClient != null)
                {
                    await this.userClient.TimeoutUser((UserModel)ChannelSession.TwitchUserNewAPI, user.GetTwitchNewAPIUserModel(), lengthInSeconds);
                }
            });
        }

        public async Task BanUser(UserViewModel user)
        {
            await AsyncRunner.RunAsync(async () =>
            {
                if (this.userClient != null)
                {
                    await this.userClient.BanUser((UserModel)ChannelSession.TwitchUserNewAPI, user.GetTwitchNewAPIUserModel());
                }
            });
        }

        public async Task UnbanUser(UserViewModel user)
        {
            await AsyncRunner.RunAsync(async () =>
            {
                if (this.userClient != null)
                {
                    await this.userClient.UnbanUser((UserModel)ChannelSession.TwitchUserNewAPI, user.GetTwitchNewAPIUserModel());
                }
            });
        }

        public async Task RunCommercial(int lengthInSeconds)
        {
            await AsyncRunner.RunAsync(async () =>
            {
                if (this.userClient != null)
                {
                    await this.userClient.RunCommercial((UserModel)ChannelSession.TwitchUserNewAPI, lengthInSeconds);
                }
            });
        }

        private ChatClient GetChatClient(bool sendAsStreamer = false) { return (this.botClient != null && !sendAsStreamer) ? this.botClient : this.userClient; }

        private async Task ChatterJoinLeaveBackground(CancellationToken cancellationToken)
        {
            List<string> joinsToProcess = new List<string>();
            await this.userJoinLeaveEventsSemaphore.WaitAndRelease(() =>
            {
                for (int i = 0; i < userJoinLeaveEventsTotalToProcess && i < this.userJoinEvents.Count(); i++)
                {
                    string chatUser = this.userJoinEvents.First();
                    joinsToProcess.Add(chatUser);
                    this.userJoinEvents.Remove(chatUser);
                }
                return Task.FromResult(0);
            });

            if (joinsToProcess.Count > 0)
            {
                List<UserViewModel> processedUsers = new List<UserViewModel>();
                foreach (string chatUser in joinsToProcess)
                {
                    TwitchNewAPI.Users.UserModel twitchUser = await ChannelSession.TwitchUserConnection.GetNewAPIUserByLogin(chatUser);
                    if (twitchUser != null)
                    {
                        UserViewModel user = await ChannelSession.Services.User.AddOrUpdateUser(twitchUser);
                        if (user != null)
                        {
                            processedUsers.Add(user);
                        }
                    }
                }
                this.OnUsersJoinOccurred(this, processedUsers);
            }

            List<string> leavesToProcess = new List<string>();
            await this.userJoinLeaveEventsSemaphore.WaitAndRelease(() =>
            {
                for (int i = 0; i < userJoinLeaveEventsTotalToProcess && i < this.userLeaveEvents.Count(); i++)
                {
                    string chatUser = this.userLeaveEvents.First();
                    leavesToProcess.Add(chatUser);
                    this.userLeaveEvents.Remove(chatUser);
                }
                return Task.FromResult(0);
            });

            if (leavesToProcess.Count > 0)
            {
                List<UserViewModel> processedUsers = new List<UserViewModel>();
                foreach (string chatUser in leavesToProcess)
                {
                    if (!string.IsNullOrEmpty(chatUser))
                    {
                        UserViewModel user = await ChannelSession.Services.User.RemoveUserByUsername(StreamingPlatformTypeEnum.Twitch, chatUser);
                        if (user != null)
                        {
                            processedUsers.Add(user);
                        }
                    }
                }
                this.OnUsersLeaveOccurred(this, processedUsers);
            }
        }

        private async Task DownloadBetterTTVEmotes(string channelName = null)
        {
            try
            {
                using (AdvancedHttpClient client = new AdvancedHttpClient())
                {
                    JObject jobj = await client.GetJObjectAsync((!string.IsNullOrEmpty(channelName)) ? "https://api.betterttv.net/2/channels/" + channelName : "https://api.betterttv.net/2/emotes");
                    if (jobj != null && jobj.ContainsKey("emotes"))
                    {
                        JArray array = (JArray)jobj["emotes"];
                        foreach (BetterTTVEmoteModel emote in array.ToTypedArray<BetterTTVEmoteModel>())
                        {
                            this.betterTTVEmotes[emote.code] = emote;
                        }
                    }
                }
            }
            catch (Exception ex) { Logger.Log(ex); }
        }

        private async Task DownloadFrankerFaceZEmotes(string channelName = null)
        {
            try
            {
                using (AdvancedHttpClient client = new AdvancedHttpClient())
                {
                    JObject jobj = await client.GetJObjectAsync((!string.IsNullOrEmpty(channelName)) ? "https://api.frankerfacez.com/v1/room/" + channelName : "https://api.frankerfacez.com/v1/set/global");
                    if (jobj != null && jobj.ContainsKey("sets"))
                    {
                        JObject setsJObj = (JObject)jobj["sets"];
                        foreach (var kvp in setsJObj)
                        {
                            JObject setJObj = (JObject)kvp.Value;
                            if (setJObj != null && setJObj.ContainsKey("emoticons"))
                            {
                                JArray emoticonsJArray = (JArray)setJObj["emoticons"];
                                foreach (FrankerFaceZEmoteModel emote in emoticonsJArray.ToTypedArray<FrankerFaceZEmoteModel>())
                                {
                                    this.frankerFaceZEmotes[emote.name] = emote;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Logger.Log(ex); }
        }

        private async void UserClient_OnPingReceived(object sender, EventArgs e)
        {
            Logger.Log(LogLevel.Debug, "Twitch User Client - Ping");
            await this.userClient.Pong();
        }

        private async void BotClient_OnPingReceived(object sender, EventArgs e)
        {
            Logger.Log(LogLevel.Debug, "Twitch Bot Client - Ping");
            await this.botClient.Pong();
        }

        private async void UserClient_OnUserJoinReceived(object sender, ChatUserJoinPacketModel userJoin)
        {
            await this.userJoinLeaveEventsSemaphore.WaitAndRelease(() =>
            {
                if (!string.IsNullOrEmpty(userJoin.UserLogin))
                {
                    this.userJoinEvents.Add(userJoin.UserLogin);
                }
                return Task.FromResult(0);
            });
        }

        private async void UserClient_OnUserLeaveReceived(object sender, ChatUserLeavePacketModel userLeave)
        {
            await this.userJoinLeaveEventsSemaphore.WaitAndRelease(() =>
            {
                if (!string.IsNullOrEmpty(userLeave.UserLogin))
                {
                    this.userLeaveEvents.Add(userLeave.UserLogin);
                }
                return Task.FromResult(0);
            });
        }

        private void UserClient_OnUserStateReceived(object sender, ChatUserStatePacketModel userState)
        {
            UserViewModel user = ChannelSession.Services.User.GetUserByUsername(userState.UserDisplayName, StreamingPlatformTypeEnum.Twitch);
            if (user != null)
            {
                user.SetTwitchChatDetails(userState);
            }
        }

        private async void UserClient_OnUserNoticeReceived(object sender, ChatUserNoticePacketModel userNotice)
        {
            try
            {
                if (RaidUserNoticeMessageTypeID.Equals(userNotice.MessageTypeID))
                {
                    UserViewModel user = ChannelSession.Services.User.GetUserByPlatformID(StreamingPlatformTypeEnum.Twitch, userNotice.UserID.ToString());
                    if (user == null)
                    {
                        user = new UserViewModel(userNotice);
                    }
                    user.SetTwitchChatDetails(userNotice);

                    EventTrigger trigger = new EventTrigger(EventTypeEnum.TwitchChannelRaided, user);
                    if (ChannelSession.Services.Events.CanPerformEvent(trigger))
                    {
                        ChannelSession.Settings.LatestSpecialIdentifiersData[SpecialIdentifierStringBuilder.LatestRaidUserData] = user.ID;
                        ChannelSession.Settings.LatestSpecialIdentifiersData[SpecialIdentifierStringBuilder.LatestRaidViewerCountData] = userNotice.RaidViewerCount;

                        foreach (CurrencyModel currency in ChannelSession.Settings.Currency.Values.ToList())
                        {
                            currency.AddAmount(user.Data, currency.OnHostBonus);
                        }

                        foreach (StreamPassModel streamPass in ChannelSession.Settings.StreamPass.Values)
                        {
                            if (user.HasPermissionsTo(streamPass.Permission))
                            {
                                streamPass.AddAmount(user.Data, streamPass.HostBonus);
                            }
                        }

                        GlobalEvents.RaidOccurred(user, userNotice.RaidViewerCount);

                        trigger.SpecialIdentifiers["hostviewercount"] = userNotice.RaidViewerCount.ToString();
                        trigger.SpecialIdentifiers["raidviewercount"] = userNotice.RaidViewerCount.ToString();
                        await ChannelSession.Services.Events.PerformEvent(trigger);

                        await ChannelSession.Services.Alerts.AddAlert(new AlertChatMessageViewModel(StreamingPlatformTypeEnum.Twitch, user, string.Format("{0} raided with {1} viewers", user.DisplayName, userNotice.RaidViewerCount), ChannelSession.Settings.AlertRaidColor));
                    }
                }
                else if (SubMysteryGiftUserNoticeMessageTypeID.Equals(userNotice.MessageTypeID) && userNotice.SubTotalGifted > 0)
                {
                    if (ChannelSession.Services.Events.TwitchEventService != null)
                    {
                        await ChannelSession.Services.Events.TwitchEventService.AddMassGiftedSub(new TwitchMassGiftedSubEventModel(userNotice));
                    }
                }
                else if (SubGiftPaidUpgradeUserNoticeMessageTypeID.Equals(userNotice.MessageTypeID))
                {
                    if (ChannelSession.Services.Events.TwitchEventService != null)
                    {
                        await ChannelSession.Services.Events.TwitchEventService.AddSub(new TwitchSubEventModel(userNotice));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.ForceLog(LogLevel.Debug, JSONSerializerHelper.SerializeToString(userNotice));
                Logger.Log(ex);
                throw ex;
            }
        }

        private async void UserClient_OnChatClearReceived(object sender, ChatClearChatPacketModel chatClear)
        {
            UserViewModel user = ChannelSession.Services.User.GetUserByPlatformID(StreamingPlatformTypeEnum.Twitch, chatClear.UserID);
            if (user == null)
            {
                user = new UserViewModel(chatClear);
            }

            if (chatClear.IsClear)
            {
                await ChannelSession.Services.Alerts.AddAlert(new AlertChatMessageViewModel(StreamingPlatformTypeEnum.Twitch, user, "Chat Cleared", ChannelSession.Settings.AlertModerationColor));
            }
            else if (chatClear.IsTimeout)
            {
                EventTrigger trigger = new EventTrigger(EventTypeEnum.ChatUserTimeout);
                trigger.Arguments.Add("@" + user.Username);
                trigger.TargetUser = user;
                trigger.SpecialIdentifiers["timeoutlength"] = chatClear.BanDuration.ToString();
                await ChannelSession.Services.Events.PerformEvent(trigger);

                await ChannelSession.Services.Alerts.AddAlert(new AlertChatMessageViewModel(StreamingPlatformTypeEnum.Twitch, user, string.Format("{0} Timed Out for {1} seconds", user.DisplayName, chatClear.BanDuration), ChannelSession.Settings.AlertModerationColor));
            }
            else if (chatClear.IsBan)
            {
                EventTrigger trigger = new EventTrigger(EventTypeEnum.ChatUserBan);
                trigger.Arguments.Add("@" + user.Username);
                trigger.TargetUser = user;
                await ChannelSession.Services.Events.PerformEvent(trigger);

                await ChannelSession.Services.Alerts.AddAlert(new AlertChatMessageViewModel(StreamingPlatformTypeEnum.Twitch, user, string.Format("{0} Banned", user.DisplayName), ChannelSession.Settings.AlertModerationColor));

                await ChannelSession.Services.User.RemoveUserByUsername(StreamingPlatformTypeEnum.Twitch, user.Data.TwitchUsername);
            }
        }

        private async void UserClient_OnMessageReceived(object sender, ChatMessagePacketModel message)
        {
            if (message != null && !string.IsNullOrEmpty(message.Message))
            {
                if (!string.IsNullOrEmpty(message.UserLogin) && message.UserLogin.Equals("jtv"))
                {
                    if (Regex.IsMatch(message.Message, TwitchChatService.HostChatMessageRegexPattern))
                    {
                        Logger.Log(LogLevel.Debug, JSONSerializerHelper.SerializeToString(message));

                        string hoster = message.Message.Substring(0, message.Message.IndexOf(' '));
                        UserViewModel user = ChannelSession.Services.User.GetUserByUsername(hoster, StreamingPlatformTypeEnum.Twitch);
                        if (user == null)
                        {
                            UserModel twitchUser = await ChannelSession.TwitchUserConnection.GetNewAPIUserByLogin(hoster);
                            if (twitchUser != null)
                            {
                                user = await ChannelSession.Services.User.AddOrUpdateUser(twitchUser);
                            }
                        }

                        if (user != null)
                        {
                            EventTrigger trigger = new EventTrigger(EventTypeEnum.TwitchChannelHosted, user);
                            if (ChannelSession.Services.Events.CanPerformEvent(trigger))
                            {
                                foreach (CurrencyModel currency in ChannelSession.Settings.Currency.Values.ToList())
                                {
                                    currency.AddAmount(user.Data, currency.OnHostBonus);
                                }

                                GlobalEvents.HostOccurred(user);

                                await ChannelSession.Services.Events.PerformEvent(trigger);

                                await ChannelSession.Services.Alerts.AddAlert(new AlertChatMessageViewModel(StreamingPlatformTypeEnum.Twitch, user, string.Format("{0} hosted the channel", user.DisplayName), ChannelSession.Settings.AlertHostColor));
                            }
                        }
                    }
                }
                else
                {
                    UserViewModel user = ChannelSession.Services.User.GetUserByPlatformID(StreamingPlatformTypeEnum.Twitch, message.UserID);
                    if (user == null)
                    {
                        UserModel twitchUser = await ChannelSession.TwitchUserConnection.GetNewAPIUserByLogin(message.UserLogin);
                        if (twitchUser != null)
                        {
                            user = await ChannelSession.Services.User.AddOrUpdateUser(twitchUser);
                        }
                    }
                    this.OnMessageOccurred(this, new TwitchChatMessageViewModel(message, user));
                }
            }
        }

        private void UserClient_OnUserListReceived(object sender, ChatUsersListPacketModel userList)
        {
            this.initialUserLogins.AddRange(userList.UserLogins);
            this.userClient.OnUserListReceived -= UserClient_OnUserListReceived;
        }

        private void Client_OnPacketReceived(object sender, ChatRawPacketModel packet)
        {
            if (!TwitchChatService.ExcludedDiagnosticPacketLogging.Contains(packet.Command))
            {
                if (ChannelSession.AppSettings.DiagnosticLogging)
                {
                    Logger.Log(LogLevel.Debug, string.Format("Twitch Client Packet Received: {0}", JSONSerializerHelper.SerializeToString(packet)));
                }
            }
        }

        private void Client_OnSentOccurred(object sender, string packet)
        {
            Logger.Log(LogLevel.Debug, string.Format("Twitch Chat Packet Sent: {0}", packet));
        }

        private async void UserClient_OnDisconnectOccurred(object sender, WebSocketCloseStatus closeStatus)
        {
            ChannelSession.DisconnectionOccurred("Twitch User Chat");

            Result result;
            await this.DisconnectUser();
            do
            {
                await Task.Delay(2500);

                result = await this.ConnectUser();
            }
            while (!result.Success);

            ChannelSession.ReconnectionOccurred("Twitch User Chat");
        }

        private async void BotClient_OnDisconnectOccurred(object sender, WebSocketCloseStatus closeStatus)
        {
            ChannelSession.DisconnectionOccurred("Twitch Bot Chat");

            Result result;
            await this.DisconnectBot();
            do
            {
                await Task.Delay(2500);

                result = await this.ConnectBot();
            }
            while (!result.Success);

            ChannelSession.ReconnectionOccurred("Twitch Bot Chat");
        }
    }
}