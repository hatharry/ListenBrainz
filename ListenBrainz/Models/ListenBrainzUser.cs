namespace ListenBrainz.Models
{
    using System;

    public class ListenBrainzUser
    {
        public string Username { get; set; }

        //We wont store the password, but instead store the session key since its a lifetime key
        public string SessionKey { get; set; }

        public Guid MediaBrowserUserId { get; set; }

        public ListenBrainzUserOptions Options { get; set; }
    }

    public class ListenBrainzUserOptions
    {
        public bool Scrobble { get; set; }
    }
}
