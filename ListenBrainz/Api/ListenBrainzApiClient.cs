namespace ListenBrainz.Api
{
    using MediaBrowser.Common.Net;
    using MediaBrowser.Controller.Entities.Audio;
    using MediaBrowser.Model.Serialization;
    using Models;
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    public class ListenBrainzApiClient
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _json;

        public ListenBrainzApiClient(IHttpClient httpClient, IJsonSerializer jsonSerializer)
        {
            _httpClient = httpClient;
            _json = jsonSerializer;
        }

        public async Task Scrobble(Audio item, ListenBrainzUser user)
        {
            var payload = BuildPayload("single", item);
            var options = BuildRequest(user, payload);
            var response = await _httpClient.Post(options);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Plugin.Logger.Info("{0} played '{1}' - {2} - {3}", user.Username, item.Name, item.Album, item.Artists.First());
                return;
            }

            Plugin.Logger.Debug("Failed to Scrobble track: {0}", item.Name);
        }

        public async Task NowPlaying(Audio item, ListenBrainzUser user)
        {
            var payload = BuildPayload("playing_now", item);
            var options = BuildRequest(user, payload);
            var response = await _httpClient.Post(options);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Plugin.Logger.Info("{0} is now playing '{1}' - {2} - {3}", user.Username, item.Name, item.Album, item.Artists.First());
                return;
            }

            Plugin.Logger.Debug("Failed to send now playing for track: {0}", item.Name);
        }

        private HttpRequestOptions BuildRequest(ListenBrainzUser user, char[] payload)
        {
            var options = new HttpRequestOptions
            {
                Url = "https://api.listenbrainz.org/1/submit-listens",
                CancellationToken = CancellationToken.None,
                EnableHttpCompression = false,
                RequestContentType = "application/json",
                RequestContent = payload,
            };

            options.RequestHeaders.Add("Authorization", $"Token {user.SessionKey}");

            return options;
        }

        private char[] BuildPayload(string type, Audio item)
        {
            var metadata = new
            {
                track_name = item.Name,
                release_name = item.Album,
                artist_name = item.Artists.First()
            };

            var newPayload = new object();
            if (type.Equals("playing_now", StringComparison.OrdinalIgnoreCase))
            {
                newPayload = new[]
                {
                    new
                    {
                        track_metadata = metadata
                    }
                };
            }
            else if (type.Equals("single", StringComparison.OrdinalIgnoreCase))
            {
                newPayload = new[]
                {
                    new
                    {
                        listened_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        track_metadata = metadata
                    }
                };
            }

            return _json.SerializeToString(new
            {
                listen_type = type,
                payload = newPayload
            }).ToCharArray();
        }

    }
}
