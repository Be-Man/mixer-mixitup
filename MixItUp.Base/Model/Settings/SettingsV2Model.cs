﻿using MixItUp.Base.Actions;
using MixItUp.Base.Commands;
using MixItUp.Base.Model.Currency;
using MixItUp.Base.Model.Overlay;
using MixItUp.Base.Model.Remote.Authentication;
using MixItUp.Base.Model.Serial;
using MixItUp.Base.Model.User;
using MixItUp.Base.Remote.Models;
using MixItUp.Base.Services;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.Requirement;
using MixItUp.Base.ViewModel.User;
using MixItUp.Base.ViewModel.Dashboard;
using Newtonsoft.Json;
using StreamingClient.Base.Model.OAuth;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using MixItUp.Base.Services.Twitch;
using MixItUp.Base.Services.External;

namespace MixItUp.Base.Model.Settings
{
    [Obsolete]
    [DataContract]
    public class SettingsV2Model
    {
        public const int LatestVersion = 45;

        public const string SettingsDirectoryName = "Settings";
        public const string DefaultAutomaticBackupSettingsDirectoryName = "AutomaticBackups";

        public const string SettingsTemplateDatabaseFileName = "SettingsTemplateDatabase.db";

        public const string SettingsFileExtension = "miu";
        public const string DatabaseFileExtension = "db";
        public const string SettingsLocalBackupFileExtension = "backup";

        public const string SettingsBackupFileExtension = "miubackup";

        [DataMember]
        public int Version { get; set; } = SettingsV2Model.LatestVersion;

        [DataMember]
        public Guid ID { get; set; } = Guid.NewGuid();
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public bool IsStreamer { get; set; }

        [DataMember]
        public string TelemetryUserID { get; set; }

        [DataMember]
        public string SettingsBackupLocation { get; set; }
        [DataMember]
        public SettingsBackupRateEnum SettingsBackupRate { get; set; }
        [DataMember]
        public DateTimeOffset SettingsLastBackup { get; set; }

        #region Authentication

        [DataMember]
        public uint MixerUserID { get; set; }
        [DataMember]
        public uint MixerChannelID { get; set; }

        [JsonProperty]
        public OAuthTokenModel TwitchUserOAuthToken { get; set; }
        [JsonProperty]
        public OAuthTokenModel TwitchBotOAuthToken { get; set; }
        [JsonProperty]
        public string TwitchUserID { get; set; }
        [JsonProperty]
        public string TwitchChannelID { get; set; }

        [DataMember]
        public OAuthTokenModel StreamlabsOAuthToken { get; set; }
        [DataMember]
        public OAuthTokenModel StreamElementsOAuthToken { get; set; }
        [DataMember]
        public OAuthTokenModel TwitterOAuthToken { get; set; }
        [DataMember]
        public OAuthTokenModel DiscordOAuthToken { get; set; }
        [DataMember]
        public OAuthTokenModel TiltifyOAuthToken { get; set; }
        [DataMember]
        public OAuthTokenModel TipeeeStreamOAuthToken { get; set; }
        [DataMember]
        public OAuthTokenModel TreatStreamOAuthToken { get; set; }
        [DataMember]
        public OAuthTokenModel StreamJarOAuthToken { get; set; }
        [DataMember]
        public OAuthTokenModel PatreonOAuthToken { get; set; }
        [DataMember]
        public OAuthTokenModel IFTTTOAuthToken { get; set; }
        [DataMember]
        public OAuthTokenModel StreamlootsOAuthToken { get; set; }
        [DataMember]
        public OAuthTokenModel JustGivingOAuthToken { get; set; }

        #endregion Authentication

        #region General

        [DataMember]
        public bool OptOutTracking { get; set; }
        [DataMember]
        public bool FeatureMe { get; set; }
        [DataMember]
        public StreamingSoftwareTypeEnum DefaultStreamingSoftware { get; set; } = StreamingSoftwareTypeEnum.OBSStudio;
        [DataMember]
        public string DefaultAudioOutput { get; set; }
        [DataMember]
        public bool SaveChatEventLogs { get; set; }

        #endregion General

        #region Chat

        [DataMember]
        public int MaxMessagesInChat { get; set; } = 100;
        [DataMember]
        public int MaxUsersShownInChat { get; set; } = 100;

        [DataMember]
        public int ChatFontSize { get; set; } = 13;
        [DataMember]
        public bool AddSeparatorsBetweenMessages { get; set; }
        [DataMember]
        public bool UseAlternatingBackgroundColors { get; set; }

        [DataMember]
        public bool OnlyShowAlertsInDashboard { get; set; }
        [DataMember]
        public bool LatestChatAtTop { get; set; }
        [DataMember]
        public bool TrackWhispererNumber { get; set; }
        [DataMember]
        public bool ShowChatMessageTimestamps { get; set; }

        [DataMember]
        public bool HideViewerAndChatterNumbers { get; set; }
        [DataMember]
        public bool HideChatUserList { get; set; }
        [DataMember]
        public bool HideDeletedMessages { get; set; }
        [DataMember]
        public bool HideBotMessages { get; set; }

        [DataMember]
        public bool ShowBetterTTVEmotes { get; set; }
        [DataMember]
        public bool ShowFrankerFaceZEmotes { get; set; }

