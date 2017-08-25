﻿using Mixer.Base.Util;

namespace MixItUp.Base.Commands
{
    public enum InteractiveCommandEventType
    {
        [Name("Mouse Down")]
        MouseDown,
        [Name("Mouse Up")]
        MouseUp,
        [Name("Key Up")]
        KeyUp,
        [Name("Key Down")]
        KeyDown,
        [Name("Move")]
        Move,
    }

    public class InteractiveCommand : CommandBase
    {
        public InteractiveCommandEventType EventType { get; set; }

        public InteractiveCommand() { }

        public InteractiveCommand(string name, string command, InteractiveCommandEventType eventType)
            : base(name, CommandTypeEnum.Interactive, command)
        {
            this.EventType = eventType;
        }

        public string EventTypeTransactionString { get { return this.EventType.ToString().ToLower(); } }
    }
}
