﻿using Mixer.Base.Model.Channel;
using MixItUp.Base.Commands;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base
{
    [DataContract]
    public class ChannelSettings
    {
        private const string SettingsDirectoryName = "Settings";

        public static async Task<ChannelSettings> LoadSettings(ChannelModel channel)
        {
            string filePath = ChannelSettings.GetSettingsFilePath(channel);
            if (File.Exists(filePath))
            {
                ChannelSettings settings = await SerializerHelper.DeserializeFromFile<ChannelSettings>(filePath);

                settings.Channel = channel;
                settings.ChatCommands = new LockedList<ChatCommand>(settings.chatCommandsInternal);
                settings.SubscribedEvents = new LockedList<SubscribedEventViewModel>(settings.subscribedEventsInternal);
                settings.InteractiveControls = new LockedList<InteractiveControlViewModel>(settings.interactiveControlsInternal);
                settings.TimerCommands = new LockedList<TimerCommand>(settings.timerCommandsInternal);

                return settings;
            }
            else
            {
                return new ChannelSettings(channel);
            }
        }

        private static string GetSettingsFilePath(ChannelModel channel) { return Path.Combine(SettingsDirectoryName, string.Format("{0}.xml", channel.id.ToString())); }

        [JsonProperty]
        private List<ChatCommand> chatCommandsInternal { get; set; }

        [JsonProperty]
        private List<SubscribedEventViewModel> subscribedEventsInternal { get; set; }

        [JsonProperty]
        private List<InteractiveControlViewModel> interactiveControlsInternal { get; set; }

        [JsonProperty]
        private List<TimerCommand> timerCommandsInternal { get; set; }

        [JsonProperty]
        public ChannelModel Channel { get; set; }

        [JsonProperty]
        public List<UserDataViewModel> UserData { get; set; }

        [JsonIgnore]
        public LockedList<ChatCommand> ChatCommands { get; set; }

        [JsonIgnore]
        public LockedList<SubscribedEventViewModel> SubscribedEvents { get; set; }

        [JsonIgnore]
        public LockedList<InteractiveControlViewModel> InteractiveControls { get; set; }

        [JsonIgnore]
        public LockedList<TimerCommand> TimerCommands { get; set; }

        public ChannelSettings(ChannelModel channel) : this() { this.Channel = channel; }

        public ChannelSettings()
        {
            this.chatCommandsInternal = new List<ChatCommand>();
            this.subscribedEventsInternal = new List<SubscribedEventViewModel>();
            this.interactiveControlsInternal = new List<InteractiveControlViewModel>();
            this.timerCommandsInternal = new List<TimerCommand>();

            this.UserData = new List<UserDataViewModel>();

            this.ChatCommands = new LockedList<ChatCommand>();
            this.SubscribedEvents = new LockedList<SubscribedEventViewModel>();
            this.InteractiveControls = new LockedList<InteractiveControlViewModel>();
            this.TimerCommands = new LockedList<TimerCommand>();
        }

        public async Task SaveSettings()
        {
            Directory.CreateDirectory(SettingsDirectoryName);
            string filePath = ChannelSettings.GetSettingsFilePath(this.Channel);

            this.chatCommandsInternal = this.ChatCommands.ToList();
            this.subscribedEventsInternal = this.SubscribedEvents.ToList();
            this.interactiveControlsInternal = this.InteractiveControls.ToList();
            this.timerCommandsInternal = this.TimerCommands.ToList();

            await SerializerHelper.SerializeToFile(filePath, this);
        }
    }
}