        [DataMember]
        public bool HideUserAvatar { get; set; }
        [DataMember]
        public bool HideUserRoleBadge { get; set; }
        [DataMember]
        public bool HideUserSubscriberBadge { get; set; }
        [DataMember]
        public bool HideUserSpecialtyBadge { get; set; }

        [DataMember]
        public bool UseCustomUsernameColors { get; set; }
        [DataMember]
        public Dictionary<UserRoleEnum, string> CustomUsernameColors { get; set; } = new Dictionary<UserRoleEnum, string>();

        #endregion Chat

        #region Commands

        [DataMember]
        public bool AllowCommandWhispering { get; set; }
        [DataMember]
        public bool IgnoreBotAccountCommands { get; set; }
        [DataMember]
        public bool DeleteChatCommandsWhenRun { get; set; }
        [DataMember]
        public bool UnlockAllCommands { get; set; }

        [DataMember]
        public int TwitchMassGiftedSubsFilterAmount { get; set; } = 1;

        [DataMember]
        public HashSet<ActionTypeEnum> ActionsToHide { get; set; } = new HashSet<ActionTypeEnum>();

        #endregion Commands

        #region Alerts

        [DataMember]
        public string AlertUserJoinLeaveColor { get; set; }
        [DataMember]
        public string AlertFollowColor { get; set; }
        [DataMember]
        public string AlertHostColor { get; set; }
        [DataMember]
        public string AlertRaidColor { get; set; }
        [DataMember]
        public string AlertSubColor { get; set; }
        [DataMember]
        public string AlertGiftedSubColor { get; set; }
        [DataMember]
        public string AlertMassGiftedSubColor { get; set; }
        [DataMember]
        public string AlertBitsCheeredColor { get; set; }
        [DataMember]
        public string AlertChannelPointsColor { get; set; }
        [DataMember]
        public string AlertModerationColor { get; set; }

        #endregion Alerts

        #region Notifications

        [DataMember]
        public string NotificationsAudioOutput { get; set; }

        [DataMember]
        public string NotificationChatMessageSoundFilePath { get; set; }
        [DataMember]
        public int NotificationChatMessageSoundVolume { get; set; } = 100;
        [DataMember]
        public string NotificationChatTaggedSoundFilePath { get; set; }
        [DataMember]
        public int NotificationChatTaggedSoundVolume { get; set; } = 100;
        [DataMember]
        public string NotificationChatWhisperSoundFilePath { get; set; }
        [DataMember]
        public int NotificationChatWhisperSoundVolume { get; set; } = 100;
        [DataMember]
        public string NotificationServiceConnectSoundFilePath { get; set; }
        [DataMember]
        public int NotificationServiceConnectSoundVolume { get; set; } = 100;
        [DataMember]
        public string NotificationServiceDisconnectSoundFilePath { get; set; }
        [DataMember]
        public int NotificationServiceDisconnectSoundVolume { get; set; } = 100;

        #endregion Notifications

        #region Users

        [DataMember]
        public int RegularUserMinimumHours { get; set; }
        [DataMember]
        public List<UserTitleModel> UserTitles { get; set; } = new List<UserTitleModel>();

        #endregion Users

        #region Game Queue

        [DataMember]
        public bool GameQueueSubPriority { get; set; }
        [DataMember]
        public RequirementViewModel GameQueueRequirements { get; set; } = new RequirementViewModel();
        [DataMember]
        public CustomCommand GameQueueUserJoinedCommand { get; set; }
        [DataMember]
        public CustomCommand GameQueueUserSelectedCommand { get; set; }

        #endregion Game Queue

        #region Quotes

        [DataMember]
        public bool QuotesEnabled { get; set; }
        [DataMember]
        public string QuotesFormat { get; set; }

        #endregion Quotes

        #region Timers

        [DataMember]
        public int TimerCommandsInterval { get; set; } = 10;
        [DataMember]
        public int TimerCommandsMinimumMessages { get; set; } = 10;
        [DataMember]
        public bool DisableAllTimers { get; set; }

        #endregion Timers

        #region Giveaway

        [DataMember]
        public string GiveawayCommand { get; set; } = "giveaway";
        [DataMember]
        public int GiveawayTimer { get; set; } = 1;
        [DataMember]
        public int GiveawayMaximumEntries { get; set; } = 1;
        [DataMember]
        public RequirementViewModel GiveawayRequirements { get; set; } = new RequirementViewModel();
        [DataMember]
        public int GiveawayReminderInterval { get; set; } = 5;
        [DataMember]
        public bool GiveawayRequireClaim { get; set; } = true;
        [DataMember]
        public bool GiveawayAllowPastWinners { get; set; }
        [DataMember]
        public CustomCommand GiveawayStartedReminderCommand { get; set; }
        [DataMember]
        public CustomCommand GiveawayUserJoinedCommand { get; set; }
        [DataMember]
        public CustomCommand GiveawayWinnerSelectedCommand { get; set; }

        #endregion Giveaway

        #region Moderation

        [DataMember]
        public bool ModerationUseCommunityFilteredWords { get; set; }
        [DataMember]
        public List<string> FilteredWords { get; set; } = new List<string>();
        [DataMember]
        public List<string> BannedWords { get; set; } = new List<string>();

