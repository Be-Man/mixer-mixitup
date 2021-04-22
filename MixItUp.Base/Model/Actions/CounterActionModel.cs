﻿using MixItUp.Base.Model.Commands;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Actions
{
    public enum CounterActionTypeEnum
    {
        Update,
        Reset,
        Set
    }

    [DataContract]
    public class CounterActionModel : ActionModelBase
    {
        [DataMember]
        public string CounterName { get; set; }

        [DataMember]
        public CounterActionTypeEnum ActionType { get; set; }

        [DataMember]
        public string Amount { get; set; }

        public CounterActionModel(string counterName, CounterActionTypeEnum actionType, string amount)
            : base(ActionTypeEnum.Counter)
        {
            this.CounterName = counterName;
            this.ActionType = actionType;
            this.Amount = amount;
        }

#pragma warning disable CS0612 // Type or member is obsolete
        internal CounterActionModel(MixItUp.Base.Actions.CounterAction action)
            : base(ActionTypeEnum.Counter)
        {
            this.CounterName = action.CounterName;
            if (action.UpdateAmount) { this.ActionType = CounterActionTypeEnum.Update; }
            else if (action.SetAmount) { this.ActionType = CounterActionTypeEnum.Set; }
            else if (action.ResetAmount) { this.ActionType = CounterActionTypeEnum.Reset; }
            this.Amount = action.Amount;
        }
#pragma warning restore CS0612 // Type or member is obsolete

        private CounterActionModel() { }

        protected override async Task PerformInternal(CommandParametersModel parameters)
        {
            string counterName = await this.ReplaceStringWithSpecialModifiers(this.CounterName, parameters);
            if (ChannelSession.Settings.Counters.ContainsKey(counterName))
            {
                if (this.ActionType == CounterActionTypeEnum.Reset)
                {
                    await ChannelSession.Settings.Counters[counterName].ResetAmount();
                }
                else
                {
                    string amountText = await this.ReplaceStringWithSpecialModifiers(this.Amount, parameters);
                    if (double.TryParse(amountText, out double amount))
                    {
                        if (this.ActionType == CounterActionTypeEnum.Update)
                        {
                            await ChannelSession.Settings.Counters[counterName].UpdateAmount(amount);
                        }
                        else if (this.ActionType == CounterActionTypeEnum.Set)
                        {
                            await ChannelSession.Settings.Counters[counterName].SetAmount(amount);
                        }
                    }
                }
            }
        }
    }
}
