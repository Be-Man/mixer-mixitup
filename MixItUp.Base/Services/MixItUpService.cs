﻿using MixItUp.Base.Model;
using MixItUp.Base.Model.Actions;
using MixItUp.Base.Model.API;
using MixItUp.Base.Model.Commands;
using MixItUp.Base.Model.Currency;
using MixItUp.Base.Model.Store;
using MixItUp.Base.Model.User;
using MixItUp.Base.Model.User.Twitch;
using MixItUp.Base.Model.Webhooks;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.Chat;
using MixItUp.Base.ViewModel.User;
using MixItUp.SignalR.Client;
using StreamingClient.Base.Model.OAuth;
using StreamingClient.Base.Services;
using StreamingClient.Base.Util;
using StreamingClient.Base.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace MixItUp.Base.Services
{
    public interface IMixItUpService
    {
        Task<MixItUpUpdateModel> GetLatestUpdate();
        Task<MixItUpUpdateModel> GetLatestPublicUpdate();
        Task<MixItUpUpdateModel> GetLatestPreviewUpdate();
        Task<MixItUpUpdateModel> GetLatestTestUpdate();

        Task SendIssueReport(IssueReportModel report);
    }

    public interface IWebhookService
    {
        bool IsWebhookHubConnected { get; }
        bool IsWebhookHubAllowed { get; }
        void BackgroundConnect();
        Task<Result> Connect();
        Task Disconnect();

        Task Authenticate(string twitchAccessToken);

        Task<GetWebhooksResponseModel> GetWebhooks();
        Task<Webhook> CreateWebhook();
        Task DeleteWebhook(Guid id);
    }

    public interface ICommunityCommandsService
    {
        Task<IEnumerable<CommunityCommandCategoryModel>> GetHomeCategories();
        Task<CommunityCommandsSearchResult> SearchCommands(string query, int skip, int top);
        Task<CommunityCommandDetailsModel> GetCommandDetails(Guid id);
        Task<CommunityCommandDetailsModel> AddOrUpdateCommand(CommunityCommandUploadModel command);
        Task DeleteCommand(Guid id);
        Task ReportCommand(CommunityCommandReportModel report);
        Task<CommunityCommandsSearchResult> GetCommandsByUser(Guid userID, int skip, int top);
        Task<CommunityCommandsSearchResult> GetMyCommands(int skip, int top);
        Task<CommunityCommandReviewModel> AddReview(CommunityCommandReviewModel review);
        Task DownloadCommand(Guid id);
    }

    public class CommunityCommandsSearchResult
    {
        public const string PageNumberHeader = "Page-Number";
        public const string PageSizeHeader = "Page-Size";
        public const string TotalElementsHeader = "Total-Elements";
        public const string TotalPagesHeader = "Total-Pages";

        public static async Task<CommunityCommandsSearchResult> Create(HttpResponseMessage response)
        {
            CommunityCommandsSearchResult result = new CommunityCommandsSearchResult();
            if (response.IsSuccessStatusCode)
            {
                result.Results.AddRange(await response.ProcessResponse<IEnumerable<CommunityCommandModel>>());

                if (int.TryParse(response.GetHeaderValue(PageNumberHeader), out int pageNumber))
                {
                    result.PageNumber = pageNumber;
                }
                if (int.TryParse(response.GetHeaderValue(PageSizeHeader), out int pageSize))
                {
                    result.PageSize = pageSize;
                }
                if (int.TryParse(response.GetHeaderValue(TotalElementsHeader), out int totalElements))
                {
                    result.TotalElements = totalElements;
                }
                if (int.TryParse(response.GetHeaderValue(TotalPagesHeader), out int totalPages))
                {
                    result.TotalPages = totalPages;
                }
            }
            return result;
        }

        public List<CommunityCommandModel> Results { get; set; } = new List<CommunityCommandModel>();

        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public int TotalElements { get; set; }
        public int TotalPages { get; set; }

        public CommunityCommandsSearchResult() { }

        public bool HasPreviousResults { get { return this.PageNumber > 1; } }

        public bool HasNextResults { get { return this.PageNumber < this.TotalPages; } }
    }

    public class MixItUpService : OAuthRestServiceBase, ICommunityCommandsService, IMixItUpService, IWebhookService, IDisposable
    {
        public const string MixItUpAPIEndpoint = "https://mixitupapi.azurewebsites.net/api/";
        public const string MixItUpSignalRHubEndpoint = "https://mixitupapi.azurewebsites.net/webhookhub";

        //public const string MixItUpAPIEndpoint = "https://localhost:44309/api/";                // Dev Endpoint
        //public const string MixItUpSignalRHubEndpoint = "https://localhost:44309/webhookhub";   // Dev Endpoint

        //public const string MixItUpAPIEndpoint = "https://9d71-98-97-49-144.ngrok.io/api/";                 // NGROK Endpoint
        //public const string MixItUpSignalRHubEndpoint = "https://9d71-98-97-49-144.ngrok.io/webhookhub";    // NGROK Endpoint

        private string accessToken = null;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public MixItUpService()
        {
            this.signalRConnection = new SignalRConnection(MixItUpSignalRHubEndpoint);

            this.signalRConnection.Listen("TwitchFollowEvent", (string followerId, string followerUsername, string followerDisplayName) =>
            {
                Logger.Log($"Webhook Event - Follow - {followerId} - {followerUsername}");
                var _ = this.TwitchFollowEvent(followerId, followerUsername, followerDisplayName);
            });

            this.signalRConnection.Listen("TwitchStreamStartedEvent", () =>
            {
                Logger.Log($"Webhook Event - Stream Start");
                var _ = this.TwitchStreamStartedEvent();
            });

            this.signalRConnection.Listen("TwitchStreamStoppedEvent", () =>
            {
                Logger.Log($"Webhook Event - Stream Stop");
                var _ = this.TwitchStreamStoppedEvent();
            });

            this.signalRConnection.Listen("TwitchChannelHypeTrainBegin", (int totalPoints, int levelPoints, int levelGoal) =>
            {
                Logger.Log($"Webhook Event - Hype Train Begin");
                var _ = this.TwitchChannelHypeTrainBegin(totalPoints, levelPoints, levelGoal);
            });

            //this.signalRConnection.Listen("TwitchChannelHypeTrainProgress", (int level, int totalPoints, int levelPoints, int levelGoal) =>
            //{
            //    var _ = this.TwitchChannelHypeTrainProgress(level, totalPoints, levelPoints, levelGoal);
            //});

            this.signalRConnection.Listen("TwitchChannelHypeTrainEnd", (int level, int totalPoints) =>
            {
                Logger.Log($"Webhook Event - Hype Train End - {level} - {totalPoints}");
                var _ = this.TwitchChannelHypeTrainEnd(level, totalPoints);
            });

            this.signalRConnection.Listen("TriggerWebhook", (Guid id, string payload) =>
            {
                Logger.Log($"Webhook Event - Generic Webhook - {id} - {payload}");
                var _ = this.TriggerGenericWebhook(id, payload);
            });

            this.signalRConnection.Listen("AuthenticationCompleteEvent", (bool approved) =>
            {
                this.IsWebhookHubAllowed = approved;
                if (!this.IsWebhookHubAllowed)
                {
                    // Force disconnect is it doesn't retry
                    var _ = this.Disconnect();
                }
            });
        }

        // IMixItUpService
        public async Task<MixItUpUpdateModel> GetLatestUpdate()
        {
            MixItUpUpdateModel update = await ChannelSession.Services.MixItUpService.GetLatestPublicUpdate();
            if (update != null)
            {
                if (ChannelSession.AppSettings.PreviewProgram)
                {
                    MixItUpUpdateModel previewUpdate = await ChannelSession.Services.MixItUpService.GetLatestPreviewUpdate();
                    if (previewUpdate != null && previewUpdate.SystemVersion >= update.SystemVersion)
                    {
                        update = previewUpdate;
                    }
                }

                // Remove this when we wish to re-enable Test Builds
                ChannelSession.AppSettings.TestBuild = false;

                if (ChannelSession.AppSettings.TestBuild)
                {
                    MixItUpUpdateModel testUpdate = await ChannelSession.Services.MixItUpService.GetLatestTestUpdate();
                    if (testUpdate != null && testUpdate.SystemVersion >= update.SystemVersion)
                    {
                        update = testUpdate;
                    }
                }
            }
            return update;
        }
        public async Task<MixItUpUpdateModel> GetLatestPublicUpdate() { return await this.GetAsync<MixItUpUpdateModel>("updates"); }
        public async Task<MixItUpUpdateModel> GetLatestPreviewUpdate() { return await this.GetAsync<MixItUpUpdateModel>("updates/preview"); }
        public async Task<MixItUpUpdateModel> GetLatestTestUpdate() { return await this.GetAsync<MixItUpUpdateModel>("updates/test"); }

        public async Task SendIssueReport(IssueReportModel report)
        {
            string content = JSONSerializerHelper.SerializeToString(report);
            var response = await this.PostAsync("issuereport", new StringContent(content, Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
            {
                string resultContent = await response.Content.ReadAsStringAsync();
                Logger.Log(resultContent);
            }
        }

        // ICommunityCommandsService
        public async Task<IEnumerable<CommunityCommandCategoryModel>> GetHomeCategories()
        {
            await EnsureLogin();
            return await GetAsync<IEnumerable<CommunityCommandCategoryModel>>("community/commands/categories");
        }

        public async Task<CommunityCommandsSearchResult> SearchCommands(string query, int skip, int top)
        {
            await EnsureLogin();
            return await CommunityCommandsSearchResult.Create(await this.GetAsync($"community/commands/command/search?query={HttpUtility.UrlEncode(query)}&skip={skip}&top={top}"));
        }

        public async Task<CommunityCommandDetailsModel> GetCommandDetails(Guid id)
        {
            try
            {
                await EnsureLogin();
                return await GetAsync<CommunityCommandDetailsModel>($"community/commands/command/{id}");
            }
            catch (HttpRestRequestException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<CommunityCommandDetailsModel> AddOrUpdateCommand(CommunityCommandUploadModel command)
        {
            await EnsureLogin();
            return await PostAsync<CommunityCommandDetailsModel>("community/commands/command", AdvancedHttpClient.CreateContentFromObject(command));
        }

        public async Task DeleteCommand(Guid id)
        {
            await EnsureLogin();
            await DeleteAsync<CommunityCommandDetailsModel>($"community/commands/command/{id}/delete");
        }

        public async Task ReportCommand(CommunityCommandReportModel report)
        {
            await EnsureLogin();
            await PostAsync($"community/commands/command/{report.CommandID}/report", AdvancedHttpClient.CreateContentFromObject(report));
        }

        public async Task<CommunityCommandsSearchResult> GetCommandsByUser(Guid userID, int skip, int top)
        {
            await EnsureLogin();
            return await CommunityCommandsSearchResult.Create(await GetAsync($"community/commands/command/user/{userID}?skip={skip}&top={top}"));
        }

        public async Task<CommunityCommandsSearchResult> GetMyCommands(int skip, int top)
        {
            await EnsureLogin();
            return await CommunityCommandsSearchResult.Create(await GetAsync($"community/commands/command/mine?skip={skip}&top={top}"));
        }

        public async Task<CommunityCommandReviewModel> AddReview(CommunityCommandReviewModel review)
        {
            await EnsureLogin();
            return await PostAsync<CommunityCommandReviewModel>($"community/commands/command/{review.CommandID}/review", AdvancedHttpClient.CreateContentFromObject(review));
        }

        public async Task DownloadCommand(Guid id)
        {
            try
            {
                await EnsureLogin();
                await GetAsync<IEnumerable<CommunityCommandDetailsModel>>($"community/commands/command/{id}/download");
            }
            catch { }
        }

        protected override Task<OAuthTokenModel> GetOAuthToken(bool autoRefreshToken = true)
        {
            return Task.FromResult(new OAuthTokenModel { accessToken = this.accessToken });
        }

        protected override string GetBaseAddress() => MixItUpService.MixItUpAPIEndpoint;

        private async Task EnsureLogin()
        {
            if (accessToken == null)
            {
                var twitchUserOAuthToken = ChannelSession.TwitchUserConnection.Connection.GetOAuthTokenCopy();
                var login = new CommunityCommandLoginModel
                {
                    TwitchAccessToken = twitchUserOAuthToken?.accessToken,
                };

                var loginResponse = await PostAsync<CommunityCommandLoginResponseModel>("user/login", AdvancedHttpClient.CreateContentFromObject(login));
                this.accessToken = loginResponse.AccessToken;
            }
        }

        // IWebhookService
        public const string AuthenticateMethodName = "Authenticate";
        private readonly SignalRConnection signalRConnection;
        public bool IsWebhookHubConnected { get { return this.signalRConnection.IsConnected(); } }
        public bool IsWebhookHubAllowed { get; private set; } = false;

        public void BackgroundConnect()
        {
            AsyncRunner.RunAsyncBackground(async (cancellationToken) =>
            {
                Result result = await this.Connect();
                if (!result.Success)
                {
                    SignalRConnection_Disconnected(this, new Exception());
                }
            }, new CancellationToken());
        }

        public async Task<Result> Connect()
        {
            if (!this.IsWebhookHubConnected)
            {
                this.signalRConnection.Connected -= SignalRConnection_Connected;
                this.signalRConnection.Disconnected -= SignalRConnection_Disconnected;

                this.signalRConnection.Connected += SignalRConnection_Connected;
                this.signalRConnection.Disconnected += SignalRConnection_Disconnected;

                if (await this.signalRConnection.Connect())
                {
                    return new Result(this.IsWebhookHubConnected);
                }
                return new Result(MixItUp.Base.Resources.WebhooksServiceFailedConnection);
            }
            return new Result(MixItUp.Base.Resources.WebhookServiceAlreadyConnected);
        }

        public async Task Disconnect()
        {
            this.signalRConnection.Connected -= SignalRConnection_Connected;
            this.signalRConnection.Disconnected -= SignalRConnection_Disconnected;

            await this.signalRConnection.Disconnect();
        }

        private async void SignalRConnection_Connected(object sender, EventArgs e)
        {
            ChannelSession.ReconnectionOccurred(MixItUp.Base.Resources.WebhookEvents);

            var twitchUserOAuthToken = ChannelSession.TwitchUserConnection.Connection.GetOAuthTokenCopy();
            await this.Authenticate(twitchUserOAuthToken?.accessToken);
        }

        private async void SignalRConnection_Disconnected(object sender, Exception e)
        {
            ChannelSession.DisconnectionOccurred(MixItUp.Base.Resources.WebhookEvents);

            Result result = new Result();
            do
            {
                await this.Disconnect();

                await Task.Delay(5000 + RandomHelper.GenerateRandomNumber(5000));

                result = await this.Connect();
            }
            while (!result.Success);
        }

        public async Task Authenticate(string twitchAccessToken)
        {
            await this.AsyncWrapper(this.signalRConnection.Send(AuthenticateMethodName, twitchAccessToken));
        }

        public async Task<GetWebhooksResponseModel> GetWebhooks()
        {
            await EnsureLogin();
            return await GetAsync<GetWebhooksResponseModel>($"webhook");
        }

        public async Task<Webhook> CreateWebhook()
        {
            await EnsureLogin();
            return await PostAsync<Webhook>($"webhook", AdvancedHttpClient.CreateContentFromObject(new {}));
        }

        public async Task DeleteWebhook(Guid id)
        {
            await EnsureLogin();
            await DeleteAsync($"webhook/{id}");
        }

        private async Task AsyncWrapper(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception ex) { Logger.Log(ex); }
        }

        private async Task TwitchFollowEvent(string followerId, string followerUsername, string followerDisplayName)
        {
            UserViewModel user = ChannelSession.Services.User.GetActiveUserByPlatformID(StreamingPlatformTypeEnum.Twitch, followerId);
            if (user == null)
            {
                user = await UserViewModel.Create(new TwitchWebhookFollowModel()
                {
                    StreamerID = ChannelSession.TwitchUserNewAPI.id,

                    UserID = followerId,
                    Username = followerUsername,
                    UserDisplayName = followerDisplayName
                });
            }

            ChannelSession.Services.Events.TwitchEventService.FollowCache.Add(user.TwitchID);

            if (user.UserRoles.Contains(UserRoleEnum.Banned))
            {
                return;
            }

            CommandParametersModel parameters = new CommandParametersModel(user);
            if (ChannelSession.Services.Events.CanPerformEvent(EventTypeEnum.TwitchChannelFollowed, parameters))
            {
                user.FollowDate = DateTimeOffset.Now;

                ChannelSession.Settings.LatestSpecialIdentifiersData[SpecialIdentifierStringBuilder.LatestFollowerUserData] = user.ID;

                foreach (CurrencyModel currency in ChannelSession.Settings.Currency.Values)
                {
                    currency.AddAmount(user.Data, currency.OnFollowBonus);
                }

                foreach (StreamPassModel streamPass in ChannelSession.Settings.StreamPass.Values)
                {
                    if (user.HasPermissionsTo(streamPass.Permission))
                    {
                        streamPass.AddAmount(user.Data, streamPass.FollowBonus);
                    }
                }

                await ChannelSession.Services.Alerts.AddAlert(new AlertChatMessageViewModel(StreamingPlatformTypeEnum.Twitch, user, string.Format("{0} Followed", user.FullDisplayName), ChannelSession.Settings.AlertFollowColor));

                await ChannelSession.Services.Events.PerformEvent(EventTypeEnum.TwitchChannelFollowed, parameters);

                GlobalEvents.FollowOccurred(user);
            }
        }

        private async Task TwitchStreamStartedEvent()
        {
            await ChannelSession.Services.Events.PerformEvent(EventTypeEnum.TwitchChannelStreamStart, new CommandParametersModel());
        }

        private async Task TwitchStreamStoppedEvent()
        {
            await ChannelSession.Services.Events.PerformEvent(EventTypeEnum.TwitchChannelStreamStop, new CommandParametersModel());
        }

        private async Task TwitchChannelHypeTrainBegin(int totalPoints, int levelPoints, int levelGoal)
        {
            Dictionary<string, string> eventCommandSpecialIdentifiers = new Dictionary<string, string>();
            eventCommandSpecialIdentifiers["hypetraintotalpoints"] = totalPoints.ToString();
            eventCommandSpecialIdentifiers["hypetrainlevelpoints"] = levelPoints.ToString();
            eventCommandSpecialIdentifiers["hypetrainlevelgoal"] = levelGoal.ToString();
            await ChannelSession.Services.Events.PerformEvent(EventTypeEnum.TwitchChannelHypeTrainBegin, new CommandParametersModel(ChannelSession.GetCurrentUser(), eventCommandSpecialIdentifiers));

            await ChannelSession.Services.Alerts.AddAlert(new AlertChatMessageViewModel(StreamingPlatformTypeEnum.Twitch, MixItUp.Base.Resources.HypeTrainStarted, ChannelSession.Settings.AlertHypeTrainColor));
        }

        //private async Task TwitchChannelHypeTrainProgress(int level, int totalPoints, int levelPoints, int levelGoal)
        //{
        //    Dictionary<string, string> eventCommandSpecialIdentifiers = new Dictionary<string, string>();
        //    eventCommandSpecialIdentifiers["hypetraintotallevel"] = level.ToString();
        //    eventCommandSpecialIdentifiers["hypetraintotalpoints"] = totalPoints.ToString();
        //    eventCommandSpecialIdentifiers["hypetrainlevelpoints"] = levelPoints.ToString();
        //    eventCommandSpecialIdentifiers["hypetrainlevelgoal"] = levelGoal.ToString();

        //    EventTrigger trigger = new EventTrigger(EventTypeEnum.TwitchChannelHypeTrainProgress, ChannelSession.GetCurrentUser(), eventCommandSpecialIdentifiers);
        //    if (ChannelSession.Services.Events.CanPerformEvent(trigger))
        //    {
        //        await ChannelSession.Services.Events.PerformEvent(trigger);
        //    }
        //}

        private async Task TwitchChannelHypeTrainEnd(int level, int totalPoints)
        {
            Dictionary<string, string> eventCommandSpecialIdentifiers = new Dictionary<string, string>();
            eventCommandSpecialIdentifiers["hypetraintotallevel"] = level.ToString();
            eventCommandSpecialIdentifiers["hypetraintotalpoints"] = totalPoints.ToString();
            await ChannelSession.Services.Events.PerformEvent(EventTypeEnum.TwitchChannelHypeTrainEnd, new CommandParametersModel(ChannelSession.GetCurrentUser(), eventCommandSpecialIdentifiers));

            await ChannelSession.Services.Alerts.AddAlert(new AlertChatMessageViewModel(StreamingPlatformTypeEnum.Twitch, string.Format(MixItUp.Base.Resources.HypeTrainEndedReachedLevel, level.ToString()), ChannelSession.Settings.AlertHypeTrainColor));
        }

        private async Task TriggerGenericWebhook(Guid id, string payload)
        {
            try
            {
                var command = ChannelSession.Services.Command.WebhookCommands.FirstOrDefault(c => c.ID == id);
                if (command != null && command.IsEnabled)
                {
                    if (string.IsNullOrEmpty(payload))
                    {
                        payload = "{}";
                    }

                    Dictionary<string, string> eventCommandSpecialIdentifiers = new Dictionary<string, string>();
                    eventCommandSpecialIdentifiers["webhookpayload"] = payload;

                    // Do JSON => Special Identifier logic
                    CommandParametersModel parameters = new CommandParametersModel(ChannelSession.GetCurrentUser(), eventCommandSpecialIdentifiers);
                    Dictionary<string, string> jsonParameters = command.JSONParameters.ToDictionary(param => param.JSONParameterName, param => param.SpecialIdentifierName);
                    await WebRequestActionModel.ProcessJSONToSpecialIdentifiers(payload, jsonParameters, parameters);

                    await ChannelSession.Services.Command.Queue(command, parameters);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    this.cancellationTokenSource.Dispose();
                }

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                // Set large fields to null.

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