        [DataMember]
        public int ModerationFilteredWordsTimeout1MinuteOffenseCount { get; set; }
        [DataMember]
        public int ModerationFilteredWordsTimeout5MinuteOffenseCount { get; set; }
        [DataMember]
        public UserRoleEnum ModerationFilteredWordsExcempt { get; set; } = UserRoleEnum.Mod;
        [DataMember]
        public bool ModerationFilteredWordsApplyStrikes { get; set; } = true;

        [DataMember]
        public int ModerationCapsBlockCount { get; set; }
        [DataMember]
        public bool ModerationCapsBlockIsPercentage { get; set; } = true;
        [DataMember]
        public int ModerationPunctuationBlockCount { get; set; }
        [DataMember]
        public bool ModerationPunctuationBlockIsPercentage { get; set; } = true;
        [DataMember]
        public UserRoleEnum ModerationChatTextExcempt { get; set; } = UserRoleEnum.Mod;
        [DataMember]
        public bool ModerationChatTextApplyStrikes { get; set; } = true;

        [DataMember]
        public bool ModerationBlockLinks { get; set; }
        [DataMember]
        public UserRoleEnum ModerationBlockLinksExcempt { get; set; } = UserRoleEnum.Mod;
        [DataMember]
        public bool ModerationBlockLinksApplyStrikes { get; set; } = true;

        [DataMember]
        public ModerationChatInteractiveParticipationEnum ModerationChatInteractiveParticipation { get; set; } = ModerationChatInteractiveParticipationEnum.None;
        [DataMember]
        public UserRoleEnum ModerationChatInteractiveParticipationExcempt { get; set; } = UserRoleEnum.Mod;

        [DataMember]
        public bool ModerationResetStrikesOnLaunch { get; set; }
        [DataMember]
        public CustomCommand ModerationStrike1Command { get; set; }
        [DataMember]
        public CustomCommand ModerationStrike2Command { get; set; }
        [DataMember]
        public CustomCommand ModerationStrike3Command { get; set; }

        #endregion Moderation

        #region Overlay

        [DataMember]
        public bool EnableOverlay { get; set; }
        [DataMember]
        public Dictionary<string, int> OverlayCustomNameAndPorts { get; set; } = new Dictionary<string, int>();
        [DataMember]
        public string OverlaySourceName { get; set; }

        [DataMember]
        public List<OverlayWidgetModel> OverlayWidgets { get; set; } = new List<OverlayWidgetModel>();
        [DataMember]
        public int OverlayWidgetRefreshTime { get; set; } = 5;

        #endregion Overlay

        #region Remote

        [DataMember]
        public RemoteConnectionAuthenticationTokenModel RemoteHostConnection { get; set; }
        [DataMember]
        public List<RemoteConnectionModel> RemoteClientConnections { get; set; } = new List<RemoteConnectionModel>();

        [DataMember]
        public List<RemoteProfileModel> RemoteProfiles { get; set; } = new List<RemoteProfileModel>();
        [DataMember]
        public Dictionary<Guid, RemoteProfileBoardsModel> RemoteProfileBoards { get; set; } = new Dictionary<Guid, RemoteProfileBoardsModel>();

        #endregion Remote

        #region Services

        [DataMember]
        public string OvrStreamServerIP { get; set; }

        [DataMember]
        public string OBSStudioServerIP { get; set; }
        [DataMember]
        public string OBSStudioServerPassword { get; set; }

        [DataMember]
        public bool EnableStreamlabsOBSConnection { get; set; }

        [DataMember]
        public bool EnableXSplitConnection { get; set; }

        [DataMember]
        public bool EnableDeveloperAPI { get; set; }
        [DataMember]
        public bool EnableDeveloperAPIAdvancedMode { get; set; }

        [DataMember]
        public int TiltifyCampaign { get; set; }

        [DataMember]
        public int ExtraLifeTeamID { get; set; }
        [DataMember]
        public int ExtraLifeParticipantID { get; set; }
        [DataMember]
        public bool ExtraLifeIncludeTeamDonations { get; set; }

        [DataMember]
        public string JustGivingPageShortName { get; set; }

        [DataMember]
        public string DiscordServer { get; set; }
        [DataMember]
        public string DiscordCustomClientID { get; set; }
        [DataMember]
        public string DiscordCustomClientSecret { get; set; }
        [DataMember]
        public string DiscordCustomBotToken { get; set; }

        [DataMember]
        public string PatreonTierMixerSubscriberEquivalent { get; set; }

        [DataMember]
        public List<SerialDeviceModel> SerialDevices { get; set; } = new List<SerialDeviceModel>();

        #endregion Services

        #region Dashboard

        [DataMember]
        public DashboardLayoutTypeEnum DashboardLayout { get; set; }
        [DataMember]
        public List<DashboardItemTypeEnum> DashboardItems { get; set; } = new List<DashboardItemTypeEnum>();
        [DataMember]
        public List<Guid> DashboardQuickCommands { get; set; } = new List<Guid>();

        #endregion Dashboard

        #region Advanced

        [DataMember]
        public bool ReRunWizard { get; set; }

        [DataMember]
        [Obsolete]
        public bool DiagnosticLogging { get; set; }

        #endregion Advanced

        #region Currency

