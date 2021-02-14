﻿using System.Runtime.Serialization;

namespace MixItUp.Base.Model.Requirements
{
    public class SettingsRequirementModel : RequirementModelBase
    {
        [DataMember]
        public bool DeleteChatMessageWhenRun { get; set; }
        [DataMember]
        public bool DontDeleteChatMessageWhenRun { get; set; }

        [DataMember]
        public bool ShowOnChatContextMenu { get; set; }

        public SettingsRequirementModel() { }

#pragma warning disable CS0612 // Type or member is obsolete
        internal SettingsRequirementModel(MixItUp.Base.ViewModel.Requirement.SettingsRequirementViewModel requirement)
            : this()
        {
            this.DeleteChatMessageWhenRun = requirement.DeleteChatCommandWhenRun;
            this.DontDeleteChatMessageWhenRun = requirement.DontDeleteChatCommandWhenRun;
            this.ShowOnChatContextMenu = requirement.ShowOnChatMenu;
        }
#pragma warning restore CS0612 // Type or member is obsolete

        public bool ShouldChatMessageBeDeletedWhenRun { get { return this.DeleteChatMessageWhenRun || (ChannelSession.Settings.DeleteChatCommandsWhenRun && !this.DontDeleteChatMessageWhenRun); } }
    }
}
