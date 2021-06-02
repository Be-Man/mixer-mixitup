﻿using MixItUp.Base.Model;
using MixItUp.Base.Model.Commands;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.User;
using Newtonsoft.Json;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Actions
{
    [Obsolete]
    public enum ActionTypeEnum
    {
        [Name("ChatMessage")]
        Chat,
        [Name("CurrencyRankInventory")]
        Currency,
        ExternalProgram,
        [Name("InputKeyboardAndMouse")]
        Input,
        [Name("OverlayImagesAndVideos")]
        Overlay,
        Sound,
        Wait,
        [Obsolete]
        OBSStudio,
        [Obsolete]
        XSplit,
        [Name("CounterCreateAndUpdate")]
        Counter,
        GameQueue,
        [Obsolete]
        [Name("MixPlay")]
        Interactive,
        TextToSpeech,
        [Obsolete]
        Rank,
        WebRequest,
        [Obsolete]
        ActionGroup,
        SpecialIdentifier,
        [Name("FileReadAndWrite")]
        File,
        [Obsolete]
        SongRequest,
        [Obsolete]
        Spotify,
        Discord,
        [Obsolete]
        Translation,
        Twitter,
        Conditional,
        [Obsolete]
        StreamlabsOBS,
        StreamingSoftware,
        Streamlabs,
        [Obsolete]
        MixerClips,
        Command,
        Serial,
        Moderation,
        OvrStream,
        StreamingPlatform,
        IFTTT,
        Clips,

        Custom = 99,
    }

    [Obsolete]
    [DataContract]
    public abstract class ActionBase
    {
        [DataMember]
        public Guid ID { get; set; }

        [DataMember]
        public ActionTypeEnum Type { get; set; }

        [DataMember]
        public string Label { get; set; }

        [JsonIgnore]
        protected StreamingPlatformTypeEnum platform = StreamingPlatformTypeEnum.None;

        [JsonIgnore]
        protected Dictionary<string, string> extraSpecialIdentifiers = new Dictionary<string, string>();

        public ActionBase()
        {
            this.ID = Guid.NewGuid();
        }

        public ActionBase(ActionTypeEnum type)
            : this()
        {
            this.Type = type;
            this.Label = EnumLocalizationHelper.GetLocalizedName(this.Type);
        }

        public async Task Perform(UserViewModel user, StreamingPlatformTypeEnum platform, IEnumerable<string> arguments, Dictionary<string, string> extraSpecialIdentifiers)
        {
            await this.AsyncSemaphore.WaitAndRelease(async () =>
            {
                this.platform = platform;
                this.extraSpecialIdentifiers = extraSpecialIdentifiers;

                await this.PerformInternal(user, arguments);
            });
        }

        protected abstract Task PerformInternal(UserViewModel user, IEnumerable<string> arguments);

        protected async Task<string> ReplaceStringWithSpecialModifiers(string str, CommandParametersModel parameters, bool encode = false)
        {
            SpecialIdentifierStringBuilder siString = new SpecialIdentifierStringBuilder(str, encode);
            foreach (var kvp in this.extraSpecialIdentifiers)
            {
                siString.ReplaceSpecialIdentifier(kvp.Key, kvp.Value);
            }
            await siString.ReplaceCommonSpecialModifiers(parameters);
            return siString.ToString();
        }

        protected Dictionary<string, string> GetExtraSpecialIdentifiers() { return this.extraSpecialIdentifiers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value); }

        protected abstract SemaphoreSlim AsyncSemaphore { get; }
    }
}

#region Legacy Actions

namespace MixItUp.Base.Actions
{
    [Obsolete]
    public class SongRequestAction : ActionBase
    {
        private static SemaphoreSlim asyncSemaphore = new SemaphoreSlim(1);

        protected override SemaphoreSlim AsyncSemaphore { get { return SongRequestAction.asyncSemaphore; } }

        public SongRequestAction() { }

        protected override Task PerformInternal(UserViewModel user, IEnumerable<string> arguments)
        {
            return Task.FromResult(0);
        }
    }

    [Obsolete]
    [DataContract]
    public class SpotifyAction : ActionBase
    {
        private static SemaphoreSlim asyncSemaphore = new SemaphoreSlim(1);

        protected override SemaphoreSlim AsyncSemaphore { get { return SpotifyAction.asyncSemaphore; } }

#pragma warning disable CS0612 // Type or member is obsolete
        public SpotifyAction() : base(ActionTypeEnum.Spotify) { }
#pragma warning restore CS0612 // Type or member is obsolete

        protected override Task PerformInternal(UserViewModel user, IEnumerable<string> arguments)
        {
            return Task.FromResult(0);
        }
    }

    [Obsolete]
    [DataContract]
    public class InteractiveAction : ActionBase
    {
        private static SemaphoreSlim asyncSemaphore = new SemaphoreSlim(1);

        protected override SemaphoreSlim AsyncSemaphore { get { return InteractiveAction.asyncSemaphore; } }

#pragma warning disable CS0612 // Type or member is obsolete
        public InteractiveAction() : base(ActionTypeEnum.Interactive) { }
#pragma warning restore CS0612 // Type or member is obsolete

        protected override Task PerformInternal(UserViewModel user, IEnumerable<string> arguments)
        {
            return Task.FromResult(0);
        }
    }

    [Obsolete]
    [DataContract]
    public class MixerClipsAction : ActionBase
    {
        private static SemaphoreSlim asyncSemaphore = new SemaphoreSlim(1);

        protected override SemaphoreSlim AsyncSemaphore { get { return MixerClipsAction.asyncSemaphore; } }

#pragma warning disable CS0612 // Type or member is obsolete
        public MixerClipsAction() : base(ActionTypeEnum.MixerClips) { }
#pragma warning restore CS0612 // Type or member is obsolete

        protected override Task PerformInternal(UserViewModel user, IEnumerable<string> arguments)
        {
            return Task.FromResult(0);
        }
    }
}

#endregion