        [DataMember]
        public Dictionary<Guid, CurrencyModel> Currency { get; set; } = new Dictionary<Guid, CurrencyModel>();

        [DataMember]
        public Dictionary<Guid, InventoryModel> Inventory { get; set; } = new Dictionary<Guid, InventoryModel>();

        [DataMember]
        public Dictionary<Guid, StreamPassModel> StreamPass { get; set; } = new Dictionary<Guid, StreamPassModel>();

        [DataMember]
        public bool RedemptionStoreEnabled { get; set; }
        [DataMember]
        public Dictionary<Guid, RedemptionStoreProductModel> RedemptionStoreProducts { get; set; } = new Dictionary<Guid, RedemptionStoreProductModel>();
        [DataMember]
        public string RedemptionStoreChatPurchaseCommand { get; set; } = "!purchase";
        [DataMember]
        public string RedemptionStoreModRedeemCommand { get; set; } = "!redeem";
        [DataMember]
        public Guid RedemptionStoreManualRedeemNeededCommandID { get; set; }
        [DataMember]
        public Guid RedemptionStoreDefaultRedemptionCommandID { get; set; }
        [DataMember]
        public List<RedemptionStorePurchaseModel> RedemptionStorePurchases { get; set; } = new List<RedemptionStorePurchaseModel>();

        #endregion Currency

        [DataMember]
        public Dictionary<string, int> CooldownGroups { get; set; } = new Dictionary<string, int>();

        [DataMember]
        public List<PreMadeChatCommandSettings> PreMadeChatCommandSettings { get; set; } = new List<PreMadeChatCommandSettings>();

        [DataMember]
        public List<string> RecentStreamTitles { get; set; } = new List<string>();
        [DataMember]
        public List<string> RecentStreamGames { get; set; } = new List<string>();

        [DataMember]
        public Dictionary<string, object> LatestSpecialIdentifiersData { get; set; } = new Dictionary<string, object>();

        [DataMember]
        public Dictionary<string, CommandGroupSettings> CommandGroups { get; set; } = new Dictionary<string, CommandGroupSettings>();
        [DataMember]
        public Dictionary<string, HotKeyConfiguration> HotKeys { get; set; } = new Dictionary<string, HotKeyConfiguration>();
        [DataMember]
        public Dictionary<string, CounterModel> Counters { get; set; } = new Dictionary<string, CounterModel>();

        #region Database Data

        [JsonIgnore]
        public DatabaseList<ChatCommand> ChatCommands { get; set; } = new DatabaseList<ChatCommand>();
        [JsonIgnore]
        public DatabaseList<EventCommand> EventCommands { get; set; } = new DatabaseList<EventCommand>();
        [JsonIgnore]
        public DatabaseList<TimerCommand> TimerCommands { get; set; } = new DatabaseList<TimerCommand>();
        [JsonIgnore]
        public DatabaseList<ActionGroupCommand> ActionGroupCommands { get; set; } = new DatabaseList<ActionGroupCommand>();
        [JsonIgnore]
        public DatabaseList<GameCommandBase> GameCommands { get; set; } = new DatabaseList<GameCommandBase>();
        [JsonIgnore]
        public DatabaseList<TwitchChannelPointsCommand> TwitchChannelPointsCommands { get; set; } = new DatabaseList<TwitchChannelPointsCommand>();
        [JsonIgnore]
        public DatabaseDictionary<Guid, CustomCommand> CustomCommands { get; set; } = new DatabaseDictionary<Guid, CustomCommand>();

        [JsonIgnore]
        public List<MixPlayCommand> OldMixPlayCommands { get; set; } = new List<MixPlayCommand>();

        [JsonIgnore]
        public DatabaseList<UserQuoteViewModel> Quotes { get; set; } = new DatabaseList<UserQuoteViewModel>();

        [JsonIgnore]
        public DatabaseDictionary<Guid, UserDataModel> UserData { get; set; } = new DatabaseDictionary<Guid, UserDataModel>();
        [JsonIgnore]
        private Dictionary<string, Guid> TwitchUserIDLookups { get; set; } = new Dictionary<string, Guid>();
        [JsonIgnore]
        private Dictionary<StreamingPlatformTypeEnum, Dictionary<string, Guid>> UsernameLookups { get; set; } = new Dictionary<StreamingPlatformTypeEnum, Dictionary<string, Guid>>();

        #endregion Database Data

        #region Obsolete

        [DataMember]
        [Obsolete]
        public bool ChatShowUserJoinLeave { get; set; }
        [DataMember]
        [Obsolete]
        public string ChatUserJoinLeaveColorScheme { get; set; } = null;
        [DataMember]
        [Obsolete]
        public bool ChatShowEventAlerts { get; set; }
        [DataMember]
        [Obsolete]
        public string ChatEventAlertsColorScheme { get; set; } = null;

        #endregion Obsolete

        [JsonIgnore]
        public string SettingsFileName { get { return string.Format("{0}.{1}", this.ID, SettingsV2Model.SettingsFileExtension); } }
        [JsonIgnore]
        public string SettingsFilePath { get { return Path.Combine(SettingsV2Model.SettingsDirectoryName, this.SettingsFileName); } }

