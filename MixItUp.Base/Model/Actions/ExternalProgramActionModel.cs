﻿using MixItUp.Base.Model.Commands;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Actions
{
    [DataContract]
    public class ExternalProgramActionModel : ActionModelBase
    {
        [DataMember]
        public string FilePath { get; set; }

        [DataMember]
        public string Arguments { get; set; }

        [DataMember]
        public bool ShowWindow { get; set; }
        [DataMember]
        public bool WaitForFinish { get; set; }

        public ExternalProgramActionModel(string filePath, string arguments, bool showWindow, bool waitForFinish)
            : base(ActionTypeEnum.ExternalProgram)
        {
            this.FilePath = filePath;
            this.Arguments = arguments;
            this.ShowWindow = showWindow;
            this.WaitForFinish = waitForFinish;
        }

        internal ExternalProgramActionModel(MixItUp.Base.Actions.ExternalProgramAction action)
            : base(ActionTypeEnum.ExternalProgram)
        {
            this.FilePath = action.FilePath;
            this.Arguments = action.Arguments;
            this.ShowWindow = action.ShowWindow;
            this.WaitForFinish = action.WaitForFinish;
        }

        protected override async Task PerformInternal(CommandParametersModel parameters)
        {
            Process process = new Process();
            process.StartInfo.FileName = this.FilePath;
            process.StartInfo.Arguments = await this.ReplaceStringWithSpecialModifiers(this.Arguments, parameters);
            process.StartInfo.CreateNoWindow = !this.ShowWindow;
            process.StartInfo.WindowStyle = (!this.ShowWindow) ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal;
            process.StartInfo.UseShellExecute = false;

            process.Start();
            if (this.WaitForFinish)
            {
                while (!process.HasExited)
                {
                    await Task.Delay(500);
                }
            }
        }
    }
}
