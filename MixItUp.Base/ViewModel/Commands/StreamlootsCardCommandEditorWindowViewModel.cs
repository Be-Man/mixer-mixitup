﻿using MixItUp.Base.Model.Commands;
using MixItUp.Base.Util;
using System.Threading.Tasks;

namespace MixItUp.Base.ViewModel.Commands
{
    public class StreamlootsCardCommandEditorWindowViewModel : CommandEditorWindowViewModelBase
    {
        public StreamlootsCardCommandEditorWindowViewModel(StreamlootsCardCommandModel existingCommand) : base(existingCommand) { }

        public StreamlootsCardCommandEditorWindowViewModel() : base(CommandTypeEnum.StreamlootsCard) { }

        public override Task<Result> Validate()
        {
            if (string.IsNullOrWhiteSpace(this.Name))
            {
                return Task.FromResult(new Result(MixItUp.Base.Resources.StreamlootsCardNameMissing));
            }
            return Task.FromResult(new Result());
        }

        public override Task<CommandModelBase> CreateNewCommand() { return Task.FromResult<CommandModelBase>(new StreamlootsCardCommandModel(this.Name)); }

        public override async Task UpdateExistingCommand(CommandModelBase command)
        {
            await base.UpdateExistingCommand(command);
        }

        public override Task SaveCommandToSettings(CommandModelBase command)
        {
            ChannelSession.Services.Command.StreamlootsCardCommands.Remove((StreamlootsCardCommandModel)this.existingCommand);
            ChannelSession.Services.Command.StreamlootsCardCommands.Add((StreamlootsCardCommandModel)command);
            return Task.FromResult(0);
        }
    }
}