        [JsonIgnore]
        public string DatabaseFileName { get { return string.Format("{0}.{1}", this.ID, SettingsV2Model.DatabaseFileExtension); } }
        [JsonIgnore]
        public string DatabaseFilePath { get { return Path.Combine(SettingsV2Model.SettingsDirectoryName, this.DatabaseFileName); } }

        [JsonIgnore]
        public string SettingsLocalBackupFileName { get { return string.Format("{0}.{1}.{2}", this.ID, SettingsV2Model.SettingsFileExtension, SettingsV2Model.SettingsLocalBackupFileExtension); } }
        [JsonIgnore]
        public string SettingsLocalBackupFilePath { get { return Path.Combine(SettingsV2Model.SettingsDirectoryName, this.SettingsLocalBackupFileName); } }

        public SettingsV2Model() { }

        public SettingsV2Model(string name, bool isStreamer = true)
            : this()
        {
            this.Name = name;
            this.IsStreamer = isStreamer;

            this.InitializeMissingData();
        }

        public async Task Initialize()
        {
            if (this.IsStreamer)
            {
                if (!ServiceManager.Get<IFileService>().FileExists(this.DatabaseFilePath))
                {
                    await ServiceManager.Get<IFileService>().CopyFile(SettingsV2Model.SettingsTemplateDatabaseFileName, this.DatabaseFilePath);
                }

                foreach (StreamingPlatformTypeEnum platform in StreamingPlatforms.Platforms)
                {
                    this.UsernameLookups[platform] = new Dictionary<string, Guid>();
                }

                await ServiceManager.Get<IDatabaseService>().Read(this.DatabaseFilePath, "SELECT * FROM Users", (Dictionary<string, object> data) =>
                {
                    UserDataModel userData = JSONSerializerHelper.DeserializeFromString<UserDataModel>((string)data["Data"]);
                    this.UserData[userData.ID] = userData;
                    if (userData.Platform.HasFlag(StreamingPlatformTypeEnum.Twitch))
                    {
                        this.TwitchUserIDLookups[userData.TwitchID] = userData.ID;
                        if (!string.IsNullOrEmpty(userData.TwitchUsername))
                        {
                            this.UsernameLookups[StreamingPlatformTypeEnum.Twitch][userData.TwitchUsername.ToLowerInvariant()] = userData.ID;
                        }
                    }
#pragma warning disable CS0612 // Type or member is obsolete
                    else if (userData.Platform.HasFlag(StreamingPlatformTypeEnum.Mixer))
                    {
                        if (!string.IsNullOrEmpty(userData.MixerUsername))
                        {
                            this.UsernameLookups[StreamingPlatformTypeEnum.Mixer][userData.MixerUsername.ToLowerInvariant()] = userData.ID;
                        }
                    }
#pragma warning restore CS0612 // Type or member is obsolete
                });
                this.UserData.ClearTracking();

                await ServiceManager.Get<IDatabaseService>().Read(this.DatabaseFilePath, "SELECT * FROM Quotes", (Dictionary<string, object> data) =>
                {
                    string json = (string)data["Data"];
                    if (json.Contains("MixItUp.Base.ViewModel.User.UserQuoteViewModel"))
                    {
                        json = json.Replace("MixItUp.Base.ViewModel.User.UserQuoteViewModel", "MixItUp.Base.Model.User.UserQuoteModel");
                        this.Quotes.Add(new UserQuoteViewModel(JSONSerializerHelper.DeserializeFromString<UserQuoteModel>(json)));
                    }
                    else
                    {
                        this.Quotes.Add(new UserQuoteViewModel(JSONSerializerHelper.DeserializeFromString<UserQuoteModel>((string)data["Data"])));
                    }
                });
                this.Quotes.ClearTracking();

                await ServiceManager.Get<IDatabaseService>().Read(this.DatabaseFilePath, "SELECT * FROM Commands", (Dictionary<string, object> data) =>
                {
                    CommandTypeEnum type = (CommandTypeEnum)Convert.ToInt32(data["TypeID"]);
                    string commandData = (string)data["Data"];
                    if (type == CommandTypeEnum.Chat)
                    {
                        this.ChatCommands.Add(JSONSerializerHelper.DeserializeFromString<ChatCommand>(commandData));
                    }
                    else if (type == CommandTypeEnum.Event)
                    {
                        this.EventCommands.Add(JSONSerializerHelper.DeserializeFromString<EventCommand>(commandData));
                    }
                    else if (type == CommandTypeEnum.Timer)
                    {
                        this.TimerCommands.Add(JSONSerializerHelper.DeserializeFromString<TimerCommand>(commandData));
                    }
                    else if (type == CommandTypeEnum.ActionGroup)
                    {
                        this.ActionGroupCommands.Add(JSONSerializerHelper.DeserializeFromString<ActionGroupCommand>(commandData));
                    }
                    else if (type == CommandTypeEnum.Game)
                    {
                        commandData = commandData.Replace("MixItUp.Base.ViewModel.User.UserRoleEnum", "MixItUp.Base.Model.User.UserRoleEnum");
                        this.GameCommands.Add(JSONSerializerHelper.DeserializeFromString<GameCommandBase>(commandData));
                    }
                    else if (type == CommandTypeEnum.TwitchChannelPoints)
                    {
                        this.TwitchChannelPointsCommands.Add(JSONSerializerHelper.DeserializeFromString<TwitchChannelPointsCommand>(commandData));
                    }
                    else if (type == CommandTypeEnum.Custom)
                    {
                        CustomCommand command = JSONSerializerHelper.DeserializeFromString<CustomCommand>(commandData);
                        this.CustomCommands[command.ID] = command;
                    }
#pragma warning disable CS0612 // Type or member is obsolete
                    else if (type == CommandTypeEnum.Interactive)
                    {
                        MixPlayCommand command = JSONSerializerHelper.DeserializeFromString<MixPlayCommand>(commandData);
                        if (command is MixPlayButtonCommand || command is MixPlayTextBoxCommand)
                        {
                            this.OldMixPlayCommands.Add(command);
                        }
                    }
#pragma warning restore CS0612 // Type or member is obsolete
                });

                this.ChatCommands.ClearTracking();
                this.EventCommands.ClearTracking();
                this.TimerCommands.ClearTracking();
                this.ActionGroupCommands.ClearTracking();
                this.GameCommands.ClearTracking();
                this.TwitchChannelPointsCommands.ClearTracking();
                this.CustomCommands.ClearTracking();

                foreach (CounterModel counter in this.Counters.Values.ToList())
                {
                    if (counter.ResetOnLoad)
                    {
                        await counter.ResetAmount();
                    }
                }
            }

            if (string.IsNullOrEmpty(this.TelemetryUserID))
            {
                if (ChannelSession.IsDebug())
                {
                    this.TelemetryUserID = "MixItUpDebuggingUser";
                }
                else
                {
                    this.TelemetryUserID = Guid.NewGuid().ToString();
                }
            }

            // Mod accounts cannot use this feature, forcefully disable on load
            if (!this.IsStreamer)
            {
                this.TrackWhispererNumber = false;
            }

            this.InitializeMissingData();
        }

