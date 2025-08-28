namespace ListenBrainz.Models
{
    public class ListenBrainzLookup
    {
        public string artist_credit_name { get; set; }
        public string[] artist_mbids { get; set; }
        public string recording_mbid { get; set; }
        public string recording_name { get; set; }
        public string release_mbid { get; set; }
        public string release_name { get; set; }
    }
}
