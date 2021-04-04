﻿using MixItUp.Base.Model;
using MixItUp.Base.Model.Commands;
using MixItUp.Base.Model.Commands.Games;
using MixItUp.Base.Model.Currency;
using MixItUp.Base.Model.Settings;
using MixItUp.Base.Model.User;
using MixItUp.Base.Services;
using MixItUp.Base.Services.External;
using MixItUp.Base.Services.Twitch;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.User;
using StreamingClient.Base.Model.OAuth;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchNewAPI = Twitch.Base.Models.NewAPI;
using TwitchV5API = Twitch.Base.Models.V5;

namespace MixItUp.Base
{
    public static class ChannelSession
    {
        public static TwitchPlatformService TwitchUserConnection { get; private set; }
        public static TwitchPlatformService TwitchBotConnection { get; private set; }
        public static TwitchV5API.Users.UserModel TwitchUserV5 { get; private set; }
        public static TwitchV5API.Channel.ChannelModel TwitchChannelV5 { get; private set; }
        public static TwitchV5API.Streams.StreamModel TwitchStreamV5 { get; private set; }
        public static HashSet<string> TwitchChannelEditorsV5 { get; private set; } = new HashSet<string>();
        public static TwitchNewAPI.Users.UserModel TwitchUserNewAPI { get; set; }
        public static TwitchNewAPI.Users.UserModel TwitchBotNewAPI { get; set; }
        public static TwitchNewAPI.Streams.StreamModel TwitchStreamNewAPI { get; set; }
        public static bool TwitchStreamIsLive { get { return ChannelSession.TwitchStreamV5 != null && ChannelSession.TwitchStreamV5.IsLive; } }

        public static ApplicationSettingsV2Model AppSettings { get; private set; }
        public static SettingsV3Model Settings { get; private set; }

        public static ServicesManagerBase Services { get; private set; }

        private static CancellationTokenSource sessionBackgroundCancellationTokenSource = new CancellationTokenSource();
        private static int sessionBackgroundTimer = 0;

        public static bool IsDebug()
        {
            #if DEBUG
                return true;
            #else
                return false;
            #endif
        }

        public static bool IsElevated { get; set; }

        public static List<PreMadeChatCommandModelBase> PreMadeChatCommands { get; private set; } = new List<PreMadeChatCommandModelBase>();

        public static List<ChatCommandModel> ChatCommands { get; set; } = new List<ChatCommandModel>();

        public static List<EventCommandModel> EventCommands { get; set; } = new List<EventCommandModel>();

        public static List<TimerCommandModel> TimerCommands { get; set; } = new List<TimerCommandModel>();

        public static List<ActionGroupCommandModel> ActionGroupCommands { get; set; } = new List<ActionGroupCommandModel>();

        public static List<GameCommandModelBase> GameCommands { get; set; } = new List<GameCommandModelBase>();

        public static List<TwitchChannelPointsCommandModel> TwitchChannelPointsCommands { get; set; } = new List<TwitchChannelPointsCommandModel>();

        public static IEnumerable<CommandModelBase> AllEnabledChatAccessibleCommands
        {
            get
            {
                List<CommandModelBase> commands = new List<CommandModelBase>();
                commands.AddRange(ChannelSession.PreMadeChatCommands.Where(c => c.IsEnabled));
                commands.AddRange(ChannelSession.ChatCommands.Where(c => c.IsEnabled));
                commands.AddRange(ChannelSession.GameCommands.Where(c => c.IsEnabled));
                return commands;
            }
        }

        public static IEnumerable<CommandModelBase> AllCommands
        {
            get
            {
                List<CommandModelBase> commands = new List<CommandModelBase>();
                commands.AddRange(ChannelSession.PreMadeChatCommands);
                commands.AddRange(ChannelSession.ChatCommands);
                commands.AddRange(ChannelSession.GameCommands);
                commands.AddRange(ChannelSession.EventCommands);
                commands.AddRange(ChannelSession.TimerCommands);
                commands.AddRange(ChannelSession.ActionGroupCommands);
                commands.AddRange(ChannelSession.TwitchChannelPointsCommands);
                return commands;
            }
        }