        public async Task ClearAllUserData()
        {
            this.UserData.Clear();
            await ServiceManager.Get<IDatabaseService>().Write(this.DatabaseFilePath, "DELETE FROM Users");
        }

        public void CopyLatestValues()
        {
            Logger.Log(LogLevel.Debug, "Copying over latest values into Settings object");

            this.Version = SettingsV2Model.LatestVersion;

            if (ServiceManager.Get<TwitchSessionService>().UserConnection != null)
            {
                this.TwitchUserOAuthToken = ServiceManager.Get<TwitchSessionService>().UserConnection.Connection.GetOAuthTokenCopy();
            }
            if (ServiceManager.Get<TwitchSessionService>().BotConnection != null)
            {
                this.TwitchBotOAuthToken = ServiceManager.Get<TwitchSessionService>().BotConnection.Connection.GetOAuthTokenCopy();
            }

            if (ServiceManager.Get<StreamlabsService>().IsConnected)
            {
                this.StreamlabsOAuthToken = ServiceManager.Get<StreamlabsService>().GetOAuthTokenCopy();
            }
            if (ServiceManager.Get<StreamElementsService>().IsConnected)
            {
                this.StreamElementsOAuthToken = ServiceManager.Get<StreamElementsService>().GetOAuthTokenCopy();
            }
            if (ServiceManager.Get<StreamJarService>().IsConnected)
            {
                this.StreamJarOAuthToken = ServiceManager.Get<StreamJarService>().GetOAuthTokenCopy();
            }
            if (ServiceManager.Get<TipeeeStreamService>().IsConnected)
            {
                this.TipeeeStreamOAuthToken = ServiceManager.Get<TipeeeStreamService>().GetOAuthTokenCopy();
            }
            if (ServiceManager.Get<TreatStreamService>().IsConnected)
            {
                this.TreatStreamOAuthToken = ServiceManager.Get<TreatStreamService>().GetOAuthTokenCopy();
            }
            if (ServiceManager.Get<StreamlootsService>().IsConnected)
            {
                this.StreamlootsOAuthToken = ServiceManager.Get<StreamlootsService>().GetOAuthTokenCopy();
            }
            if (ServiceManager.Get<TiltifyService>().IsConnected)
            {
                this.TiltifyOAuthToken = ServiceManager.Get<TiltifyService>().GetOAuthTokenCopy();
            }
            if (ServiceManager.Get<PatreonService>().IsConnected)
            {
                this.PatreonOAuthToken = ServiceManager.Get<PatreonService>().GetOAuthTokenCopy();
            }
            if (ServiceManager.Get<IFTTTService>().IsConnected)
            {
                this.IFTTTOAuthToken = ServiceManager.Get<IFTTTService>().GetOAuthTokenCopy();
            }
            if (ServiceManager.Get<JustGivingService>().IsConnected)
            {
                this.JustGivingOAuthToken = ServiceManager.Get<JustGivingService>().GetOAuthTokenCopy();
            }
            if (ServiceManager.Get<DiscordService>().IsConnected)
            {
                this.DiscordOAuthToken = ServiceManager.Get<DiscordService>().GetOAuthTokenCopy();
            }
            if (ServiceManager.Get<TwitterService>().IsConnected)
            {
                this.TwitterOAuthToken = ServiceManager.Get<TwitterService>().GetOAuthTokenCopy();
            }

            // Clear out unused Cooldown Groups and Command Groups
            var allUsedCooldownGroupNames =
                this.ChatCommands.Select(c => c.Requirements?.Cooldown?.GroupName)
                .Union(this.GameCommands.Select(c => c.Requirements?.Cooldown?.GroupName))
                .Distinct();
            var allUnusedCooldownGroupNames = this.CooldownGroups.ToList().Where(c => !allUsedCooldownGroupNames.Contains(c.Key, StringComparer.InvariantCultureIgnoreCase));
            foreach (var unused in allUnusedCooldownGroupNames)
            {
                this.CooldownGroups.Remove(unused.Key);
            }

            var allUsedCommandGroupNames =
                this.ChatCommands.Select(c => c.GroupName)
                .Union(this.ActionGroupCommands.Select(a => a.GroupName))
                .Union(this.TimerCommands.Select(a => a.GroupName))
                .Distinct();
            var allUnusedCommandGroupNames = this.CommandGroups.ToList().Where(c => !allUsedCommandGroupNames.Contains(c.Key, StringComparer.InvariantCultureIgnoreCase));
            foreach (var unused in allUnusedCommandGroupNames)
            {
                this.CommandGroups.Remove(unused.Key);
            }
        }

