namespace ListenBrainz
{
    using Api;
    using Models;
    using MediaBrowser.Common.Net;
    using MediaBrowser.Controller.Entities;
    using MediaBrowser.Controller.Entities.Audio;
    using MediaBrowser.Controller.Library;
    using MediaBrowser.Controller.Plugins;
    using MediaBrowser.Controller.Session;
    using MediaBrowser.Model.Logging;
    using MediaBrowser.Model.Serialization;
    using System.Linq;
    using System;
    using MediaBrowser.Common.Configuration;

    /// <summary>
    /// Class ServerEntryPoint
    /// </summary>
    public class ServerEntryPoint : IServerEntryPoint
    {
        private readonly ISessionManager _sessionManager;
        private readonly IUserDataManager _userDataManager;

        private ListenBrainzApiClient _apiClient;

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static ServerEntryPoint Instance { get; private set; }

        public ServerEntryPoint(ISessionManager sessionManager, IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogManager logManager, IUserDataManager userDataManager)
        {
            Plugin.Logger = logManager.GetLogger(Plugin.Instance.Name);

            _sessionManager = sessionManager;
            _userDataManager = userDataManager;

            _apiClient = new ListenBrainzApiClient(httpClient, jsonSerializer);

            Instance = this;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public void Run()
        {
            //Bind events
            _sessionManager.PlaybackStart += PlaybackStart;
            _sessionManager.PlaybackStopped += PlaybackStopped;
        }

        /// <summary>
        /// Let ListenBrainz know when a track has finished.
        /// Playback stopped is run when a track is finished.
        /// </summary>
        private async void PlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            //We only care about audio
            if (!(e.Item is Audio))
                return;

            var item = e.Item as Audio;

            //Make sure the track has been fully played
            if (!e.PlayedToCompletion)
            {
                return;
            }

            //Played to completion will sometimes be true even if the track has only played 10% so check the playback ourselfs (it must use the app settings or something)
            //Make sure 80% of the track has been played back
            if (e.PlaybackPositionTicks == null)
            {
                Plugin.Logger.Debug("Playback ticks for {0} is null", item.Name);
                return;
            }

            var playPercent = ((double)e.PlaybackPositionTicks / item.RunTimeTicks) * 100;
            if (playPercent < 80)
            {
                Plugin.Logger.Debug("'{0}' only played {1}%, not scrobbling", item.Name, playPercent);
                return;
            }

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                Plugin.Logger.Info("No title present, aborting");
                return;
            }

            if (item.Artists.Length == 0)
            {
                Plugin.Logger.Info("No artist present, aborting");
                return;
            }

            var user = e.Users.FirstOrDefault();
            if (user == null)
            {
                return;
            }

            var listenBrainzUser = GetUser(user);
            if (listenBrainzUser == null)
            {
                return;
            }

            //User doesn't want to scrobble
            if (!listenBrainzUser.Scrobble)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(listenBrainzUser.SessionKey))
            {
                return;
            }

            if (!user.IsGrantedAccessToFeature(Feature.StaticId))
            {
                return;
            }

            try
            {
                await _apiClient.Scrobble(item, listenBrainzUser).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Plugin.Logger.ErrorException("Error scrobbling to ListenBrainz", ex);
            }
        }

        /// <summary>
        /// Let ListenBrainz know when a user has started listening to a track
        /// </summary>
        private async void PlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            //We only care about audio
            if (!(e.Item is Audio))
                return;

            var item = e.Item as Audio;

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                Plugin.Logger.Info("No title present, aborting");
                return;
            }

            if (item.Artists.Length == 0)
            {
                Plugin.Logger.Info("No artist present, aborting");
                return;
            }

            var user = e.Users.FirstOrDefault();
            if (user == null)
            {
                return;
            }

            var listenBrainzUser = GetUser(user);
            if (listenBrainzUser == null)
            {
                return;
            }

            //User doesn't want to scrobble
            if (!listenBrainzUser.Scrobble)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(listenBrainzUser.SessionKey))
            {
                return;
            }

            if (!user.IsGrantedAccessToFeature(Feature.StaticId))
            {
                return;
            }

            try
            {
                await _apiClient.NowPlaying(item, listenBrainzUser).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Plugin.Logger.ErrorException("Error reporting playback start to ListenBrainz", ex);
            }
        }

        private ListenBrainzUser GetUser(User user)
        {
            return (ListenBrainzUser)user.GetTypedSetting(ConfigurationFactory.ConfigKey);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            //Unbind events
            _sessionManager.PlaybackStart -= PlaybackStart;
            _sessionManager.PlaybackStopped -= PlaybackStopped;

            //Clean up
            _apiClient = null;

        }
    }
}
