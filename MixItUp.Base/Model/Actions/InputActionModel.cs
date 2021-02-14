﻿using MixItUp.Base.Model.Commands;
using MixItUp.Base.Services;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Actions
{
    public enum SimpleInputMouseEnum
    {
        LeftButton,
        RightButton,
        MiddleButton,
    }

    public enum InputActionTypeEnum
    {
        Click,
        Press,
        Release,
    }

    [DataContract]
    public class InputActionModel : ActionModelBase
    {
        [DataMember]
        public InputKeyEnum? Key { get; set; }

        [DataMember]
        public SimpleInputMouseEnum? Mouse { get; set; }

        [DataMember]
        public InputActionTypeEnum ActionType { get; set; }

        [DataMember]
        public bool Shift { get; set; }
        [DataMember]
        public bool Control { get; set; }
        [DataMember]
        public bool Alt { get; set; }

        public InputActionModel(InputKeyEnum key, InputActionTypeEnum actionType, bool shift, bool control, bool alt)
            : this(actionType, shift, control, alt)
        {
            this.Key = key;
        }

        public InputActionModel(SimpleInputMouseEnum mouse, InputActionTypeEnum actionType, bool shift, bool control, bool alt)
            : this(actionType, shift, control, alt)
        {
            this.Mouse = mouse;
        }

        private InputActionModel(InputActionTypeEnum actionType, bool shift, bool control, bool alt)
            : base(ActionTypeEnum.Input)
        {
            this.ActionType = actionType;
            this.Shift = shift;
            this.Control = control;
            this.Alt = alt;
        }

#pragma warning disable CS0612 // Type or member is obsolete
        internal InputActionModel(MixItUp.Base.Actions.InputAction action)
            : base(ActionTypeEnum.Input)
        {
            this.Key = action.Key;
            if (action.Mouse != null) { this.Mouse = (SimpleInputMouseEnum)(int)action.Mouse; }
            this.ActionType = (InputActionTypeEnum)(int)action.ActionType;
            this.Shift = action.Shift;
            this.Control = action.Control;
            this.Alt = action.Alt;
        }
#pragma warning restore CS0612 // Type or member is obsolete

        private InputActionModel() { }

        protected override async Task PerformInternal(CommandParametersModel parameters)
        {
            if (this.ActionType == InputActionTypeEnum.Press || this.ActionType == InputActionTypeEnum.Click)
            {
                if (this.Shift) { ChannelSession.Services.InputService.KeyDown(InputKeyEnum.LeftShift); }
                if (this.Control) { ChannelSession.Services.InputService.KeyDown(InputKeyEnum.LeftControl); }
                if (this.Alt) { ChannelSession.Services.InputService.KeyDown(InputKeyEnum.LeftAlt); }
            }

            await ChannelSession.Services.InputService.WaitForKeyToRegister();

            if (this.Key != null)
            {
                if (this.ActionType == InputActionTypeEnum.Click)
                {
                    await ChannelSession.Services.InputService.KeyClick(this.Key.GetValueOrDefault());
                }
                else if (this.ActionType == InputActionTypeEnum.Press)
                {
                    ChannelSession.Services.InputService.KeyDown(this.Key.GetValueOrDefault());
                }
                else if (this.ActionType == InputActionTypeEnum.Release)
                {
                    ChannelSession.Services.InputService.KeyUp(this.Key.GetValueOrDefault());
                }
            }
            else if (this.Mouse != null)
            {
                if (this.ActionType == InputActionTypeEnum.Click)
                {
                    if (this.Mouse.GetValueOrDefault() == SimpleInputMouseEnum.LeftButton)
                    {
                        await ChannelSession.Services.InputService.LeftMouseClick();
                    }
                    else if (this.Mouse.GetValueOrDefault() == SimpleInputMouseEnum.RightButton)
                    {
                        await ChannelSession.Services.InputService.RightMouseClick();
                    }
                    else if (this.Mouse.GetValueOrDefault() == SimpleInputMouseEnum.MiddleButton)
                    {
                        await ChannelSession.Services.InputService.MiddleMouseClick();
                    }
                }
                else if (this.ActionType == InputActionTypeEnum.Press)
                {
                    if (this.Mouse.GetValueOrDefault() == SimpleInputMouseEnum.LeftButton)
                    {
                        ChannelSession.Services.InputService.MouseEvent(InputMouseEnum.LeftDown);
                    }
                    else if (this.Mouse.GetValueOrDefault() == SimpleInputMouseEnum.RightButton)
                    {
                        ChannelSession.Services.InputService.MouseEvent(InputMouseEnum.RightDown);
                    }
                    else if (this.Mouse.GetValueOrDefault() == SimpleInputMouseEnum.MiddleButton)
                    {
                        ChannelSession.Services.InputService.MouseEvent(InputMouseEnum.MiddleDown);
                    }
                }
                else if (this.ActionType == InputActionTypeEnum.Release)
                {
                    if (this.Mouse.GetValueOrDefault() == SimpleInputMouseEnum.LeftButton)
                    {
                        ChannelSession.Services.InputService.MouseEvent(InputMouseEnum.LeftUp);
                    }
                    else if (this.Mouse.GetValueOrDefault() == SimpleInputMouseEnum.RightButton)
                    {
                        ChannelSession.Services.InputService.MouseEvent(InputMouseEnum.RightUp);
                    }
                    else if (this.Mouse.GetValueOrDefault() == SimpleInputMouseEnum.MiddleButton)
                    {
                        ChannelSession.Services.InputService.MouseEvent(InputMouseEnum.MiddleUp);
                    }
                }
            }

            await ChannelSession.Services.InputService.WaitForKeyToRegister();

            if (this.ActionType == InputActionTypeEnum.Release || this.ActionType == InputActionTypeEnum.Click)
            {
                if (this.Shift) { ChannelSession.Services.InputService.KeyUp(InputKeyEnum.LeftShift); }
                if (this.Control) { ChannelSession.Services.InputService.KeyUp(InputKeyEnum.LeftControl); }
                if (this.Alt) { ChannelSession.Services.InputService.KeyUp(InputKeyEnum.LeftAlt); }
            }
        }
    }
}