        public static async Task Initialize(ServicesManagerBase serviceHandler)
        {
            ChannelSession.Services = serviceHandler;

            try
            {
                Type mixItUpSecretsType = Type.GetType("MixItUp.Base.MixItUpSecrets");
                if (mixItUpSecretsType != null)
                {
                    ChannelSession.Services.SetSecrets((SecretsService)Activator.CreateInstance(mixItUpSecretsType));
                }
            }
            catch (Exception ex) { Logger.Log(ex); }

            ChannelSession.AppSettings = await ApplicationSettingsV2Model.Load();
        }

        public static async Task<Result> ConnectTwitchUser()
        {
            Result<TwitchPlatformService> result = await TwitchPlatformService.ConnectUser();
            if (result.Success)
            {
                ChannelSession.TwitchUserConnection = result.Value;
                ChannelSession.TwitchUserNewAPI = await ChannelSession.TwitchUserConnection.GetNewAPICurrentUser();
                if (ChannelSession.TwitchUserNewAPI == null)
                {
                    return new Result(Resources.TwitchFailedNewAPIUserData);
                }

                ChannelSession.TwitchUserV5 = await ChannelSession.TwitchUserConnection.GetV5APIUserByLogin(ChannelSession.TwitchUserNewAPI.login);
                if (ChannelSession.TwitchUserV5 == null)
                {
                    return new Result(Resources.TwitchFailedV5APIUserData);
                }
            }
            return result;
        }

        public static async Task<Result> ConnectTwitchBot()
        {
            Result<TwitchPlatformService> result = await TwitchPlatformService.ConnectBot();
            if (result.Success)
            {
                ChannelSession.TwitchBotConnection = result.Value;
                ChannelSession.TwitchBotNewAPI = await ChannelSession.TwitchBotConnection.GetNewAPICurrentUser();
                if (ChannelSession.TwitchBotNewAPI == null)
                {
                    return new Result(Resources.TwitchFailedBotData);
                }

                if (ChannelSession.Services.Chat.TwitchChatService != null && ChannelSession.Services.Chat.TwitchChatService.IsUserConnected)
                {
                    return await ChannelSession.Services.Chat.TwitchChatService.ConnectBot();
                }
            }
            return result;
        }

