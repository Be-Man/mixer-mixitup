﻿using MixItUp.Base.Services.External;
using System.Windows.Input;

namespace MixItUp.Base.ViewModel.Controls.Services
{
    public class OvrStreamServiceControlViewModel : ServiceControlViewModelBase
    {
        public string OvrStreamAddress
        {
            get { return this.ovrStreamAddress; }
            set
            {
                this.ovrStreamAddress = value;
                this.NotifyPropertyChanged();
            }
        }
        private string ovrStreamAddress;

        public ICommand ConnectCommand { get; set; }
        public ICommand DisconnectCommand { get; set; }

        public OvrStreamServiceControlViewModel()
            : base("OvrStream")
        {
            this.OvrStreamAddress = ChannelSession.DefaultOvrStreamConnection;

            this.ConnectCommand = this.CreateCommand(async (parameter) =>
            {
                ChannelSession.Settings.OvrStreamServerIP = this.OvrStreamAddress;

                ExternalServiceResult result = await ChannelSession.Services.OvrStream.Connect();
                if (result.Success)
                {
                    this.IsConnected = true;
                }
                else
                {
                    await this.ShowConnectFailureMessage(result);
                }
            });

            this.DisconnectCommand = this.CreateCommand(async (parameter) =>
            {
                await ChannelSession.Services.OvrStream.Disconnect();
                this.IsConnected = false;
            });

            this.IsConnected = ChannelSession.Services.OvrStream.IsConnected;
        }
    }
}