        public async Task SaveDatabaseData()
        {
            if (this.IsStreamer)
            {
                IEnumerable<Guid> removedUsers = this.UserData.GetRemovedValues();
                await ServiceManager.Get<IDatabaseService>().BulkWrite(this.DatabaseFilePath, "DELETE FROM Users WHERE ID = @ID", removedUsers.Select(u => new Dictionary<string, object>() { { "@ID", u.ToString() } }));

                IEnumerable<UserDataModel> changedUsers = this.UserData.GetChangedValues();
                await ServiceManager.Get<IDatabaseService>().BulkWrite(this.DatabaseFilePath, "REPLACE INTO Users(ID, Data) VALUES(@ID, @Data)",
                    changedUsers.Select(u => new Dictionary<string, object>() { { "@ID", u.ID.ToString() }, { "@Data", JSONSerializerHelper.SerializeToString(u) } }));

                List<Guid> removedCommands = new List<Guid>();
                removedCommands.AddRange(this.ChatCommands.GetRemovedValues().Select(c => c.ID));
                removedCommands.AddRange(this.EventCommands.GetRemovedValues().Select(c => c.ID));
                removedCommands.AddRange(this.TimerCommands.GetRemovedValues().Select(c => c.ID));
                removedCommands.AddRange(this.ActionGroupCommands.GetRemovedValues().Select(c => c.ID));
                removedCommands.AddRange(this.GameCommands.GetRemovedValues().Select(c => c.ID));
                removedCommands.AddRange(this.TwitchChannelPointsCommands.GetRemovedValues().Select(c => c.ID));
                removedCommands.AddRange(this.CustomCommands.GetRemovedValues());
                await ServiceManager.Get<IDatabaseService>().BulkWrite(this.DatabaseFilePath, "DELETE FROM Commands WHERE ID = @ID",
                    removedCommands.Select(id => new Dictionary<string, object>() { { "@ID", id.ToString() } }));

                List<CommandBase> addedChangedCommands = new List<CommandBase>();
                addedChangedCommands.AddRange(this.ChatCommands.GetAddedChangedValues());
                addedChangedCommands.AddRange(this.EventCommands.GetAddedChangedValues());
                addedChangedCommands.AddRange(this.TimerCommands.GetAddedChangedValues());
                addedChangedCommands.AddRange(this.ActionGroupCommands.GetAddedChangedValues());
                addedChangedCommands.AddRange(this.GameCommands.GetAddedChangedValues());
                addedChangedCommands.AddRange(this.TwitchChannelPointsCommands.GetAddedChangedValues());
                addedChangedCommands.AddRange(this.CustomCommands.GetAddedChangedValues());
                await ServiceManager.Get<IDatabaseService>().BulkWrite(this.DatabaseFilePath, "REPLACE INTO Commands(ID, TypeID, Data) VALUES(@ID, @TypeID, @Data)",
                    addedChangedCommands.Select(c => new Dictionary<string, object>() { { "@ID", c.ID.ToString() }, { "@TypeID", (int)c.Type }, { "@Data", JSONSerializerHelper.SerializeToString(c) } }));

                await ServiceManager.Get<IDatabaseService>().BulkWrite(this.DatabaseFilePath, "DELETE FROM Quotes WHERE ID = @ID",
                    this.Quotes.GetRemovedValues().Select(q => new Dictionary<string, object>() { { "@ID", q.ID.ToString() } }));

                await ServiceManager.Get<IDatabaseService>().BulkWrite(this.DatabaseFilePath, "REPLACE INTO Quotes(ID, Data) VALUES(@ID, @Data)",
                    this.Quotes.GetAddedChangedValues().Select(q => new Dictionary<string, object>() { { "@ID", q.ID.ToString() }, { "@Data", JSONSerializerHelper.SerializeToString(q.Model) } }));
            }
        }

        public UserDataModel GetUserData(Guid id)
        {
            lock (this.UserData)
            {
                if (this.UserData.ContainsKey(id))
                {
                    return this.UserData[id];
                }
                return null;
            }
        }

