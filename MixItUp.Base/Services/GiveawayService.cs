﻿using MixItUp.Base.Model.Commands;
using MixItUp.Base.Model.Requirements;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.Chat;
using MixItUp.Base.ViewModel.User;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Services
{
    public class GiveawayUser
    {
        public UserViewModel User { get; set; }
        public int Entries { get; set; }
    }

    public interface IGiveawayService
    {
        bool IsRunning { get; }

        string Item { get; }
        int TimeLeft { get; }
        IEnumerable<GiveawayUser> Users { get; }
        UserViewModel Winner { get; }

        Task<string> Start(string item);

        Task End();
    }

    public class GiveawayService : IGiveawayService
    {
        public bool IsRunning { get; private set; }

        public string Item { get; private set; }
        public int TimeLeft { get; private set; }
        public IEnumerable<GiveawayUser> Users { get { return this.enteredUsers.Values.ToList(); } }
        public UserViewModel Winner { get; private set; }

        private ChatCommandModel giveawayCommand = null;

        private Dictionary<Guid, GiveawayUser> enteredUsers = new Dictionary<Guid, GiveawayUser>();

        private List<Guid> pastWinners = new List<Guid>();

        private CancellationTokenSource backgroundThreadCancellationTokenSource = new CancellationTokenSource();

        public async Task<string> Start(string item)
        {
            if (this.IsRunning)
            {
                return "A giveaway is already underway";
            }

            if (string.IsNullOrEmpty(item))
            {
                return "The name of the giveaway item must be specified";
            }
            this.Item = item;

            if (ChannelSession.Settings.GiveawayTimer <= 0)
            {
                return "The giveaway length must be greater than 0";
            }

            if (ChannelSession.Settings.GiveawayReminderInterval < 0)
            {
                return "The giveaway reminder must be 0 or greater";
            }

            if (ChannelSession.Settings.GiveawayMaximumEntries <= 0)
            {
                return "The maximum entries must be greater than 0";
            }

            if (string.IsNullOrEmpty(ChannelSession.Settings.GiveawayCommand))
            {
                return "Giveaway command must be specified";
            }

            if (ChannelSession.Settings.GiveawayCommand.Any(c => !Char.IsLetterOrDigit(c)))
            {
                return "Giveaway Command can only contain letters and numbers";
            }

            await ChannelSession.SaveSettings();

            this.IsRunning = true;
            this.Winner = null;

            this.giveawayCommand = new ChatCommandModel("Giveaway Command", new HashSet<string>() { ChannelSession.Settings.GiveawayCommand });
            if (ChannelSession.Settings.GiveawayAllowPastWinners)
            {
                this.pastWinners.Clear();
            }

            this.TimeLeft = ChannelSession.Settings.GiveawayTimer * 60;
            this.enteredUsers.Clear();

            GlobalEvents.GiveawaysChangedOccurred(usersUpdated: true);

            this.backgroundThreadCancellationTokenSource = new CancellationTokenSource();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () => { await this.GiveawayTimerBackground(); }, this.backgroundThreadCancellationTokenSource.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            await ChannelSession.Services.Command.Queue(ChannelSession.Settings.GiveawayStartedReminderCommandID, new CommandParametersModel(this.GetSpecialIdentifiers()));

            return null;
        }

        public Task End()
        {
            this.backgroundThreadCancellationTokenSource.Cancel();

            if (this.Winner != null)
            {
                pastWinners.Add(this.Winner.ID);
            }

            this.TimeLeft = 0;

            this.giveawayCommand = null;
            this.enteredUsers.Clear();

            this.IsRunning = false;

            GlobalEvents.GiveawaysChangedOccurred(usersUpdated: true);

            GlobalEvents.OnChatMessageReceived -= GlobalEvents_OnChatCommandMessageReceived;

            return Task.FromResult(0);
        }

        private async Task GiveawayTimerBackground()
        {
            GlobalEvents.OnChatMessageReceived += GlobalEvents_OnChatCommandMessageReceived;

            int totalTime = ChannelSession.Settings.GiveawayTimer * 60;
            int reminderTime = ChannelSession.Settings.GiveawayReminderInterval * 60;

            try
            {
                while (this.TimeLeft > 0)
                {
                    await Task.Delay(1000);
                    this.TimeLeft--;

                    if (reminderTime > 0 && this.TimeLeft > 0 && (totalTime - this.TimeLeft) % reminderTime == 0)
                    {
                        await ChannelSession.Services.Command.Queue(ChannelSession.Settings.GiveawayStartedReminderCommandID, new CommandParametersModel(this.GetSpecialIdentifiers()));
                    }

                    if (this.backgroundThreadCancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await this.End();
                        return;
                    }

                    GlobalEvents.GiveawaysChangedOccurred();
                }

                while (true)
                {
                    this.Winner = null;
                    if (this.enteredUsers.Count > 0)
                    {
                        int totalEntries = this.enteredUsers.Values.Sum(u => u.Entries);
                        int entryNumber = RandomHelper.GenerateRandomNumber(totalEntries);

                        int currentEntry = 0;
                        foreach (var kvp in this.enteredUsers.Values)
                        {
                            currentEntry += kvp.Entries;
                            if (entryNumber < currentEntry)
                            {
                                this.enteredUsers.Remove(kvp.User.ID);
                                this.Winner = kvp.User;
                                break;
                            }
                        }
                    }

                    if (this.Winner != null)
                    {
                        GlobalEvents.GiveawaysChangedOccurred(usersUpdated: true);

                        if (!ChannelSession.Settings.GiveawayRequireClaim)
                        {
                            await ChannelSession.Services.Command.Queue(ChannelSession.Settings.GiveawayWinnerSelectedCommandID, new CommandParametersModel(this.Winner, this.GetSpecialIdentifiers()));
                            await this.End();
                            return;
                        }
                        else
                        {
                            await ChannelSession.Services.Chat.SendMessage(string.Format("@{0} you've won the giveaway; type \"!claim\" in chat!.", this.Winner.Username));

                            this.TimeLeft = 60;
                            while (this.TimeLeft > 0)
                            {
                                await Task.Delay(1000);
                                this.TimeLeft--;

                                if (this.backgroundThreadCancellationTokenSource.Token.IsCancellationRequested)
                                {
                                    await this.End();
                                    return;
                                }

                                GlobalEvents.GiveawaysChangedOccurred();
                            }
                        }
                    }
                    else
                    {
                        await ChannelSession.Services.Chat.SendMessage("There are no users that entered/left in the giveaway");
                        await this.End();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private async void GlobalEvents_OnChatCommandMessageReceived(object sender, ChatMessageViewModel message)
        {
            try
            {
                if (this.TimeLeft > 0 && this.Winner == null && this.giveawayCommand.DoesMessageMatchTriggers(message, out IEnumerable<string> arguments))
                {
                    CommandParametersModel parameters = new CommandParametersModel(message);
                    parameters.Arguments = new List<string>(arguments);
                    int entries = 1;

                    if (pastWinners.Contains(message.User.ID))
                    {
                        await ChannelSession.Services.Chat.SendMessage("You have already won a giveaway and can not enter this one");
                        return;
                    }

                    if (arguments.Count() > 0 && ChannelSession.Settings.GiveawayMaximumEntries > 1)
                    {
                        if (!int.TryParse(arguments.ElementAt(0), out entries) || entries < 1)
                        {
                            entries = 1;
                        }
                    }

                    int currentEntries = 0;
                    if (this.enteredUsers.ContainsKey(message.User.ID))
                    {
                        currentEntries = this.enteredUsers[message.User.ID].Entries;
                    }

                    if ((entries + currentEntries) > ChannelSession.Settings.GiveawayMaximumEntries)
                    {
                        await ChannelSession.Services.Chat.SendMessage(string.Format("You may only enter {0} time(s), you currently have entered {1} time(s)", ChannelSession.Settings.GiveawayMaximumEntries, currentEntries));
                        return;
                    }

                    Result result = await ChannelSession.Settings.GiveawayRequirementsSet.Validate(parameters);
                    if (result.Success)
                    {
                        foreach (CurrencyRequirementModel requirement in ChannelSession.Settings.GiveawayRequirementsSet.Currency)
                        {
                            int totalAmount = requirement.MinAmount * entries;
                            result = requirement.ValidateAmount(message.User, totalAmount);
                            if (!result.Success)
                            {
                                await requirement.SendErrorChatMessage(message.User, result);
                                return;
                            }
                        }

                        foreach (InventoryRequirementModel requirement in ChannelSession.Settings.GiveawayRequirementsSet.Inventory)
                        {
                            int totalAmount = requirement.Amount * entries;
                            result = requirement.ValidateAmount(message.User, totalAmount);
                            if (!result.Success)
                            {
                                await requirement.SendErrorChatMessage(message.User, result);
                                return;
                            }
                        }

                        await ChannelSession.Settings.GiveawayRequirementsSet.Perform(parameters);
                        // Do additional currency / inventory performs passed on how many additional entries they put in
                        for (int i = 1; i < entries; i++)
                        {
                            foreach (CurrencyRequirementModel requirement in ChannelSession.Settings.GiveawayRequirementsSet.Currency)
                            {
                                await requirement.Perform(parameters);
                            }

                            foreach (InventoryRequirementModel requirement in ChannelSession.Settings.GiveawayRequirementsSet.Inventory)
                            {
                                await requirement.Perform(parameters);
                            }
                        }

                        if (!this.enteredUsers.ContainsKey(message.User.ID))
                        {
                            this.enteredUsers[message.User.ID] = new GiveawayUser() { User = message.User, Entries = 0 };
                        }
                        GiveawayUser giveawayUser = this.enteredUsers[message.User.ID];

                        if (giveawayUser != null)
                        {
                            giveawayUser.Entries += entries;

                            Dictionary<string, string> specialIdentifiers = this.GetSpecialIdentifiers();
                            specialIdentifiers["usergiveawayentries"] = entries.ToString();
                            specialIdentifiers["usergiveawaytotalentries"] = giveawayUser.Entries.ToString();

                            await ChannelSession.Services.Command.Queue(ChannelSession.Settings.GiveawayUserJoinedCommandID, new CommandParametersModel(message.User, arguments, specialIdentifiers));

                            GlobalEvents.GiveawaysChangedOccurred(usersUpdated: true);
                        }
                    }
                }
                else if (this.Winner != null && this.Winner.Equals(message.User) && message.PlainTextMessage.Equals("!claim", StringComparison.InvariantCultureIgnoreCase))
                {
                    await ChannelSession.Services.Command.Queue(ChannelSession.Settings.GiveawayWinnerSelectedCommandID, new CommandParametersModel(this.Winner, this.GetSpecialIdentifiers()));
                    await this.End();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private Dictionary<string, string> GetSpecialIdentifiers()
        {
            return new Dictionary<string, string>()
            {
                { "giveawayitem", this.Item },
                { "giveawaycommand", "!" + ChannelSession.Settings.GiveawayCommand },
                { "giveawaytimelimit", (this.TimeLeft / 60).ToString() }
            };
        }
    }
}
