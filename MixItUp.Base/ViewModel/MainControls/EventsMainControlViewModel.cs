﻿using MixItUp.Base.Model.Commands;
using MixItUp.Base.Services;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel;
using System.Collections.ObjectModel;

namespace MixItUp.Base.ViewModel.MainControls
{
    public class EventCommandItemViewModel
    {
        public EventTypeEnum EventType { get; set; }

        public EventCommandModel Command { get; set; }

        public EventCommandItemViewModel(EventCommandModel command)
        {
            this.Command = command;
            this.EventType = this.Command.EventType;
        }

        public EventCommandItemViewModel(EventTypeEnum eventType) { this.EventType = eventType; }

        public string Name { get { return EnumLocalizationHelper.GetLocalizedName(this.EventType); } }

        public string Service
        {
            get
            {
                int eventNumber = (int)this.EventType;
                if (this.EventType == EventTypeEnum.StreamlabsDonation)
                {
                    return "Streamlabs";
                }
                else if (this.EventType == EventTypeEnum.TiltifyDonation)
                {
                    return "Tiltify";
                }
                else if (this.EventType == EventTypeEnum.ExtraLifeDonation)
                {
                    return "Extra Life";
                }
                else if (this.EventType == EventTypeEnum.TipeeeStreamDonation)
                {
                    return "TipeeeStream";
                }
                else if (this.EventType == EventTypeEnum.TreatStreamDonation)
                {
                    return "TreatStream";
                }
                else if (this.EventType == EventTypeEnum.StreamJarDonation)
                {
                    return "StreamJar";
                }
                else if (this.EventType == EventTypeEnum.PatreonSubscribed)
                {
                    return "Patreon";
                }
                else if (this.EventType == EventTypeEnum.JustGivingDonation)
                {
                    return "JustGiving";
                }
                else if (this.EventType == EventTypeEnum.StreamlootsCardRedeemed || this.EventType == EventTypeEnum.StreamlootsPackGifted || this.EventType == EventTypeEnum.StreamlootsPackPurchased)
                {
                    return "Streamloots";
                }
                else if (this.EventType == EventTypeEnum.StreamElementsDonation)
                {
                    return "StreamElements";
                }
                else if (eventNumber >= 100 && eventNumber < 200)
                {
                    return "Mixer";
                }
                else if (eventNumber >= 200 && eventNumber < 300)
                {
                    return "Twitch";
                }
                else
                {
                    return "Generic";
                }
            }
        }

        public bool IsNewCommand { get { return this.Command == null; } }

        public bool IsExistingCommand { get { return this.Command != null; } }
    }

    public class EventsMainControlViewModel : WindowControlViewModelBase
    {
        public ObservableCollection<EventCommandItemViewModel> EventCommands { get; set; } = new ObservableCollection<EventCommandItemViewModel>();

        public EventsMainControlViewModel(MainWindowViewModel windowViewModel)
            : base(windowViewModel)
        {
            this.RefreshCommands();
        }

        public void RefreshCommands()
        {
            this.EventCommands.Clear();

            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.TwitchChannelStreamStart));
            //this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.TwitchChannelStreamStop));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.TwitchChannelFollowed));
            //this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.TwitchChannelUnfollowed));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.TwitchChannelHosted));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.TwitchChannelRaided));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.TwitchChannelSubscribed));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.TwitchChannelResubscribed));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.TwitchChannelSubscriptionGifted));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.TwitchChannelMassSubscriptionsGifted));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.TwitchChannelBitsCheered));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.TwitchChannelPointsRedeemed));

            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.GlimeshChannelStreamStart));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.GlimeshChannelStreamStop));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.GlimeshChannelFollowed));

            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.ChatUserFirstJoin));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.ChatUserJoined));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.ChatUserLeft));
            //this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.ChatUserPurge));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.ChatUserTimeout));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.ChatUserBan));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.ChatMessageReceived));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.ChatWhisperReceived));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.ChatMessageDeleted));

            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.StreamlabsDonation));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.StreamElementsDonation));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.TipeeeStreamDonation));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.TreatStreamDonation));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.StreamJarDonation));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.TiltifyDonation));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.ExtraLifeDonation));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.JustGivingDonation));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.PatreonSubscribed));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.StreamlootsCardRedeemed));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.StreamlootsPackPurchased));
            this.EventCommands.Add(this.GetEventCommand(EventTypeEnum.StreamlootsPackGifted));
        }

        private EventCommandItemViewModel GetEventCommand(EventTypeEnum eventType)
        {
            EventCommandModel command = ServiceManager.Get<EventService>().GetEventCommand(eventType);
            if (command != null)
            {
                return new EventCommandItemViewModel(command);
            }
            else
            {
                return new EventCommandItemViewModel(eventType);
            }
        }
    }
}
