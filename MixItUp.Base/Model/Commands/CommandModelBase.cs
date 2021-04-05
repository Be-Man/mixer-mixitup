﻿using MixItUp.Base.Model.Actions;
using MixItUp.Base.Model.Requirements;
using MixItUp.Base.Util;
using Newtonsoft.Json;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Commands
{
    public enum CommandTypeEnum
    {
        Custom = 0,
        Chat = 1,
        Event = 2,
        Timer = 3,
        ActionGroup = 4,
        Game = 5,
        [Obsolete]
        Remote = 6,
        TwitchChannelPoints = 7,
        PreMade = 8,

        // Specialty Command Types
        UserOnlyChat = 1000,
    }

    [DataContract]
    public class CommandGroupSettingsModel
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int TimerInterval { get; set; }

        public CommandGroupSettingsModel() { }

        public CommandGroupSettingsModel(string name) { this.Name = name; }

#pragma warning disable CS0612 // Type or member is obsolete
        internal CommandGroupSettingsModel(MixItUp.Base.Commands.CommandGroupSettings oldGroupSettings)
        {
            this.Name = oldGroupSettings.Name;
            this.TimerInterval = oldGroupSettings.TimerInterval;
        }
#pragma warning restore CS0612 // Type or member is obsolete
    }

    [DataContract]
    public abstract class CommandModelBase : IEquatable<CommandModelBase>, IComparable<CommandModelBase>
    {
        public const string CommandNameSpecialIdentifier = "commandname";

        public static IEnumerable<CommandTypeEnum> GetSelectableCommandTypes()
        {
            List<CommandTypeEnum> types = new List<CommandTypeEnum>(EnumHelper.GetEnumList<CommandTypeEnum>());
            types.Remove(CommandTypeEnum.PreMade);
            types.Remove(CommandTypeEnum.UserOnlyChat);
            types.Remove(CommandTypeEnum.Custom);
            return types;
        }

        public static async Task RunActions(IEnumerable<ActionModelBase> actions, CommandParametersModel parameters)
        {
            List<ActionModelBase> actionsToRun = new List<ActionModelBase>(actions);
            for (int i = 0; i < actionsToRun.Count; i++)
            {
                ActionModelBase action = actionsToRun[i];
                if (action is OverlayActionModel && ChannelSession.Services.Overlay.IsConnected)
                {
                    ChannelSession.Services.Overlay.StartBatching();
                }

                await action.Perform(parameters);

                if (action is OverlayActionModel && ChannelSession.Services.Overlay.IsConnected)
                {
                    if (i == (actionsToRun.Count - 1) || !(actionsToRun[i + 1] is OverlayActionModel))
                    {
                        await ChannelSession.Services.Overlay.EndBatching();
                    }
                }
            }
        }

        public static Dictionary<string, string> GetGeneralTestSpecialIdentifiers() { return new Dictionary<string, string>(); }

        [DataMember]
        public Guid ID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public CommandTypeEnum Type { get; set; }

        [DataMember]
        public bool IsEnabled { get; set; }

        [DataMember]
        public bool Unlocked { get; set; }

        [DataMember]
        public bool IsEmbedded { get; set; }

        [DataMember]
        public string GroupName { get; set; }

        [DataMember]
        public HashSet<string> Triggers { get; set; } = new HashSet<string>();

        [DataMember]
        public RequirementsSetModel Requirements { get; set; } = new RequirementsSetModel();

        [DataMember]
        public List<ActionModelBase> Actions { get; set; } = new List<ActionModelBase>();

        public CommandModelBase(string name, CommandTypeEnum type)
        {
            this.ID = Guid.NewGuid();
            this.IsEnabled = true;
            this.Name = name;
            this.Type = type;
        }

#pragma warning disable CS0612 // Type or member is obsolete
        protected CommandModelBase(MixItUp.Base.Commands.CommandBase command)
        {
            if (command != null)
            {
                this.ID = command.ID;
                this.GroupName = command.GroupName;
                this.IsEnabled = command.IsEnabled;
                this.Unlocked = command.Unlocked;

                if (command is MixItUp.Base.Commands.PermissionsCommandBase)
                {
                    MixItUp.Base.Commands.PermissionsCommandBase pCommand = (MixItUp.Base.Commands.PermissionsCommandBase)command;
                    this.Requirements = new RequirementsSetModel(pCommand.Requirements);
                }

                foreach (MixItUp.Base.Actions.ActionBase action in command.Actions)
                {
                    this.Actions.AddRange(ActionModelBase.UpgradeAction(action));
                }
            }
            else
            {
                this.ID = Guid.NewGuid();
            }
        }
#pragma warning restore CS0612 // Type or member is obsolete

        protected CommandModelBase() { }

        [JsonIgnore]
        protected abstract SemaphoreSlim CommandLockSemaphore { get; }

        public string TriggersString { get { return string.Join(" ", this.Triggers); } }

        public virtual IEnumerable<string> GetFullTriggers() { return this.Triggers; }

        public CommandGroupSettingsModel CommandGroupSettings { get { return (!string.IsNullOrEmpty(this.GroupName) && ChannelSession.Settings.CommandGroups.ContainsKey(this.GroupName)) ? ChannelSession.Settings.CommandGroups[this.GroupName] : null; } }

        public bool IsUnlocked { get { return this.Unlocked || ChannelSession.Settings.UnlockAllCommands; } }

        public virtual Dictionary<string, string> GetTestSpecialIdentifiers() { return CommandModelBase.GetGeneralTestSpecialIdentifiers(); }

        public virtual async Task TestPerform()
        {
            await this.Perform(CommandParametersModel.GetTestParameters(this.GetTestSpecialIdentifiers()));
            if (this.Requirements.Cooldown != null)
            {
                this.Requirements.Reset();
            }
        }

        public async Task Perform() { await this.Perform(new CommandParametersModel()); }

        public virtual async Task Perform(CommandParametersModel parameters)
        {
            if (this.IsEnabled && this.DoesCommandHaveWork)
            {
                bool waitForFinish = parameters.WaitForCommandToFinish;
                Task commandTask = Task.Run(async () =>
                {
                    bool lockPerformed = false;
                    try
                    {
                        Logger.Log(LogLevel.Debug, $"Starting command performing: {this}");

                        if (!this.IsUnlocked && !parameters.DontLockCommand)
                        {
                            lockPerformed = true;
                            await this.CommandLockSemaphore.WaitAsync();
                        }

                        parameters.SpecialIdentifiers[CommandModelBase.CommandNameSpecialIdentifier] = this.Name;

                        if (await this.ValidateRequirements(parameters))
                        {
                            List<CommandParametersModel> runnerParameters = new List<CommandParametersModel>() { parameters };
                            if (this.Requirements != null)
                            {
                                await this.PerformRequirements(parameters);
                                runnerParameters = new List<CommandParametersModel>(this.Requirements.GetPerformingUsers(parameters));
                            }

                            this.TrackTelemetry();

                            foreach (CommandParametersModel p in runnerParameters)
                            {
                                p.WaitForCommandToFinish = true;
                                p.User.Data.TotalCommandsRun++;
                                await this.PerformInternal(p);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }

                    if (lockPerformed)
                    {
                        this.CommandLockSemaphore.Release();
                    }
                });

                if (waitForFinish)
                {
                    await commandTask;
                }
            }
        }

        public override string ToString() { return string.Format("{0} - {1}", this.ID, this.Name); }

        public int CompareTo(object obj)
        {
            if (obj is CommandModelBase)
            {
                return this.CompareTo((CommandModelBase)obj);
            }
            return 0;
        }

        public int CompareTo(CommandModelBase other) { return this.Name.CompareTo(other.Name); }

        public override bool Equals(object obj)
        {
            if (obj is CommandModelBase)
            {
                return this.Equals((CommandModelBase)obj);
            }
            return false;
        }

        public bool Equals(CommandModelBase other) { return this.ID.Equals(other.ID); }

        public override int GetHashCode() { return this.ID.GetHashCode(); }

        public virtual bool DoesCommandHaveWork { get { return this.Actions.Count > 0; } }

        protected virtual async Task<bool> ValidateRequirements(CommandParametersModel parameters)
        {
            if (this.Requirements != null)
            {
                Result result = await this.Requirements.Validate(parameters);
                return result.Success;
            }
            return true;
        }

        protected virtual async Task PerformRequirements(CommandParametersModel parameters)
        {
            await this.Requirements.Perform(parameters);
        }

        protected virtual async Task PerformInternal(CommandParametersModel parameters)
        {
            await CommandModelBase.RunActions(this.Actions, parameters);
        }

        protected virtual void TrackTelemetry() { ChannelSession.Services.Telemetry.TrackCommand(this.Type); }
    }
}