        public UserDataModel GetUserDataByTwitchID(string twitchID)
        {
            lock (this.UserData)
            {
                if (!string.IsNullOrEmpty(twitchID) && this.TwitchUserIDLookups.ContainsKey(twitchID))
                {
                    Guid id = this.TwitchUserIDLookups[twitchID];
                    if (this.UserData.ContainsKey(id))
                    {
                        return this.UserData[id];
                    }
                }
                return null;
            }
        }

        public UserDataModel GetUserDataByUsername(StreamingPlatformTypeEnum platform, string username)
        {
            if (!string.IsNullOrEmpty(username))
            {
                if (platform == StreamingPlatformTypeEnum.All)
                {
                    foreach (StreamingPlatformTypeEnum p in StreamingPlatforms.Platforms)
                    {
                        UserDataModel userData = this.GetUserDataByUsername(p, username);
                        if (userData != null)
                        {
                            return userData;
                        }
                    }
                }
                else
                {
                    lock (this.UserData)
                    {
                        if (this.UsernameLookups.ContainsKey(platform) && this.UsernameLookups[platform].ContainsKey(username.ToLowerInvariant()))
                        {
                            Guid id = this.UsernameLookups[platform][username.ToLowerInvariant()];
                            if (this.UserData.ContainsKey(id))
                            {
                                return this.UserData[id];
                            }
                        }
                    }
                }
            }
            return null;
        }

        public void AddUserData(UserDataModel user)
        {
            this.UserData[user.ID] = user;
            if (!string.IsNullOrEmpty(user.TwitchID))
            {
                this.TwitchUserIDLookups[user.TwitchID] = user.ID;
            }
            this.UserData.ManualValueChanged(user.ID);
        }

        public CustomCommand GetCustomCommand(Guid id) { return this.CustomCommands.ContainsKey(id) ? this.CustomCommands[id] : null; }

        public void SetCustomCommand(CustomCommand command)
        {
            if (command != null)
            {
                this.CustomCommands[command.ID] = command;
            }
        }

        private void InitializeMissingData()
        {
            this.GameQueueUserJoinedCommand = this.GameQueueUserJoinedCommand ?? CustomCommand.BasicChatCommand("Game Queue Used Joined", "You are #$queueposition in the queue to play.");
            this.GameQueueUserSelectedCommand = this.GameQueueUserSelectedCommand ?? CustomCommand.BasicChatCommand("Game Queue Used Selected", "It's time to play @$username! Listen carefully for instructions on how to join...");

            this.GiveawayStartedReminderCommand = this.GiveawayStartedReminderCommand ?? CustomCommand.BasicChatCommand("Giveaway Started/Reminder", "A giveaway has started for $giveawayitem! Type $giveawaycommand in chat in the next $giveawaytimelimit minute(s) to enter!");
            this.GiveawayUserJoinedCommand = this.GiveawayUserJoinedCommand ?? CustomCommand.BasicChatCommand("Giveaway User Joined");
            this.GiveawayWinnerSelectedCommand = this.GiveawayWinnerSelectedCommand ?? CustomCommand.BasicChatCommand("Giveaway Winner Selected", "Congratulations @$username, you won $giveawayitem!");

            this.ModerationStrike1Command = this.ModerationStrike1Command ?? CustomCommand.BasicChatCommand("Moderation Strike 1", "$moderationreason. You have received a moderation strike & currently have $usermoderationstrikes strike(s)", isWhisper: true);
            this.ModerationStrike2Command = this.ModerationStrike2Command ?? CustomCommand.BasicChatCommand("Moderation Strike 2", "$moderationreason. You have received a moderation strike & currently have $usermoderationstrikes strike(s)", isWhisper: true);
            this.ModerationStrike3Command = this.ModerationStrike3Command ?? CustomCommand.BasicChatCommand("Moderation Strike 3", "$moderationreason. You have received a moderation strike & currently have $usermoderationstrikes strike(s)", isWhisper: true);

            if (this.DashboardItems.Count < 4)
            {
                this.DashboardItems = new List<DashboardItemTypeEnum>() { DashboardItemTypeEnum.None, DashboardItemTypeEnum.None, DashboardItemTypeEnum.None, DashboardItemTypeEnum.None };
            }
            if (this.DashboardQuickCommands.Count < 5)
            {
                this.DashboardQuickCommands = new List<Guid>() { Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty };
            }

            if (this.GetCustomCommand(this.RedemptionStoreManualRedeemNeededCommandID) == null)
            {
                CustomCommand command = CustomCommand.BasicChatCommand(MixItUp.Base.Resources.RedemptionStoreManualRedeemNeededCommandName, "@$username just purchased $productname and needs to be manually redeemed");
                this.RedemptionStoreManualRedeemNeededCommandID = command.ID;
                this.SetCustomCommand(command);
            }
            if (this.GetCustomCommand(this.RedemptionStoreDefaultRedemptionCommandID) == null)
            {
                CustomCommand command = CustomCommand.BasicChatCommand(MixItUp.Base.Resources.RedemptionStoreDefaultRedemptionCommandName, "@$username just redeemed $productname");
                this.RedemptionStoreDefaultRedemptionCommandID = command.ID;
                this.SetCustomCommand(command);
            }
        }

        public Version GetLatestVersion() { return Assembly.GetEntryAssembly().GetName().Version; }
    }
}