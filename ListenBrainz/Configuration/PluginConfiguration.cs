namespace ListenBrainz.Configuration
{
    using Models;
    using MediaBrowser.Model.Plugins;

    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public ListenBrainzUser[] ListenBrainzUsers { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration" /> class.
        /// </summary>
        public PluginConfiguration()
        {
            ListenBrainzUsers = new ListenBrainzUser[] { };
        }
    }
}