        public static async Task<Result> ConnectUser(SettingsV3Model settings)
        {
            Result userResult = null;
            ChannelSession.Settings = settings;

            // Twitch connection
            if (!ChannelSession.Settings.StreamingPlatformAuthentications.ContainsKey(StreamingPlatformTypeEnum.Twitch))
            {
                ChannelSession.Settings.StreamingPlatformAuthentications[StreamingPlatformTypeEnum.Twitch] = new StreamingPlatformAuthenticationSettingsModel(StreamingPlatformTypeEnum.Twitch);
            }

            Result<TwitchPlatformService> twitchResult = twitchResult = await TwitchPlatformService.Connect(ChannelSession.Settings.StreamingPlatformAuthentications[StreamingPlatformTypeEnum.Twitch].UserOAuthToken);
            if (twitchResult.Success)
            {
                ChannelSession.TwitchUserConnection = twitchResult.Value;
                userResult = twitchResult;
            }
            else
            {
                userResult = await ChannelSession.ConnectTwitchUser();
            }

            if (userResult.Success)
            {
                ChannelSession.Settings.StreamingPlatformAuthentications[StreamingPlatformTypeEnum.Twitch].IsEnabled = true;

                ChannelSession.TwitchUserNewAPI = await ChannelSession.TwitchUserConnection.GetNewAPICurrentUser();
                if (ChannelSession.TwitchUserNewAPI == null)
                {
                    return new Result(Resources.TwitchFailedNewAPIUserData);
                }

                ChannelSession.TwitchUserV5 = await ChannelSession.TwitchUserConnection.GetV5APIUserByLogin(ChannelSession.TwitchUserNewAPI.login);
                if (ChannelSession.TwitchUserV5 == null)
                {
                    return new Result(Resources.TwitchFailedV5APIUserData);
                }

                if (ChannelSession.Settings.StreamingPlatformAuthentications[StreamingPlatformTypeEnum.Twitch].BotOAuthToken != null)
                {
                    twitchResult = await TwitchPlatformService.Connect(ChannelSession.Settings.StreamingPlatformAuthentications[StreamingPlatformTypeEnum.Twitch].BotOAuthToken);
                    if (twitchResult.Success)
                    {
                        ChannelSession.TwitchBotConnection = twitchResult.Value;
                        ChannelSession.TwitchBotNewAPI = await ChannelSession.TwitchBotConnection.GetNewAPICurrentUser();
                        if (ChannelSession.TwitchBotNewAPI == null)
                        {
                            return new Result(Resources.TwitchFailedBotData);
                        }
                    }
                    else
                    {
                        ChannelSession.Settings.StreamingPlatformAuthentications[StreamingPlatformTypeEnum.Twitch].BotOAuthToken = null;
                        return new Result(success: true, message: "Failed to connect Twitch bot account, please manually reconnect");
                    }
                }
            }
            else
            {
                ChannelSession.Settings.StreamingPlatformAuthentications[StreamingPlatformTypeEnum.Twitch] = null;
                return userResult;
            }

            return userResult;
        }

        public static async Task DisconnectTwitchBot()
        {
            ChannelSession.TwitchBotConnection = null;
            if (ChannelSession.Services.Chat.TwitchChatService != null)
            {
                await ChannelSession.Services.Chat.TwitchChatService.DisconnectBot();
            }
        }

        public static async Task Close()
        {
            await ChannelSession.Services.Close();

            if (ChannelSession.Services.Chat.TwitchChatService != null)
            {
                await ChannelSession.Services.Chat.TwitchChatService.DisconnectUser();
            }
            await ChannelSession.DisconnectTwitchBot();
        }

        public static async Task SaveSettings()
        {
            await ChannelSession.Services.Settings.Save(ChannelSession.Settings);
        }

        public static async Task RefreshUser()
        {
            if (ChannelSession.TwitchUserNewAPI != null)
            {
                TwitchNewAPI.Users.UserModel twitchUserNewAPI = await ChannelSession.TwitchUserConnection.GetNewAPICurrentUser();
                if (twitchUserNewAPI != null)
                {
                    ChannelSession.TwitchUserNewAPI = twitchUserNewAPI;

                    TwitchV5API.Users.UserModel twitchUserV5 = await ChannelSession.TwitchUserConnection.GetV5APIUserByLogin(ChannelSession.TwitchUserNewAPI.login);
                    if (twitchUserV5 != null)
                    {
                        ChannelSession.TwitchUserV5 = twitchUserV5;
                    }
                }
            }
        }

        public static async Task RefreshChannel()
        {
            if (ChannelSession.TwitchChannelV5 != null)
            {
                TwitchV5API.Channel.ChannelModel twitchChannel = await ChannelSession.TwitchUserConnection.GetV5APIChannel(ChannelSession.TwitchChannelV5.id);
                if (twitchChannel != null)
                {
                    ChannelSession.TwitchChannelV5 = twitchChannel;
                    ChannelSession.TwitchStreamV5 = await ChannelSession.TwitchUserConnection.GetV5LiveStream(ChannelSession.TwitchChannelV5);
                }
            }

            if (ChannelSession.TwitchUserNewAPI != null)
            {
                ChannelSession.TwitchStreamNewAPI = await ChannelSession.TwitchUserConnection.GetStream(ChannelSession.TwitchUserNewAPI);
            }
        }

