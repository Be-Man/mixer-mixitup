﻿using MixItUp.Base.Model.Commands;
using MixItUp.Base.Model.User;
using MixItUp.Base.Services;
using MixItUp.Base.Services.Twitch;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.Chat;
using MixItUp.Base.ViewModel.User;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Overlay
{
    public enum OverlayEndCreditsSectionTypeEnum
    {
        Chatters,
        [Name("NewFollowers")]
        Followers,
        Hosts,
        NewSubscribers,
        Resubscribers,
        GiftedSubs,
        Donations,
        [Obsolete]
        Sparks,
        [Obsolete]
        Embers,
        Subscribers,
        Moderators,
        FreeFormHTML,
        FreeFormHTML2,
        FreeFormHTML3,
        Bits,
        Raids,
    }

    public enum OverlayEndCreditsSpeedEnum
    {
        Fast = 10,
        Medium = 20,
        Slow = 30
    }

    [DataContract]
    public class OverlayEndCreditsSectionModel
    {
        [DataMember]
        public OverlayEndCreditsSectionTypeEnum SectionType { get; set; }
        [DataMember]
        public string SectionHTML { get; set; }
        [DataMember]
        public string UserHTML { get; set; }
    }

    [DataContract]
    public class OverlayEndCreditsItemModel : OverlayHTMLTemplateItemModelBase
    {
        public const string CreditsWrapperHTML = @"<div id=""titles"" style=""background-color: {0}""><div id=""credits"">{1}</div></div>";

        public const string TitleHTMLTemplate = @"<div id=""the-end"">Stream Credits</div>";

        public const string SectionHTMLTemplate = @"<h1 style=""font-family: '{TEXT_FONT}'; font-size: {TEXT_SIZE}px; color: {TEXT_COLOR};"">{NAME}</h1>";
        public const string FreeFormSectionHTMLTemplate = @"<h1 style=""font-family: '{TEXT_FONT}'; font-size: {TEXT_SIZE}px; color: {TEXT_COLOR};"">FREE FORM</h1>";

        public const string UserHTMLTemplate = @"<p style=""font-family: '{TEXT_FONT}'; font-size: {TEXT_SIZE}px; color: {TEXT_COLOR};"">{NAME}</p>";
        public const string UserDetailsHTMLTemplate = @"<dt style=""font-family: '{TEXT_FONT}'; font-size: {TEXT_SIZE}px; color: {TEXT_COLOR};"">{NAME}</dt><dd style=""font-family: '{TEXT_FONT}'; font-size: {TEXT_SIZE}px; color: {TEXT_COLOR};"">{DETAILS}</dd>";
        public const string FreeFormUserHTMLTemplate = @"<p style=""font-family: '{TEXT_FONT}'; font-size: {TEXT_SIZE}px; color: {TEXT_COLOR};"">FREE FORM</p>";

        public const string SectionSeparatorHTML = @"<div class=""clearfix""></div>";

        [DataMember]
        public string TitleTemplate { get; set; }
        [DataMember]
        public Dictionary<OverlayEndCreditsSectionTypeEnum, OverlayEndCreditsSectionModel> SectionTemplates { get; set; } = new Dictionary<OverlayEndCreditsSectionTypeEnum, OverlayEndCreditsSectionModel>();

        [DataMember]
        public string BackgroundColor { get; set; }

        [DataMember]
        public string SectionTextColor { get; set; }
        [DataMember]
        public string SectionTextFont { get; set; }
        [DataMember]
        public int SectionTextSize { get; set; }

        [DataMember]
        public string ItemTextColor { get; set; }
        [DataMember]
        public string ItemTextFont { get; set; }
        [DataMember]
        public int ItemTextSize { get; set; }

        [DataMember]
        public OverlayEndCreditsSpeedEnum Speed { get; set; }
        [DataMember]
        public int SpeedNumber { get { return (int)this.Speed; } }

        [JsonIgnore]
        private HashSet<Guid> viewers = new HashSet<Guid>();
        [JsonIgnore]
        private HashSet<Guid> subs = new HashSet<Guid>();
        [JsonIgnore]
        private HashSet<Guid> mods = new HashSet<Guid>();
        [JsonIgnore]
        private HashSet<Guid> follows = new HashSet<Guid>();
        [JsonIgnore]
        private HashSet<Guid> hosts = new HashSet<Guid>();
        [JsonIgnore]
        private Dictionary<Guid, uint> raids = new Dictionary<Guid, uint>();
        [JsonIgnore]
        private HashSet<Guid> newSubs = new HashSet<Guid>();
        [JsonIgnore]
        private Dictionary<Guid, uint> resubs = new Dictionary<Guid, uint>();
        [JsonIgnore]
        private Dictionary<Guid, uint> giftedSubs = new Dictionary<Guid, uint>();
        [JsonIgnore]
        private Dictionary<Guid, double> donations = new Dictionary<Guid, double>();
        [JsonIgnore]
        private Dictionary<Guid, uint> bits = new Dictionary<Guid, uint>();

        public OverlayEndCreditsItemModel() : base() { }

        public OverlayEndCreditsItemModel(string titleTemplate, Dictionary<OverlayEndCreditsSectionTypeEnum, OverlayEndCreditsSectionModel> sectionTemplates, string backgroundColor,
            string sectionTextColor, string sectionTextFont, int sectionTextSize, string itemTextColor, string itemTextFont, int itemTextSize,
            OverlayEndCreditsSpeedEnum speed)
            : base(OverlayItemModelTypeEnum.EndCredits, string.Empty)
        {
            this.TitleTemplate = titleTemplate;
            this.SectionTemplates = sectionTemplates;
            this.BackgroundColor = backgroundColor;
            this.SectionTextColor = sectionTextColor;
            this.SectionTextFont = sectionTextFont;
            this.SectionTextSize = sectionTextSize;
            this.ItemTextColor = itemTextColor;
            this.ItemTextFont = itemTextFont;
            this.ItemTextSize = itemTextSize;
            this.Speed = speed;
        }

        [JsonIgnore]
        public override bool SupportsTestData { get { return true; } }

        public override Task LoadTestData()
        {
            UserViewModel user = ChannelSession.GetCurrentUser();
            List<Guid> userIDs = new List<Guid>(ChannelSession.Settings.UserData.Keys.Take(20));
            for (int i = userIDs.Count; i < 20; i++)
            {
                userIDs.Add(user.ID);
            }

            foreach (Guid userID in userIDs)
            {
                if (this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.Chatters))
                {
                    this.viewers.Add(userID);
                }
                if (this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.Subscribers))
                {
                    this.subs.Add(userID);
                }
                if (this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.Moderators))
                {
                    this.mods.Add(userID);
                }
                if (this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.Followers))
                {
                    this.follows.Add(userID);
                }
                if (this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.Hosts))
                {
                    this.hosts.Add(userID);
                }
                if (this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.Raids))
                {
                    this.raids[userID] = 10;
                }
                if (this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.NewSubscribers))
                {
                    this.newSubs.Add(userID);
                }
                if (this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.Resubscribers))
                {
                    this.resubs[userID] = 5;
                }
                if (this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.GiftedSubs))
                {
                    this.giftedSubs[userID] = 5;
                }
                if (this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.Donations))
                {
                    this.donations[userID] = 12.34;
                }
                if (this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.Bits))
                {
                    this.bits[userID] = 123;
                }
            }
            return Task.FromResult(0);
        }

        public override Task Initialize()
        {
            if (this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.Chatters) || this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.Subscribers) ||
                this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.Moderators))
            {
                GlobalEvents.OnChatMessageReceived += GlobalEvents_OnChatMessageReceived;
            }
            if (this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.Followers))
            {
                GlobalEvents.OnFollowOccurred += GlobalEvents_OnFollowOccurred;
            }
            if (this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.Hosts))
            {
                GlobalEvents.OnHostOccurred += GlobalEvents_OnHostOccurred;
            }
            if (this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.Raids))
            {
                GlobalEvents.OnRaidOccurred += GlobalEvents_OnRaidOccurred;
            }
            if (this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.NewSubscribers))
            {
                GlobalEvents.OnSubscribeOccurred += GlobalEvents_OnSubscribeOccurred;
            }
            if (this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.Resubscribers))
            {
                GlobalEvents.OnResubscribeOccurred += GlobalEvents_OnResubscribeOccurred;
            }
            if (this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.GiftedSubs))
            {
                GlobalEvents.OnSubscriptionGiftedOccurred += GlobalEvents_OnSubscriptionGiftedOccurred;
            }
            if (this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.Donations))
            {
                GlobalEvents.OnDonationOccurred += GlobalEvents_OnDonationOccurred;
            }
            if (this.SectionTemplates.ContainsKey(OverlayEndCreditsSectionTypeEnum.Bits))
            {
                GlobalEvents.OnBitsOccurred += GlobalEvents_OnBitsOccurred;
            }
            return base.Initialize();
        }

        public override Task Disable()
        {
            GlobalEvents.OnChatMessageReceived -= GlobalEvents_OnChatMessageReceived;
            GlobalEvents.OnFollowOccurred -= GlobalEvents_OnFollowOccurred;
            GlobalEvents.OnHostOccurred -= GlobalEvents_OnHostOccurred;
            GlobalEvents.OnRaidOccurred -= GlobalEvents_OnRaidOccurred;
            GlobalEvents.OnSubscribeOccurred -= GlobalEvents_OnSubscribeOccurred;
            GlobalEvents.OnResubscribeOccurred -= GlobalEvents_OnResubscribeOccurred;
            GlobalEvents.OnSubscriptionGiftedOccurred -= GlobalEvents_OnSubscriptionGiftedOccurred;
            GlobalEvents.OnDonationOccurred -= GlobalEvents_OnDonationOccurred;
            GlobalEvents.OnBitsOccurred -= GlobalEvents_OnBitsOccurred;
            return Task.FromResult(0);
        }

        public override async Task Reset()
        {
            this.viewers.Clear();
            this.subs.Clear();
            this.mods.Clear();
            this.follows.Clear();
            this.hosts.Clear();
            this.raids.Clear();
            this.newSubs.Clear();
            this.resubs.Clear();
            this.giftedSubs.Clear();
            this.donations.Clear();
            this.bits.Clear();

            await base.Reset();
        }

        protected override async Task PerformReplacements(JObject jobj, CommandParametersModel parameters)
        {
            StringBuilder htmlBuilder = new StringBuilder();

            htmlBuilder.AppendLine(SectionSeparatorHTML);
            htmlBuilder.AppendLine(await this.ReplaceStringWithSpecialModifiers(this.TitleTemplate, parameters));

            foreach (var kvp in this.SectionTemplates)
            {
                if (kvp.Key == OverlayEndCreditsSectionTypeEnum.FreeFormHTML || kvp.Key == OverlayEndCreditsSectionTypeEnum.FreeFormHTML2 ||
                    kvp.Key == OverlayEndCreditsSectionTypeEnum.FreeFormHTML3)
                {
                    OverlayEndCreditsSectionModel sectionTemplate = this.SectionTemplates[kvp.Key];

                    string sectionHTML = this.PerformTemplateReplacements(sectionTemplate.SectionHTML, new Dictionary<string, string>());
                    sectionHTML = await this.ReplaceStringWithSpecialModifiers(sectionHTML, parameters);

                    string userHTML = this.PerformTemplateReplacements(sectionTemplate.UserHTML, new Dictionary<string, string>());
                    userHTML = await this.ReplaceStringWithSpecialModifiers(userHTML, parameters);

                    htmlBuilder.AppendLine(SectionSeparatorHTML);
                    htmlBuilder.AppendLine(sectionHTML);
                    htmlBuilder.AppendLine(userHTML);
                }
                else
                {
                    Dictionary<UserViewModel, string> items = new Dictionary<UserViewModel, string>();
                    switch (kvp.Key)
                    {
                        case OverlayEndCreditsSectionTypeEnum.Chatters: items = this.GetUsersDictionary(this.viewers); break;
                        case OverlayEndCreditsSectionTypeEnum.Subscribers: items = this.GetUsersDictionary(this.subs); break;
                        case OverlayEndCreditsSectionTypeEnum.Moderators: items = this.GetUsersDictionary(this.mods); break;
                        case OverlayEndCreditsSectionTypeEnum.Followers: items = this.GetUsersDictionary(this.follows); break;
                        case OverlayEndCreditsSectionTypeEnum.Hosts: items = this.GetUsersDictionary(this.hosts); break;
                        case OverlayEndCreditsSectionTypeEnum.Raids: items = this.GetUsersDictionary(this.raids); break;
                        case OverlayEndCreditsSectionTypeEnum.NewSubscribers: items = this.GetUsersDictionary(this.newSubs); break;
                        case OverlayEndCreditsSectionTypeEnum.Resubscribers: items = this.GetUsersDictionary(this.resubs); break;
                        case OverlayEndCreditsSectionTypeEnum.GiftedSubs: items = this.GetUsersDictionary(this.giftedSubs); break;
                        case OverlayEndCreditsSectionTypeEnum.Donations: items = this.GetUsersDictionary(this.donations); break;
                        case OverlayEndCreditsSectionTypeEnum.Bits: items = this.GetUsersDictionary(this.bits); break;
                    }
                    await this.PerformSectionTemplateReplacement(htmlBuilder, kvp.Key, items, parameters);
                }
            }

            jobj["HTML"] = string.Format(CreditsWrapperHTML, this.BackgroundColor, htmlBuilder.ToString());
        }

        private void GlobalEvents_OnChatMessageReceived(object sender, ChatMessageViewModel message)
        {
            if (message.User != null && !message.User.IgnoreForQueries)
            {
                this.AddUserForRole(message.User);
            }
        }

        private void GlobalEvents_OnFollowOccurred(object sender, UserViewModel user)
        {
            if (!this.follows.Contains(user.ID))
            {
                this.follows.Add(user.ID);
                this.AddUserForRole(user);
            }
        }

        private void GlobalEvents_OnHostOccurred(object sender, UserViewModel host)
        {
            if (!this.hosts.Contains(host.ID))
            {
                this.hosts.Add(host.ID);
                this.AddUserForRole(host);
            }
        }

        private void GlobalEvents_OnRaidOccurred(object sender, Tuple<UserViewModel, int> raid)
        {
            if (!this.raids.ContainsKey(raid.Item1.ID))
            {
                this.raids[raid.Item1.ID] = (uint)raid.Item2;
                this.AddUserForRole(raid.Item1);
            }
        }

        private void GlobalEvents_OnSubscribeOccurred(object sender, UserViewModel user)
        {
            if (!this.newSubs.Contains(user.ID))
            {
                this.newSubs.Add(user.ID);
                this.AddUserForRole(user);
            }
        }

        private void GlobalEvents_OnResubscribeOccurred(object sender, Tuple<UserViewModel, int> resub)
        {
            if (!this.resubs.ContainsKey(resub.Item1.ID))
            {
                this.resubs[resub.Item1.ID] = (uint)resub.Item2;
                this.AddUserForRole(resub.Item1);
            }
        }

        private void GlobalEvents_OnSubscriptionGiftedOccurred(object sender, Tuple<UserViewModel, UserViewModel> subGift)
        {
            if (!this.newSubs.Contains(subGift.Item2.ID))
            {
                this.newSubs.Add(subGift.Item2.ID);
                this.AddUserForRole(subGift.Item2);
            }

            if (!this.giftedSubs.ContainsKey(subGift.Item1.ID))
            {
                this.giftedSubs[subGift.Item1.ID] = 0;
                this.AddUserForRole(subGift.Item1);
            }
            this.giftedSubs[subGift.Item1.ID]++;
        }

        private void GlobalEvents_OnDonationOccurred(object sender, UserDonationModel donation)
        {
            if (!this.donations.ContainsKey(donation.User.ID))
            {
                this.donations[donation.User.ID] = 0;
                this.AddUserForRole(donation.User);
            }
            this.donations[donation.User.ID] += donation.Amount;
        }

        private void GlobalEvents_OnBitsOccurred(object sender, User.Twitch.TwitchUserBitsCheeredModel bits)
        {
            if (!this.bits.ContainsKey(bits.User.ID))
            {
                this.bits[bits.User.ID] = 0;
                this.AddUserForRole(bits.User);
            }
            this.bits[bits.User.ID] += (uint)bits.Amount;
        }

        private void AddUserForRole(UserViewModel user)
        {
            if (this.ShouldIncludeUser(user))
            {
                this.viewers.Add(user.ID);
                if (user.UserRoles.Contains(UserRoleEnum.Subscriber) || user.IsEquivalentToSubscriber())
                {
                    this.subs.Add(user.ID);
                }
                if (user.UserRoles.Contains(UserRoleEnum.Mod) || user.UserRoles.Contains(UserRoleEnum.ChannelEditor))
                {
                    this.mods.Add(user.ID);
                }
            }
        }

        private bool ShouldIncludeUser(UserViewModel user)
        {
            if (user == null)
            {
                return false;
            }

            if (user.ID.Equals(ChannelSession.GetCurrentUser()?.ID))
            {
                return false;
            }

            // TODO
            if (ServiceManager.Get<TwitchSessionService>().BotConnection != null && string.Equals(user.TwitchID, ServiceManager.Get<TwitchSessionService>().BotNewAPI?.id))
            {
                return false;
            }

            return true;
        }

        private Dictionary<UserViewModel, string> GetUsersDictionary(HashSet<Guid> data)
        {
            Dictionary<UserViewModel, string> results = new Dictionary<UserViewModel, string>();
            foreach (Guid userID in data)
            {
                try
                {
                    UserViewModel user = this.GetUser(userID);
                    if (user != null)
                    {
                        results[user] = string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
            return results;
        }

        private Dictionary<UserViewModel, string> GetUsersDictionary(Dictionary<Guid, uint> data)
        {
            Dictionary<UserViewModel, string> results = new Dictionary<UserViewModel, string>();
            foreach (var kvp in data)
            {
                try
                {
                    UserViewModel user = this.GetUser(kvp.Key);
                    if (user != null)
                    {
                        results[user] = kvp.Value.ToString();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
            return results;
        }

        private Dictionary<UserViewModel, string> GetUsersDictionary(Dictionary<Guid, double> data)
        {
            Dictionary<UserViewModel, string> results = new Dictionary<UserViewModel, string>();
            foreach (var kvp in data)
            {
                try
                {
                    UserViewModel user = this.GetUser(kvp.Key);
                    if (user != null)
                    {
                        results[user] = string.Format("{0:C}", Math.Round(kvp.Value, 2));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
            return results;
        }

        private UserViewModel GetUser(Guid userID)
        {
            UserDataModel data = ChannelSession.Settings.GetUserData(userID);
            if (data != null)
            {
                return new UserViewModel(data);
            }
            return null;
        }

        private async Task PerformSectionTemplateReplacement(StringBuilder htmlBuilder, OverlayEndCreditsSectionTypeEnum itemType, Dictionary<UserViewModel, string> replacers, CommandParametersModel parameters)
        {
            if (this.SectionTemplates.ContainsKey(itemType) && replacers.Count > 0)
            {
                OverlayEndCreditsSectionModel sectionTemplate = this.SectionTemplates[itemType];

                string sectionHTML = this.PerformTemplateReplacements(sectionTemplate.SectionHTML, new Dictionary<string, string>()
                {
                    { "NAME", EnumHelper.GetEnumName(itemType) },
                    { "TEXT_FONT", this.SectionTextFont },
                    { "TEXT_SIZE", this.SectionTextSize.ToString() },
                    { "TEXT_COLOR", this.SectionTextColor }
                });
                sectionHTML = await this.ReplaceStringWithSpecialModifiers(sectionHTML, parameters);

                List<string> userHTMLs = new List<string>();
                foreach (var kvp in replacers.OrderBy(kvp => kvp.Key.DisplayName))
                {
                    if (!string.IsNullOrEmpty(kvp.Key.DisplayName))
                    {
                        string userHTML = this.PerformTemplateReplacements(sectionTemplate.UserHTML, new Dictionary<string, string>()
                        {
                            { "NAME", kvp.Key.DisplayName },
                            { "DETAILS", kvp.Value },
                            { "TEXT_FONT", this.ItemTextFont },
                            { "TEXT_SIZE", this.ItemTextSize.ToString() },
                            { "TEXT_COLOR", this.ItemTextColor }
                        });
                        userHTML = await this.ReplaceStringWithSpecialModifiers(userHTML, parameters);
                        userHTMLs.Add(userHTML);
                    }
                }

                htmlBuilder.AppendLine(SectionSeparatorHTML);
                htmlBuilder.AppendLine(sectionHTML);
                foreach (string userHTML in userHTMLs)
                {
                    htmlBuilder.AppendLine(userHTML);
                }
            }
        }
    }
}
