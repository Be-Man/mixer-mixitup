﻿using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.User;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Commands.Games
{
    [DataContract]
    public class HeistGameCommandModel : GameCommandModelBase
    {
        [DataMember]
        public int MinimumParticipants { get; set; }
        [DataMember]
        public int TimeLimit { get; set; }

        [DataMember]
        public CustomCommandModel StartedCommand { get; set; }
        [DataMember]
        public CustomCommandModel UserJoinCommand { get; set; }
        [DataMember]
        public CustomCommandModel NotEnoughPlayersCommand { get; set; }

        [DataMember]
        public GameOutcomeModel UserSuccessOutcome { get; set; }
        [DataMember]
        public CustomCommandModel UserFailureCommand { get; set; }

        [DataMember]
        public CustomCommandModel AllSucceedCommand { get; set; }
        [DataMember]
        public CustomCommandModel TopThirdsSucceedCommand { get; set; }
        [DataMember]
        public CustomCommandModel MiddleThirdsSucceedCommand { get; set; }
        [DataMember]
        public CustomCommandModel LowThirdsSucceedCommand { get; set; }
        [DataMember]
        public CustomCommandModel NoneSucceedCommand { get; set; }

        [JsonIgnore]
        private CommandParametersModel runParameters;
        [JsonIgnore]
        private Dictionary<UserViewModel, CommandParametersModel> runUsers = new Dictionary<UserViewModel, CommandParametersModel>();

        public HeistGameCommandModel(string name, HashSet<string> triggers, int minimumParticipants, int timeLimit, CustomCommandModel startedCommand,
            CustomCommandModel userJoinCommand, CustomCommandModel notEnoughPlayersCommand, GameOutcomeModel userSuccessOutcome, CustomCommandModel userFailureCommand,
            CustomCommandModel allSucceedCommand, CustomCommandModel topThirdsSucceedCommand, CustomCommandModel middleThirdsSucceedCommand, CustomCommandModel lowThirdsSucceedCommand,
            CustomCommandModel noneSucceedCommand)
            : base(name, triggers, GameCommandTypeEnum.Heist)
        {
            this.MinimumParticipants = minimumParticipants;
            this.TimeLimit = timeLimit;
            this.StartedCommand = startedCommand;
            this.UserJoinCommand = userJoinCommand;
            this.NotEnoughPlayersCommand = notEnoughPlayersCommand;
            this.UserSuccessOutcome = userSuccessOutcome;
            this.UserFailureCommand = userFailureCommand;
            this.AllSucceedCommand = allSucceedCommand;
            this.TopThirdsSucceedCommand = topThirdsSucceedCommand;
            this.MiddleThirdsSucceedCommand = middleThirdsSucceedCommand;
            this.LowThirdsSucceedCommand = lowThirdsSucceedCommand;
            this.NoneSucceedCommand = noneSucceedCommand;
        }

#pragma warning disable CS0612 // Type or member is obsolete
        internal HeistGameCommandModel(Base.Commands.HeistGameCommand command)
            : base(command, GameCommandTypeEnum.Heist)
        {
            this.MinimumParticipants = command.MinimumParticipants;
            this.TimeLimit = command.TimeLimit;
            this.StartedCommand = new CustomCommandModel(command.StartedCommand) { IsEmbedded = true };
            this.UserJoinCommand = new CustomCommandModel(command.UserJoinCommand) { IsEmbedded = true };
            this.NotEnoughPlayersCommand = new CustomCommandModel(command.NotEnoughPlayersCommand) { IsEmbedded = true };
            this.UserSuccessOutcome = new GameOutcomeModel(command.UserSuccessOutcome);
            this.UserFailureCommand = new CustomCommandModel(command.UserFailOutcome.Command) { IsEmbedded = true };
            this.AllSucceedCommand = new CustomCommandModel(command.AllSucceedCommand) { IsEmbedded = true };
            this.TopThirdsSucceedCommand = new CustomCommandModel(command.TopThirdsSucceedCommand) { IsEmbedded = true };
            this.MiddleThirdsSucceedCommand = new CustomCommandModel(command.MiddleThirdsSucceedCommand) { IsEmbedded = true };
            this.LowThirdsSucceedCommand = new CustomCommandModel(command.LowThirdsSucceedCommand) { IsEmbedded = true };
            this.NoneSucceedCommand = new CustomCommandModel(command.NoneSucceedCommand) { IsEmbedded = true };
        }
#pragma warning restore CS0612 // Type or member is obsolete

        private HeistGameCommandModel() { }

        public override IEnumerable<CommandModelBase> GetInnerCommands()
        {
            List<CommandModelBase> commands = new List<CommandModelBase>();
            commands.Add(this.StartedCommand);
            commands.Add(this.UserJoinCommand);
            commands.Add(this.NotEnoughPlayersCommand);
            commands.Add(this.UserSuccessOutcome.Command);
            commands.Add(this.UserFailureCommand);
            commands.Add(this.AllSucceedCommand);
            commands.Add(this.TopThirdsSucceedCommand);
            commands.Add(this.MiddleThirdsSucceedCommand);
            commands.Add(this.LowThirdsSucceedCommand);
            commands.Add(this.NoneSucceedCommand);
            return commands;
        }

        public override async Task CustomRun(CommandParametersModel parameters)
        {
            await this.RefundCooldown(parameters);
            if (this.runParameters == null)
            {
                this.runParameters = parameters;
                this.runUsers[parameters.User] = parameters;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                AsyncRunner.RunAsyncBackground(async (cancellationToken) =>
                {
                    await DelayNoThrow(this.TimeLimit * 1000, cancellationToken);

                    if (this.runUsers.Count < this.MinimumParticipants)
                    {
                        await this.RunSubCommand(this.NotEnoughPlayersCommand, this.runParameters);
                        foreach (var kvp in this.runUsers.ToList())
                        {
                            await this.Requirements.Refund(kvp.Value);
                        }
                        await this.PerformCooldown(this.runParameters);
                        this.ClearData();
                        return;
                    }

                    List<CommandParametersModel> participants = new List<CommandParametersModel>();
                    List<CommandParametersModel> winners = new List<CommandParametersModel>();
                    int totalPayout = 0;
                    foreach (CommandParametersModel participant in this.runUsers.Values.ToList())
                    {
                        if (this.GenerateProbability() <= this.UserSuccessOutcome.GetRoleProbabilityPayout(parameters.User).Probability)
                        {
                            winners.Add(participant);
                            totalPayout += await this.RunOutcome(participant, this.UserSuccessOutcome);
                        }
                        else
                        {
                            await this.RunSubCommand(this.UserFailureCommand, participant);
                        }
                    }

                    this.SetGameWinners(this.runParameters, winners);
                    this.runParameters.SpecialIdentifiers[GameCommandModelBase.GameAllPayoutSpecialIdentifier] = totalPayout.ToString();
                    double successRate = Convert.ToDouble(winners.Count) / Convert.ToDouble(this.runUsers.Count);
                    if (successRate == 1.0)
                    {
                        await this.RunSubCommand(this.AllSucceedCommand, this.runParameters);
                    }
                    else if (successRate > (2.0 / 3.0))
                    {
                        await this.RunSubCommand(this.TopThirdsSucceedCommand, this.runParameters);
                    }
                    else if (successRate > (1.0 / 3.0))
                    {
                        await this.RunSubCommand(this.MiddleThirdsSucceedCommand, this.runParameters);
                    }
                    else if (successRate > 0)
                    {
                        await this.RunSubCommand(this.LowThirdsSucceedCommand, this.runParameters);
                    }
                    else
                    {
                        await this.RunSubCommand(this.NoneSucceedCommand, this.runParameters);
                    }

                    await this.PerformCooldown(this.runParameters);
                    this.ClearData();
                }, new CancellationToken());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                await this.RunSubCommand(this.StartedCommand, this.runParameters);
                await this.RunSubCommand(this.UserJoinCommand, this.runParameters);
                return;
            }
            else if (this.runParameters != null && !this.runUsers.ContainsKey(parameters.User))
            {
                this.runUsers[parameters.User] = parameters;
                await this.RunSubCommand(this.UserJoinCommand, parameters);
                return;
            }
            else
            {
                await ChannelSession.Services.Chat.SendMessage(MixItUp.Base.Resources.GameCommandAlreadyUnderway);
            }
            await this.Requirements.Refund(parameters);
        }

        private void ClearData()
        {
            this.runParameters = null;
            this.runUsers.Clear();
        }
    }
}