        public static UserViewModel GetCurrentUser()
        {
            // TO-DO: Update UserViewModel so that all platform accounts are combined into the same UserViewModel

            UserViewModel user = null;

            if (ChannelSession.TwitchUserNewAPI != null)
            {
                user = ChannelSession.Services.User.GetUserByTwitchID(ChannelSession.TwitchUserNewAPI.id);
                if (user == null)
                {
                    user = new UserViewModel(ChannelSession.TwitchUserNewAPI);
                }
            }

            return user;
        }

        public static void DisconnectionOccurred(string serviceName)
        {
            Logger.Log(serviceName + " Service disconnection occurred");
            GlobalEvents.ServiceDisconnect(serviceName);
        }

        public static void ReconnectionOccurred(string serviceName)
        {
            Logger.Log(serviceName + " Service reconnection successful");
            GlobalEvents.ServiceReconnect(serviceName);
        }

        public static async Task<bool> InitializeSession()
        {
            try
            {
                TwitchNewAPI.Users.UserModel twitchChannelNew = await ChannelSession.TwitchUserConnection.GetNewAPICurrentUser();
                TwitchV5API.Channel.ChannelModel twitchChannelv5 = await ChannelSession.TwitchUserConnection.GetCurrentV5APIChannel();
                if (twitchChannelNew != null && twitchChannelv5 != null)
                {
                    try
                    {
                        ChannelSession.TwitchUserNewAPI = twitchChannelNew;
                        ChannelSession.TwitchChannelV5 = twitchChannelv5;
                        ChannelSession.TwitchStreamNewAPI = await ChannelSession.TwitchUserConnection.GetStream(ChannelSession.TwitchUserNewAPI);
                        ChannelSession.TwitchStreamV5 = await ChannelSession.TwitchUserConnection.GetV5LiveStream(ChannelSession.TwitchChannelV5);

                        IEnumerable<TwitchV5API.Users.UserModel> channelEditors = await ChannelSession.TwitchUserConnection.GetV5APIChannelEditors(ChannelSession.TwitchChannelV5);
                        if (channelEditors != null)
                        {
                            foreach (TwitchV5API.Users.UserModel channelEditor in channelEditors)
                            {
                                ChannelSession.TwitchChannelEditorsV5.Add(channelEditor.id);
                            }
                        }

                        if (ChannelSession.Settings == null)
                        {
                            IEnumerable<SettingsV3Model> currentSettings = await ChannelSession.Services.Settings.GetAllSettings();

                            if (currentSettings.Any(s => !string.IsNullOrEmpty(s.StreamingPlatformAuthentications[StreamingPlatformTypeEnum.Twitch].ChannelID) && string.Equals(s.StreamingPlatformAuthentications[StreamingPlatformTypeEnum.Twitch].ChannelID, twitchChannelNew.id)))
                            {
                                GlobalEvents.ShowMessageBox(string.Format(Resources.TwitchAccountExists, twitchChannelNew.login));
                                return false;
                            }

                            ChannelSession.Settings = await ChannelSession.Services.Settings.Create(twitchChannelNew.display_name);
                        }
                        await ChannelSession.Services.Settings.Initialize(ChannelSession.Settings);

                        if (!string.IsNullOrEmpty(ChannelSession.Settings.StreamingPlatformAuthentications[StreamingPlatformTypeEnum.Twitch].UserID) && !string.Equals(ChannelSession.TwitchUserNewAPI.id, ChannelSession.Settings.StreamingPlatformAuthentications[StreamingPlatformTypeEnum.Twitch].UserID))
                        {
                            Logger.Log(LogLevel.Error, $"Signed in account does not match settings account: {ChannelSession.TwitchUserNewAPI.login} - {ChannelSession.TwitchUserNewAPI.id} - {ChannelSession.Settings.StreamingPlatformAuthentications[StreamingPlatformTypeEnum.Twitch].UserID}");
                            GlobalEvents.ShowMessageBox(Resources.TwitchAccountMismatch);
                            ChannelSession.Settings.StreamingPlatformAuthentications[StreamingPlatformTypeEnum.Twitch].UserOAuthToken.accessToken = string.Empty;
                            ChannelSession.Settings.StreamingPlatformAuthentications[StreamingPlatformTypeEnum.Twitch].UserOAuthToken.refreshToken = string.Empty;
                            ChannelSession.Settings.StreamingPlatformAuthentications[StreamingPlatformTypeEnum.Twitch].UserOAuthToken.expiresIn = 0;
                            return false;
                        }

                        ChannelSession.Settings.Name = ChannelSession.TwitchUserNewAPI.display_name;

                        ChannelSession.Settings.StreamingPlatformAuthentications[StreamingPlatformTypeEnum.Twitch].UserID = ChannelSession.TwitchUserNewAPI.id;
                        ChannelSession.Settings.StreamingPlatformAuthentications[StreamingPlatformTypeEnum.Twitch].ChannelID = ChannelSession.TwitchUserNewAPI.id;
                        if (ChannelSession.TwitchBotNewAPI != null)
                        {
                            ChannelSession.Settings.StreamingPlatformAuthentications[StreamingPlatformTypeEnum.Twitch].BotID = ChannelSession.TwitchBotNewAPI.id;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                        Logger.Log(LogLevel.Error, "Initialize Settings - " + JSONSerializerHelper.SerializeToString(ex));
                        await DialogHelper.ShowMessage(Resources.FailedToInitializeSettings +
                            Environment.NewLine + Environment.NewLine + Resources.ErrorDetailsHeader + " " + ex.Message);
                        return false;
                    }

                    try
                    {
                        await ChannelSession.Services.Telemetry.Connect();
                        ChannelSession.Services.Telemetry.SetUserID(ChannelSession.Settings.TelemetryUserID);

                        TwitchChatService twitchChatService = new TwitchChatService();
                        TwitchEventService twitchEventService = new TwitchEventService();

                        List<Task<Result>> twitchPlatformServiceTasks = new List<Task<Result>>();
                        twitchPlatformServiceTasks.Add(twitchChatService.ConnectUser());
                        twitchPlatformServiceTasks.Add(twitchEventService.Connect());

                        await Task.WhenAll(twitchPlatformServiceTasks);

                        if (twitchPlatformServiceTasks.Any(c => !c.Result.Success))
                        {
                            string errors = string.Join(Environment.NewLine, twitchPlatformServiceTasks.Where(c => !c.Result.Success).Select(c => c.Result.Message));
                            GlobalEvents.ShowMessageBox(Resources.TwitchFailed + Environment.NewLine + Environment.NewLine + errors);
                            return false;
                        }

                        await ChannelSession.Services.Chat.Initialize(twitchChatService);
                        await ChannelSession.Services.Events.Initialize(twitchEventService);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                        Logger.Log(LogLevel.Error, "Twitch Services - " + JSONSerializerHelper.SerializeToString(ex));
                        await DialogHelper.ShowMessage(Resources.FailedToConnectToTwitch +
                            Environment.NewLine + Environment.NewLine + Resources.ErrorDetailsHeader + " " + ex.Message);
                        return false;
                    }

                    Result result = await ChannelSession.InitializeBotInternal();
                    if (!result.Success)
                    {
                        await DialogHelper.ShowMessage(Resources.FailedToInitializeBotAccount);
                        return false;
                    }

                    try
                    {
                        // Connect External Services
                        Dictionary<IExternalService, OAuthTokenModel> externalServiceToConnect = new Dictionary<IExternalService, OAuthTokenModel>();
                        if (ChannelSession.Settings.StreamlabsOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.Streamlabs] = ChannelSession.Settings.StreamlabsOAuthToken; }
                        if (ChannelSession.Settings.StreamElementsOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.StreamElements] = ChannelSession.Settings.StreamElementsOAuthToken; }
                        if (ChannelSession.Settings.StreamJarOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.StreamJar] = ChannelSession.Settings.StreamJarOAuthToken; }
                        if (ChannelSession.Settings.TipeeeStreamOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.TipeeeStream] = ChannelSession.Settings.TipeeeStreamOAuthToken; }
                        if (ChannelSession.Settings.TreatStreamOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.TreatStream] = ChannelSession.Settings.TreatStreamOAuthToken; }
                        if (ChannelSession.Settings.StreamlootsOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.Streamloots] = ChannelSession.Settings.StreamlootsOAuthToken; }
                        if (ChannelSession.Settings.TiltifyOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.Tiltify] = ChannelSession.Settings.TiltifyOAuthToken; }
                        if (ChannelSession.Settings.JustGivingOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.JustGiving] = ChannelSession.Settings.JustGivingOAuthToken; }
                        if (ChannelSession.Settings.IFTTTOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.IFTTT] = ChannelSession.Settings.IFTTTOAuthToken; }
                        if (ChannelSession.Settings.ExtraLifeTeamID > 0) { externalServiceToConnect[ChannelSession.Services.ExtraLife] = new OAuthTokenModel(); }
                        if (ChannelSession.Settings.PatreonOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.Patreon] = ChannelSession.Settings.PatreonOAuthToken; }
                        if (ChannelSession.Settings.DiscordOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.Discord] = ChannelSession.Settings.DiscordOAuthToken; }
                        if (ChannelSession.Settings.TwitterOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.Twitter] = ChannelSession.Settings.TwitterOAuthToken; }
                        if (ChannelSession.Services.OBSStudio.IsEnabled) { externalServiceToConnect[ChannelSession.Services.OBSStudio] = null; }
                        if (ChannelSession.Services.StreamlabsOBS.IsEnabled) { externalServiceToConnect[ChannelSession.Services.StreamlabsOBS] = null; }
                        if (ChannelSession.Services.XSplit.IsEnabled) { externalServiceToConnect[ChannelSession.Services.XSplit] = null; }
                        if (!string.IsNullOrEmpty(ChannelSession.Settings.OvrStreamServerIP)) { externalServiceToConnect[ChannelSession.Services.OvrStream] = null; }
                        if (ChannelSession.Settings.EnableOverlay) { externalServiceToConnect[ChannelSession.Services.Overlay] = null; }
                        if (ChannelSession.Settings.EnableDeveloperAPI) { externalServiceToConnect[ChannelSession.Services.DeveloperAPI] = null; }

                        if (externalServiceToConnect.Count > 0)
                        {
                            Dictionary<IExternalService, Task<Result>> externalServiceTasks = new Dictionary<IExternalService, Task<Result>>();
                            foreach (var kvp in externalServiceToConnect)
                            {
                                Logger.Log(LogLevel.Debug, "Trying automatic OAuth service connection: " + kvp.Key.Name);

                                try
                                {
                                    if (kvp.Key is IOAuthExternalService && kvp.Value != null)
                                    {
                                        externalServiceTasks[kvp.Key] = ((IOAuthExternalService)kvp.Key).Connect(kvp.Value);
                                    }
                                    else
                                    {
                                        externalServiceTasks[kvp.Key] = kvp.Key.Connect();
                                    }
                                }
                                catch (Exception sex)
                                {
                                    Logger.Log(LogLevel.Error, "Error in external service initial connection: " + kvp.Key.Name);
                                    Logger.Log(sex);
                                }
                            }

                            try
                            {
                                await Task.WhenAll(externalServiceTasks.Values);
                            }
                            catch (Exception sex)
                            {
                                Logger.Log(LogLevel.Error, "Error in batch external service connection");
                                Logger.Log(sex);
                            }

                            List<IExternalService> failedServices = new List<IExternalService>();
                            foreach (var kvp in externalServiceTasks)
                            {
                                try
                                {
                                    if (kvp.Value.Result != null && !kvp.Value.Result.Success && kvp.Key is IOAuthExternalService)
                                    {
                                        Logger.Log(LogLevel.Debug, "Automatic OAuth token connection failed, trying manual connection: " + kvp.Key.Name);
                                        result = await kvp.Key.Connect();
                                        if (!result.Success)
                                        {
                                            failedServices.Add(kvp.Key);
                                        }
                                    }
                                }
                                catch (Exception sex)
                                {
                                    Logger.Log(LogLevel.Error, "Error in external service failed re-connection: " + kvp.Key.Name);
                                    Logger.Log(sex);
                                    failedServices.Add(kvp.Key);
                                }
                            }

                            if (failedServices.Count > 0)
                            {
                                Logger.Log(LogLevel.Debug, "Connection failed for services: " + string.Join(", ", failedServices.Select(s => s.Name)));

                                StringBuilder message = new StringBuilder();
                                message.AppendLine("The following services could not be connected:");
                                message.AppendLine();
                                foreach (IExternalService service in failedServices)
                                {
                                    message.AppendLine(" - " + service.Name);
                                }
                                message.AppendLine();
                                message.Append("Please go to the Services page to reconnect them manually.");
                                await DialogHelper.ShowMessage(message.ToString());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                        Logger.Log(LogLevel.Error, "External Services - " + JSONSerializerHelper.SerializeToString(ex));
                        await DialogHelper.ShowMessage(Resources.FailedToInitializeExternalServices +
                            Environment.NewLine + Environment.NewLine + Resources.ErrorDetailsHeader + " " + ex.Message);
                        return false;
                    }

                    try
                    {
                        //if (ChannelSession.Settings.RemoteHostConnection != null)
                        //{
                        //    await ChannelSession.Services.RemoteService.InitializeConnection(ChannelSession.Settings.RemoteHostConnection);
                        //}

                        foreach (CurrencyModel currency in ChannelSession.Settings.Currency.Values)
                        {
                            if (currency.ShouldBeReset())
                            {
                                await currency.Reset();
                            }
                        }

                        if (ChannelSession.Settings.ModerationResetStrikesOnLaunch)
                        {
                            foreach (UserDataModel userData in ChannelSession.Settings.UserData.Values)
                            {
                                if (userData.ModerationStrikes > 0)
                                {
                                    userData.ModerationStrikes = 0;
                                    ChannelSession.Settings.UserData.ManualValueChanged(userData.ID);
                                }
                            }
                        }

                        ChannelSession.PreMadeChatCommands.Clear();
                        foreach (PreMadeChatCommandModelBase command in ReflectionHelper.CreateInstancesOfImplementingType<PreMadeChatCommandModelBase>())
                        {
                            ChannelSession.PreMadeChatCommands.Add(command);
                        }

                        foreach (PreMadeChatCommandSettingsModel commandSetting in ChannelSession.Settings.PreMadeChatCommandSettings)
                        {
                            PreMadeChatCommandModelBase command = ChannelSession.PreMadeChatCommands.FirstOrDefault(c => c.Name.Equals(commandSetting.Name));
                            if (command != null)
                            {
                                command.UpdateFromSettings(commandSetting);
                            }
                        }
                        ChannelSession.Services.Chat.RebuildCommandTriggers();

                        await ChannelSession.Services.Timers.Initialize();
                        await ChannelSession.Services.Moderation.Initialize();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                        Logger.Log(LogLevel.Error, "Streamer Services - " + JSONSerializerHelper.SerializeToString(ex));
                        await DialogHelper.ShowMessage(Resources.FailedToInitializeStreamerBasedServices +
                            Environment.NewLine + Environment.NewLine + Resources.ErrorDetailsHeader + " " + ex.Message);
                        return false;
                    }

                    try
                    {
                        ChannelSession.Services.Statistics.Initialize();

                        ChannelSession.Services.InputService.HotKeyPressed += InputService_HotKeyPressed;

                        foreach (RedemptionStoreProductModel product in ChannelSession.Settings.RedemptionStoreProducts.Values)
                        {
                            product.ReplenishAmount();
                        }

                        foreach (RedemptionStorePurchaseModel purchase in ChannelSession.Settings.RedemptionStorePurchases.ToList())
                        {
                            if (purchase.State != RedemptionStorePurchaseRedemptionState.ManualRedeemNeeded)
                            {
                                ChannelSession.Settings.RedemptionStorePurchases.Remove(purchase);
                            }
                        }

                        ChannelSession.Services.Telemetry.TrackLogin(ChannelSession.Settings.TelemetryUserID, ChannelSession.TwitchUserNewAPI?.broadcaster_type);

                        await ChannelSession.SaveSettings();
                        await ChannelSession.Services.Settings.SaveLocalBackup(ChannelSession.Settings);
                        await ChannelSession.Services.Settings.PerformAutomaticBackupIfApplicable(ChannelSession.Settings);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        AsyncRunner.RunAsyncBackground(SessionBackgroundTask, sessionBackgroundCancellationTokenSource.Token, 60000);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                        Logger.Log(LogLevel.Error, "Finalize Initialization - " + JSONSerializerHelper.SerializeToString(ex));
                        await DialogHelper.ShowMessage(Resources.FailedToFinalizeInitialization +
                            Environment.NewLine + Environment.NewLine + Resources.ErrorDetailsHeader + " " + ex.Message);
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                Logger.Log(LogLevel.Error, "Channel Information - " + JSONSerializerHelper.SerializeToString(ex));
                await DialogHelper.ShowMessage(Resources.FailedToGetChannelInformation +
                    Environment.NewLine + Environment.NewLine + Resources.ErrorDetailsHeader + " " + ex.Message);
            }
            return false;
        }

        private static async Task<Result> InitializeBotInternal()
        {
            if (ChannelSession.TwitchBotConnection != null)
            {
                Result result = await ChannelSession.Services.Chat.TwitchChatService.ConnectBot();
                if (!result.Success)
                {
                    return result;
                }
            }

            return new Result();
        }

        private static async Task SessionBackgroundTask(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                sessionBackgroundTimer++;

                await ChannelSession.RefreshUser();

                await ChannelSession.RefreshChannel();

                if (sessionBackgroundTimer >= 5)
                {
                    await ChannelSession.SaveSettings();
                    sessionBackgroundTimer = 0;

                    if (ChannelSession.TwitchStreamIsLive)
                    {
                        try
                        {
                            string type = null;
                            if (ChannelSession.TwitchUserNewAPI.IsPartner())
                            {
                                type = "Partner";
                            }
                            else if (ChannelSession.TwitchUserNewAPI.IsAffiliate())
                            {
                                type = "Affiliate";
                            }
                            ChannelSession.Services.Telemetry.TrackChannelMetrics(type, ChannelSession.TwitchStreamV5.viewers, ChannelSession.Services.Chat.AllUsers.Count,
                                ChannelSession.TwitchStreamV5.game, ChannelSession.TwitchChannelV5.views, ChannelSession.TwitchChannelV5.followers);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex);
                        }
                    }
                }
            }
        }

        private static async void InputService_HotKeyPressed(object sender, HotKey hotKey)
        {
            if (ChannelSession.Settings.HotKeys.ContainsKey(hotKey.ToString()))
            {
                HotKeyConfiguration hotKeyConfiguration = ChannelSession.Settings.HotKeys[hotKey.ToString()];
                CommandModelBase command = ChannelSession.Settings.GetCommand(hotKeyConfiguration.CommandID);
                if (command != null)
                {
                    await command.Perform();
                }
            }
        }
    }
}