namespace ListenBrainz.Api
{
    using MediaBrowser.Common.Net;
    using MediaBrowser.Controller.Entities;
    using MediaBrowser.Controller.Entities.Audio;
    using MediaBrowser.Model.Serialization;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;

    public class ListenBrainzApiClient
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _json;

        public ListenBrainzApiClient(IHttpClient httpClient, IJsonSerializer jsonSerializer)
        {
            _httpClient = httpClient;
            _json = jsonSerializer;
        }

        public async Task<ListenBrainzLookup> LookupMetadata(Audio item)
        {
            var artist_name = HttpUtility.UrlEncode(item.Artists.FirstOrDefault());
            var recording_name = HttpUtility.UrlEncode(item.Name);
            var release_name = HttpUtility.UrlEncode(item.Album);

            var options = new HttpRequestOptions
            {
                Url = $"https://api.listenbrainz.org/1/metadata/lookup?artist_name={artist_name}&recording_name={recording_name}&release_name={release_name}",
                CancellationToken = CancellationToken.None,
            };

            using (var response = await _httpClient.GetResponse(options).ConfigureAwait(false))
            {
                var lookup = _json.DeserializeFromStream<ListenBrainzLookup>(response.Content);
                return lookup;
            }
        }

        public async Task Feedback(Audio item, User user)
        {
            var lookup = await LookupMetadata(item).ConfigureAwait(false);
            var recording_mbid = lookup.recording_mbid;

            if (string.IsNullOrEmpty(recording_mbid))
            {
                Plugin.Logger.Debug("Failed to get recording_mbid: {0}", item.Name);
                return;
            }

            var payload = _json.SerializeToString(new
            {
                recording_mbid = recording_mbid,
                score = item.IsFavoriteOrLiked(user) ? 1 : 0
            }).ToCharArray();

            var options = new HttpRequestOptions
            {
                Url = "https://api.listenbrainz.org/1/feedback/recording-feedback",
                CancellationToken = CancellationToken.None,
                RequestContentType = "application/json",
                RequestContent = payload,
            };

            var sessionKey = ServerEntryPoint.Instance.GetUser(user).SessionKey;
            if (string.IsNullOrEmpty(sessionKey))
                return;

            options.RequestHeaders.Add("Authorization", $"Token {sessionKey}");
            using (var response = await _httpClient.Post(options).ConfigureAwait(false))
            {

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Plugin.Logger.Info("Sent track feedback: {0}", item.Name);
                    return;
                }

                Plugin.Logger.Debug("Failed to send track feedback: {0}", item.Name);
            }
        }

        public async Task Scrobble(Audio item, ListenBrainzUser user)
        {
            var payload = BuildPayload("single", item);
            var options = BuildRequest(user, payload);
            using (var response = await _httpClient.Post(options).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Plugin.Logger.Info("{0} played '{1}' - {2} - {3}", user.Username, item.Name, item.Album, item.Artists.FirstOrDefault());
                    return;
                }

                Plugin.Logger.Debug("Failed to Scrobble track: {0}", item.Name);
            }
        }

        public async Task NowPlaying(Audio item, ListenBrainzUser user)
        {
            var payload = BuildPayload("playing_now", item);
            var options = BuildRequest(user, payload);
            using (var response = await _httpClient.Post(options).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Plugin.Logger.Info("{0} is now playing '{1}' - {2} - {3}", user.Username, item.Name, item.Album, item.Artists.FirstOrDefault());
                    return;
                }

                Plugin.Logger.Debug("Failed to send now playing for track: {0}", item.Name);
            }
        }

        private HttpRequestOptions BuildRequest(ListenBrainzUser user, char[] payload)
        {
            var options = new HttpRequestOptions
            {
                Url = "https://api.listenbrainz.org/1/submit-listens",
                CancellationToken = CancellationToken.None,
                RequestContentType = "application/json",
                RequestContent = payload,
            };

            options.RequestHeaders.Add("Authorization", $"Token {user.SessionKey}");

            return options;
        }

        private char[] BuildPayload(string type, Audio item)
        {
            var artistMbids = new List<string>();
            if (item.ProviderIds.ContainsKey("MusicBrainzAlbumArtist"))
                artistMbids.Add(item.ProviderIds["MusicBrainzAlbumArtist"]);
            if (item.ProviderIds.ContainsKey("MusicBrainzArtist"))
                artistMbids.Add(item.ProviderIds["MusicBrainzArtist"]);

            var additionalInfo = new Dictionary<string, object>();
            if (item.ProviderIds.ContainsKey("MusicBrainzAlbum"))
                additionalInfo.Add("release_mbid", item.ProviderIds["MusicBrainzAlbum"]);
            if (item.ProviderIds.ContainsKey("MusicBrainzTrack"))
                additionalInfo.Add("track_mbid", item.ProviderIds["MusicBrainzTrack"]);
            if (item.ProviderIds.ContainsKey("MusicBrainzReleaseGroup"))
                additionalInfo.Add("release_group_mbid", item.ProviderIds["MusicBrainzReleaseGroup"]);
            if (item.IndexNumber.HasValue)
                additionalInfo.Add("tracknumber", item.IndexNumber);
            if (artistMbids.Count > 0)
                additionalInfo.Add("artist_mbids", artistMbids);

            var trackMetadata = new Dictionary<string, object>
            {
                { "track_name", item.Name },
                { "artist_name", item.Artists.FirstOrDefault() }
            };
            if (!string.IsNullOrWhiteSpace(item.Album))
                trackMetadata.Add("release_name", item.Album);
            if (additionalInfo.Count > 0)
                trackMetadata.Add("additional_info", additionalInfo);

            var metadata = new Dictionary<string, object>
            {
                { "track_metadata", trackMetadata}
            };
            if (type.Equals("single", StringComparison.OrdinalIgnoreCase))
                metadata.Add("listened_at", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            return _json.SerializeToString(new
            {
                listen_type = type,
                payload = new[]
                {
                    metadata
                }
            }).ToCharArray();
        }

    }
}
