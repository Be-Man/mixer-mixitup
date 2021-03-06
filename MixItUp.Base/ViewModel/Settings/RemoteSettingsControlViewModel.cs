﻿using MixItUp.Base.Model.Remote.Authentication;
using MixItUp.Base.Util;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MixItUp.Base.ViewModel.Settings
{
    public class RemoteConnectionUIViewModel : ControlViewModelBase
    {
        public const string StreamerDeviceType = "Streamer";
        public const string NormalDeviceType = "Normal";

        public RemoteConnectionModel Connection { get; private set; }

        public string Name { get { return this.Connection.Name; } }

        public string Status { get { return this.Connection.IsTemporary ? "1-Time" : "Permanent"; } }

        public IEnumerable<string> DeviceTypes { get { return new List<string>() { NormalDeviceType, StreamerDeviceType }; } }

        public string DeviceType
        {
            get { return (this.Connection.IsStreamer) ? StreamerDeviceType : NormalDeviceType; }
            set
            {
                if (!string.IsNullOrEmpty(value) && value.Equals(StreamerDeviceType))
                {
                    this.Connection.IsStreamer = true;
                }
                else
                {
                    this.Connection.IsStreamer = false;
                }
                this.NotifyPropertyChanged();
            }
        }

        public ICommand DeleteCommand { get; set; }

        private RemoteSettingsControlViewModel parent;

        public RemoteConnectionUIViewModel(RemoteSettingsControlViewModel parent, RemoteConnectionModel connection)
        {
            this.parent = parent;
            this.Connection = connection;

            this.DeleteCommand = this.CreateCommand(async (parameter) =>
            {
                await ChannelSession.Services.RemoteService.RemoveClient(ChannelSession.Settings.RemoteHostConnection, this.Connection);
                ChannelSession.Settings.RemoteClientConnections.Remove(this.Connection);
                this.parent.RefreshList();
            });
        }
    }

    public class RemoteSettingsControlViewModel : ControlViewModelBase
    {
        public ThreadSafeObservableCollection<RemoteConnectionUIViewModel> Connections { get; set; } = new ThreadSafeObservableCollection<RemoteConnectionUIViewModel>();

        public RemoteSettingsControlViewModel()
        {
            this.RefreshList();
        }

        protected override Task OnVisibleInternal()
        {
            this.RefreshList();
            return Task.FromResult(0);
        }

        public void RefreshList()
        {
            this.Connections.ClearAndAddRange(ChannelSession.Settings.RemoteClientConnections.Select(c => new RemoteConnectionUIViewModel(this, c)));
        }
    }
}
