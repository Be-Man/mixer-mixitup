﻿using MixItUp.Base.Model.Actions;
using StreamingClient.Base.Util;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MixItUp.Base.ViewModel.Controls.Actions
{
    public class StreamlabsActionEditorControlViewModel : ActionEditorControlViewModelBase
    {
        public override ActionTypeEnum Type { get { return ActionTypeEnum.Streamlabs; } }

        public IEnumerable<StreamlabsActionTypeEnum> ActionTypes { get { return EnumHelper.GetEnumList<StreamlabsActionTypeEnum>(); } }

        public StreamlabsActionTypeEnum SelectedActionType
        {
            get { return this.selectedActionType; }
            set
            {
                this.selectedActionType = value;
                this.NotifyPropertyChanged();
            }
        }
        private StreamlabsActionTypeEnum selectedActionType;

        public StreamlabsActionEditorControlViewModel(StreamlabsActionModel action)
        {
            this.SelectedActionType = action.ActionType;
        }

        public StreamlabsActionEditorControlViewModel() { }

        public override Task<ActionModelBase> GetAction() { return Task.FromResult<ActionModelBase>(new StreamlabsActionModel(this.SelectedActionType)); }
    }
}
