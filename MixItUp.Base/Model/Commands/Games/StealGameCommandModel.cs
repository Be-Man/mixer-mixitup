﻿using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Commands.Games
{
    [DataContract]
    public class StealGameCommandModel : GameCommandModelBase
    {
        [DataMember]
        public GamePlayerSelectionType PlayerSelectionType { get; set; }

        [DataMember]
        public GameOutcomeModel SuccessfulOutcome { get; set; }

        [DataMember]
        public CustomCommandModel FailedCommand { get; set; }

        public StealGameCommandModel(string name, HashSet<string> triggers, GamePlayerSelectionType playerSelectionType, GameOutcomeModel successfulOutcome, CustomCommandModel failedCommand)
            : base(name, triggers, GameCommandTypeEnum.Steal)
        {
            this.PlayerSelectionType = playerSelectionType;
            this.SuccessfulOutcome = successfulOutcome;
            this.FailedCommand = failedCommand;
        }

#pragma warning disable CS0612 // Type or member is obsolete
        internal StealGameCommandModel(Base.Commands.StealGameCommand command)
            : base(command, GameCommandTypeEnum.Steal)
        {
            this.PlayerSelectionType = GamePlayerSelectionType.Random;
            this.SuccessfulOutcome = new GameOutcomeModel(command.SuccessfulOutcome);
            this.FailedCommand = new CustomCommandModel(command.FailedOutcome.Command) { IsEmbedded = true };
        }

        internal StealGameCommandModel(Base.Commands.PickpocketGameCommand command)
            : base(command, GameCommandTypeEnum.Steal)
        {
            this.PlayerSelectionType = GamePlayerSelectionType.Targeted;
            this.SuccessfulOutcome = new GameOutcomeModel(command.SuccessfulOutcome);
            this.FailedCommand = new CustomCommandModel(command.FailedOutcome.Command) { IsEmbedded = true };
        }
#pragma warning restore CS0612 // Type or member is obsolete

        private StealGameCommandModel() { }

        public override IEnumerable<CommandModelBase> GetInnerCommands()
        {
            List<CommandModelBase> commands = new List<CommandModelBase>();
            commands.Add(this.SuccessfulOutcome.Command);
            commands.Add(this.FailedCommand);
            return commands;
        }

        public override async Task CustomRun(CommandParametersModel parameters)
        {
            await this.SetSelectedUser(this.PlayerSelectionType, parameters);
            if (parameters.TargetUser != null)
            {
                if (await this.ValidateTargetUserPrimaryBetAmount(parameters))
                {
                    int betAmount = this.GetPrimaryBetAmount(parameters);
                    parameters.SpecialIdentifiers[GameCommandModelBase.GamePayoutSpecialIdentifier] = betAmount.ToString();
                    if (this.GenerateProbability() <= this.SuccessfulOutcome.GetRoleProbabilityPayout(parameters.User).Probability)
                    {
                        this.PerformPrimarySetPayout(parameters.User, betAmount * 2);
                        this.PerformPrimarySetPayout(parameters.TargetUser, -betAmount);
                        this.SetGameWinners(parameters, new List<CommandParametersModel>() { parameters });
                        await this.RunSubCommand(this.SuccessfulOutcome.Command, parameters);
                    }
                    else
                    {
                        await this.RunSubCommand(this.FailedCommand, parameters);
                    }
                    await this.PerformCooldown(parameters);
                    return;
                }
            }
            else
            {
                await ChannelSession.Services.Chat.SendMessage(MixItUp.Base.Resources.GameCommandCouldNotFindUser);
            }
            await this.Requirements.Refund(parameters);
        }
    }
}