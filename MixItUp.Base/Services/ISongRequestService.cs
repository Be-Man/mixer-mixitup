﻿using MixItUp.Base.ViewModel.User;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MixItUp.Base.Services
{
    public enum SongRequestStateEnum
    {
        NotStarted = 0,
        Playing = 1,
        Paused = 2,
        Ended = 3,
    }

    public enum SongRequestServiceTypeEnum
    {
        Spotify,
        YouTube,
        [Obsolete]
        SoundCloud,

        All = 10
    }

    public class SongRequestItem
    {
        public string ID { get; set; }
        public string Name { get; set; }

        public SongRequestServiceTypeEnum Type { get; set; }
        public UserViewModel User { get; set; }

        public SongRequestStateEnum State { get; set; }
        public long Progress { get; set; }
        public long Length { get; set; }
    }

    public interface ISongRequestService
    {
        bool IsEnabled { get; }

        Task<bool> Initialize();

        Task Disable();

        Task AddSongRequest(UserViewModel user, SongRequestServiceTypeEnum service, string identifier, bool pickFirst = false);
        Task RemoveSongRequest(SongRequestItem song);
        Task RemoveLastSongRequestedByUser(UserViewModel user);

        Task PlayPauseCurrentSong();
        Task SkipToNextSong();
        Task RefreshVolume();

        Task<SongRequestItem> GetCurrentlyPlaying();
        Task<SongRequestItem> GetNextTrack();

        Task<IEnumerable<SongRequestItem>> GetAllRequests();
        Task ClearAllRequests();

        Task StatusUpdate(SongRequestItem item);
    }
}
