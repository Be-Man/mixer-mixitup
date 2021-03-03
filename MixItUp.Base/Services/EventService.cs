﻿using MixItUp.Base.Model;
using MixItUp.Base.Model.Commands;
using MixItUp.Base.Model.Currency;
using MixItUp.Base.Model.User;
using MixItUp.Base.Services.Twitch;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.Chat;
using MixItUp.Base.ViewModel.User;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MixItUp.Base.Services
{
    public enum EventTypeEnum
    {
        None = 0,

        // Platform-agnostic = 1

        //[Name("Channel Stream Start")]
        //ChannelStreamStart = 1,
        //[Name("Channel Stream Stop")]
        //ChannelStreamStop = 2,
        //[Name("Channel Hosted")]
        //ChannelHosted = 3,

        //[Name("Channel Followed")]
        //ChannelFollowed = 10,
        //[Name("Channel Unfollowed")]
        //ChannelUnfollowed = 11,

        //[Name("Channel Subscribed")]
        //ChannelSubscribed = 20,
        //[Name("Channel Resubscribed")]
        //ChannelResubscribed = 21,
        //[Name("Channel Subscription Gifted")]
        //ChannelSubscriptionGifted = 22,

        [Name("Chat New User Joined")]
        ChatUserFirstJoin = 50,
        [Name("Chat User Purged")]
        ChatUserPurge = 51,
        [Name("Chat User Banned")]
        ChatUserBan = 52,
        [Name("Chat Message Received")]
        ChatMessageReceived = 53,
        [Name("Chat User Joined")]
        ChatUserJoined = 54,
        [Name("Chat User Left")]
        ChatUserLeft = 55,
        [Name("Chat Message Deleted")]
        ChatMessageDeleted = 56,
        [Name("Chat User Timeout")]
        ChatUserTimeout = 57,
        [Name("Chat Whisper Received")]
        ChatWhisperReceived = 58,

        // Mixer = 100

        [Obsolete]
        [Name("Mixer Channel Stream Start")]
        MixerChannelStreamStart = 100,
        [Obsolete]
        [Name("Mixer Channel Stream Stop")]
        MixerChannelStreamStop = 101,
        [Obsolete]
        [Name("Mixer Channel Hosted")]
        MixerChannelHosted = 102,

        [Obsolete]
        [Name("Mixer Channel Followed")]
        MixerChannelFollowed = 110,
        [Obsolete]
        [Name("Mixer Channel Unfollowed")]
        MixerChannelUnfollowed = 111,

        [Obsolete]
        [Name("Mixer Channel Subscribed")]
        MixerChannelSubscribed = 120,
        [Obsolete]
        [Name("Mixer Channel Resubscribed")]
        MixerChannelResubscribed = 121,
        [Obsolete]
        [Name("Mixer Channel Subscription Gifted")]
        MixerChannelSubscriptionGifted = 122,

        //[Name("Mixer Chat New User Joined")]
        //MixerChatUserFirstJoin = 150,
        //[Name("Mixer Chat User Purged")]
        //MixerChatUserPurge = 151,
        //[Name("Mixer Chat User Banned")]
        //MixerChatUserBan = 152,
        //[Name("Mixer Chat Message Received")]
        //MixerChatMessageReceived = 153,
        //[Name("Mixer Chat User Joined")]
        //MixerChatUserJoined = 154,
        //[Name("Mixer Chat User Left")]
        //MixerChatUserLeft = 155,
        //[Name("Mixer Chat Message Deleted")]
        //MixerChatMessageDeleted = 156,
        //[Name("Mixer Chat User Timeout")]
        //MixerChatUserTimeout = 156,

        [Obsolete]
        [Name("Mixer Channel Sparks Spent")]
        MixerChannelSparksUsed = 170,
        [Obsolete]
        [Name("Mixer Channel Embers Spent")]
        MixerChannelEmbersUsed = 171,
        [Obsolete]
        [Name("Mixer Channel Skill Used")]
        MixerChannelSkillUsed = 172,
        [Obsolete]
        [Name("Mixer Channel Milestone Reached")]
        MixerChannelMilestoneReached = 173,
        [Obsolete]
        [Name("Mixer Channel Fan Progression Level-Up")]
        MixerChannelFanProgressionLevelUp = 174,

        // Twitch = 200

        [Name("Twitch Channel Stream Start (1 Minute Delay)")]
        TwitchChannelStreamStart = 200,
        [Name("Twitch Channel Stream Stop")]
        TwitchChannelStreamStop = 201,
        [Name("Twitch Channel Hosted")]
        TwitchChannelHosted = 202,
        [Name("Twitch Channel Raided")]
        TwitchChannelRaided = 203,

        [Name("Twitch Channel Followed (1 Minute Delay)")]
        TwitchChannelFollowed = 210,
        [Name("Twitch Channel Unfollowed")]
        TwitchChannelUnfollowed = 211,

        [Name("Twitch Channel Subscribed")]
        TwitchChannelSubscribed = 220,
        [Name("Twitch Channel Resubscribed")]
        TwitchChannelResubscribed = 221,
        [Name("Twitch Channel Subscription Gifted")]
        TwitchChannelSubscriptionGifted = 222,
        [Name("Twitch Channel Mass Subscriptions Gifted")]
        TwitchChannelMassSubscriptionsGifted = 223,

        //[Name("Twitch Chat New User Joined")]
        //TwitchChatUserFirstJoin = 250,
        //[Name("Twitch Chat User Purged")]
        //TwitchChatUserPurge = 251,
        //[Name("Twitch Chat User Banned")]
        //TwitchChatUserBan = 252,
        //[Name("Twitch Chat Message Received")]
        //TwitchChatMessageReceived = 253,
        //[Name("Twitch Chat User Joined")]
        //TwitchChatUserJoined = 254,
        //[Name("Twitch Chat User Left")]
        //TwitchChatUserLeft = 255,
        //[Name("Twitch Chat Message Deleted")]
        //TwitchChatMessageDeleted = 256,

        [Name("Twitch Channel Bits Cheered")]
        TwitchChannelBitsCheered = 270,
        [Name("Twitch Channel Points Redeemed")]
        TwitchChannelPointsRedeemed = 271,

        // 300 = YouTube

        // 400 = Trovo

        // 500 = Glimesh

        [Name("Glimesh Channel Stream Start")]
        GlimeshChannelStreamStart = 500,
        [Name("Glimesh Channel Stream Stop")]
        GlimeshChannelStreamStop = 501,

        [Name("Glimesh Channel Followed")]
        GlimeshChannelFollowed = 510,

        // External Services = 1000

        [Name("Streamlabs Donation")]
        StreamlabsDonation = 1000,
        [Name("Tiltify Donation (1 Minute Delay)")]
        TiltifyDonation = 1020,
        [Name("Extra Life Donation (1 Minute Delay)")]
        ExtraLifeDonation = 1030,
        [Name("TipeeeStream Donation")]
        TipeeeStreamDonation = 1040,
        [Name("TreatStream Donation")]
        TreatStreamDonation = 1050,
        [Name("Patreon Subscribed (1 Minute Delay)")]
        PatreonSubscribed = 1060,
        [Name("StreamJar Donation (1 Minute Delay)")]
        StreamJarDonation = 1070,
        [Name("JustGiving Donation (1 Minute Delay)")]
        JustGivingDonation = 1080,
        [Name("Streamloots Card Redeemed")]
        StreamlootsCardRedeemed = 1090,
        [Name("Streamloots Pack Purchased")]
        StreamlootsPackPurchased = 1091,
        [Name("Streamloots Pack Gifted")]
        StreamlootsPackGifted = 1092,
        [Name("StreamElements Donation (1 Minute Delay)")]
        StreamElementsDonation = 1100,
    }

    public class EventTrigger
    {
        public EventTypeEnum Type { get; set; }
        public StreamingPlatformTypeEnum Platform { get; set; } = StreamingPlatformTypeEnum.None;
        public UserViewModel User { get; set; }
        public List<string> Arguments { get; set; } = new List<string>();
        public Dictionary<string, string> SpecialIdentifiers { get; set; } = new Dictionary<string, string>();
        public UserViewModel TargetUser { get; set; }

        public EventTrigger(EventTypeEnum type)
        {
            this.Type = type;
        }

        public EventTrigger(EventTypeEnum type, UserViewModel user)
            : this(type)
        {
            this.User = user;
            if (this.User != null)
            {
                this.Platform = this.User.Platform;
            }
            else
            {
                this.Platform = StreamingPlatformTypeEnum.All;
            }
        }

        public EventTrigger(EventTypeEnum type, UserViewModel user, Dictionary<string, string> specialIdentifiers)
            : this(type, user)
        {
            this.SpecialIdentifiers = specialIdentifiers;
        }
    }

    public interface IEventService
    {
        Task Initialize();

        EventCommandModel GetEventCommand(EventTypeEnum type);

        bool CanPerformEvent(EventTrigger trigger);

        Task PerformEvent(EventTrigger trigger);
    }

    public class EventService : IEventService
    {
        private static HashSet<EventTypeEnum> singleUseTracking = new HashSet<EventTypeEnum>()
        {
            EventTypeEnum.ChatUserFirstJoin, EventTypeEnum.ChatUserJoined, EventTypeEnum.ChatUserLeft,

            EventTypeEnum.TwitchChannelStreamStart, EventTypeEnum.TwitchChannelStreamStop, EventTypeEnum.TwitchChannelFollowed, EventTypeEnum.TwitchChannelUnfollowed, EventTypeEnum.TwitchChannelHosted, EventTypeEnum.TwitchChannelRaided, EventTypeEnum.TwitchChannelSubscribed, EventTypeEnum.TwitchChannelResubscribed,

            EventTypeEnum.GlimeshChannelStreamStart, EventTypeEnum.GlimeshChannelStreamStop, EventTypeEnum.GlimeshChannelFollowed,
        };

        private LockedDictionary<EventTypeEnum, HashSet<Guid>> userEventTracking = new LockedDictionary<EventTypeEnum, HashSet<Guid>>();

        public EventService()
        {
            foreach (EventTypeEnum type in singleUseTracking)
            {
                this.userEventTracking[type] = new HashSet<Guid>();
            }
        }

        public static async Task ProcessDonationEvent(EventTypeEnum type, UserDonationModel donation, Dictionary<string, string> additionalSpecialIdentifiers = null)
        {
            EventTrigger trigger = new EventTrigger(type, donation.User);
            trigger.User.Data.TotalAmountDonated += donation.Amount;

            ChannelSession.Settings.LatestSpecialIdentifiersData[SpecialIdentifierStringBuilder.LatestDonationUserData] = trigger.User.ID;
            ChannelSession.Settings.LatestSpecialIdentifiersData[SpecialIdentifierStringBuilder.LatestDonationAmountData] = donation.AmountText;

            trigger.SpecialIdentifiers = donation.GetSpecialIdentifiers();
            if (additionalSpecialIdentifiers != null)
            {
                foreach (var kvp in additionalSpecialIdentifiers)
                {
                    trigger.SpecialIdentifiers[kvp.Key] = kvp.Value;
                }
            }

            await ServiceManager.Get<EventService>().PerformEvent(trigger);

            foreach (StreamPassModel streamPass in ChannelSession.Settings.StreamPass.Values)
            {
                if (trigger.User.HasPermissionsTo(streamPass.Permission))
                {
                    streamPass.AddAmount(donation.User.Data, (int)Math.Ceiling(streamPass.DonationBonus * donation.Amount));
                }
            }

            await ServiceManager.Get<AlertsService>().AddAlert(new AlertChatMessageViewModel(StreamingPlatformTypeEnum.All, trigger.User, string.Format("{0} Donated {1}", trigger.User.DisplayName, donation.AmountText), ChannelSession.Settings.AlertDonationColor));

            try
            {
                GlobalEvents.DonationOccurred(donation);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public Task Initialize()
        {
            return Task.FromResult(0);
        }

        public EventCommandModel GetEventCommand(EventTypeEnum type)
        {
            foreach (EventCommandModel command in ChannelSession.EventCommands)
            {
                if (command.EventType == type)
                {
                    return command;
                }
            }
            return null;
        }

        public bool CanPerformEvent(EventTrigger trigger)
        {
            UserViewModel user = (trigger.User != null) ? trigger.User : ChannelSession.GetCurrentUser();
            if (EventService.singleUseTracking.Contains(trigger.Type) && this.userEventTracking.ContainsKey(trigger.Type))
            {
                return !this.userEventTracking[trigger.Type].Contains(user.ID);
            }
            return true;
        }

        public async Task PerformEvent(EventTrigger trigger)
        {
            if (this.CanPerformEvent(trigger))
            {
                UserViewModel user = trigger.User;
                if (user == null)
                {
                    user = ChannelSession.GetCurrentUser();
                }

                if (this.userEventTracking.ContainsKey(trigger.Type))
                {
                    lock (this.userEventTracking)
                    {
                        this.userEventTracking[trigger.Type].Add(user.ID);
                    }
                }

                EventCommandModel command = this.GetEventCommand(trigger.Type);
                if (command != null)
                {
                    Logger.Log(LogLevel.Debug, $"Performing event trigger: {trigger.Type}");

                    await command.Perform(new CommandParametersModel(user, platform: trigger.Platform, arguments: trigger.Arguments, specialIdentifiers: trigger.SpecialIdentifiers) { TargetUser = trigger.TargetUser });
                }
            }
        }
    }
}
