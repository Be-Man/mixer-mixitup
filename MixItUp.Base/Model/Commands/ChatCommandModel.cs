﻿using MixItUp.Base.ViewModel.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;

namespace MixItUp.Base.Model.Commands
{
    [DataContract]
    public class ChatCommandModel : CommandModelBase
    {
        public const string CommandWildcardMatchingRegexFormat = "\\s?{0}\\s?";

        public static bool IsValidCommandTrigger(string command)
        {
            if (!string.IsNullOrWhiteSpace(command))
            {
                return command.All(c => Char.IsLetterOrDigit(c) || Char.IsWhiteSpace(c) || Char.IsSymbol(c) || Char.IsPunctuation(c));
            }
            return false;
        }

        public static bool DoesMessageMatchTriggers(ChatMessageViewModel message, IEnumerable<string> triggers, out IEnumerable<string> arguments)
        {
            arguments = null;
            if (!string.IsNullOrEmpty(message.PlainTextMessage))
            {
                foreach (string trigger in triggers)
                {
                    if (string.Equals(message.PlainTextMessage, trigger, StringComparison.CurrentCultureIgnoreCase) || message.PlainTextMessage.StartsWith(trigger + " "))
                    {
                        arguments = message.PlainTextMessage.Replace(trigger, "").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool DoesMessageMatchWildcardTriggers(ChatMessageViewModel message, IEnumerable<string> triggers, out IEnumerable<string> arguments)
        {
            arguments = null;
            foreach (string trigger in triggers)
            {
                Match match = Regex.Match(message.PlainTextMessage, string.Format(CommandWildcardMatchingRegexFormat, Regex.Escape(trigger)), RegexOptions.IgnoreCase);
                if (match != null && match.Success)
                {
                    arguments = message.PlainTextMessage.Substring(match.Index + match.Length).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    return true;
                }
            }
            return false;
        }

        private static SemaphoreSlim commandLockSemaphore = new SemaphoreSlim(1);

        [DataMember]
        public bool IncludeExclamation { get; set; }

        [DataMember]
        public bool Wildcards { get; set; }

        public ChatCommandModel(string name, HashSet<string> triggers) : this(name, CommandTypeEnum.Chat, triggers, includeExclamation: true, wildcards: false) { }

        public ChatCommandModel(string name, HashSet<string> triggers, bool includeExclamation, bool wildcards) : this(name, CommandTypeEnum.Chat, triggers, includeExclamation, wildcards) { }

#pragma warning disable CS0612 // Type or member is obsolete
        internal ChatCommandModel(MixItUp.Base.Commands.ChatCommand command)
            : base(command)
        {
            this.Name = command.Name;
            this.Type = CommandTypeEnum.Chat;
            this.Triggers = command.Commands;
            this.IncludeExclamation = command.IncludeExclamationInCommands;
            this.Wildcards = command.Wildcards;
        }
#pragma warning restore CS0612 // Type or member is obsolete

        protected ChatCommandModel(string name, CommandTypeEnum type, HashSet<string> triggers, bool includeExclamation, bool wildcards)
            : base(name, type)
        {
            this.Triggers = triggers;
            this.IncludeExclamation = includeExclamation;
            this.Wildcards = wildcards;
        }

        protected ChatCommandModel() : base() { }

        protected override SemaphoreSlim CommandLockSemaphore { get { return ChatCommandModel.commandLockSemaphore; } }

        public override IEnumerable<string> GetFullTriggers() { return this.IncludeExclamation ? this.Triggers.Select(t => "!" + t) : this.Triggers; }

        public bool DoesMessageMatchTriggers(ChatMessageViewModel message, out IEnumerable<string> arguments) { return ChatCommandModel.DoesMessageMatchTriggers(message, this.GetFullTriggers(), out arguments); }

        public bool DoesMessageMatchWildcardTriggers(ChatMessageViewModel message, out IEnumerable<string> arguments) { return ChatCommandModel.DoesMessageMatchWildcardTriggers(message, this.Triggers, out arguments); }
    }

    [DataContract]
    public class UserOnlyChatCommandModel : ChatCommandModel
    {
        [DataMember]
        public Guid UserID { get; set; }

        public UserOnlyChatCommandModel(string name, HashSet<string> triggers, bool includeExclamation, bool wildcards, Guid userID)
            : base(name, CommandTypeEnum.UserOnlyChat, triggers, includeExclamation, wildcards)
        {
            this.UserID = userID;
        }

#pragma warning disable CS0612 // Type or member is obsolete
        internal UserOnlyChatCommandModel(MixItUp.Base.Commands.ChatCommand command, Guid userID)
            : base(command)
        {
            this.UserID = userID;
        }
#pragma warning restore CS0612 // Type or member is obsolete

        private UserOnlyChatCommandModel() { }
    }

    public class NewAutoChatCommandModel
    {
        public bool AddCommand { get; set; }
        public string Description { get; set; }
        public ChatCommandModel Command { get; set; }

        public NewAutoChatCommandModel(string description, ChatCommandModel command)
        {
            this.AddCommand = true;
            this.Description = description;
            this.Command = command;
        }
    }
}
