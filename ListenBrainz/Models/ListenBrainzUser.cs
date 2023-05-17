namespace ListenBrainz.Models
{
    using MediaBrowser.Common.Configuration;
    using MediaBrowser.Controller.Configuration;
    using MediaBrowser.Controller.Entities;
    using System;
    using System.Collections.Generic;

    public class ListenBrainzUser
    {
        public string Username { get; set; }

        //We wont store the password, but instead store the session key since its a lifetime key
        public string SessionKey { get; set; }

        public bool Scrobble { get; set; } = true;
    }

    public class ConfigurationFactory : IUserConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new[]
            {
                new TraktConfigStore
                {
                     ConfigurationType = typeof(ListenBrainzUser),
                     Key = ConfigKey
                }
            };
        }

        public static string ConfigKey = Feature.StaticId;
    }

    public class TraktConfigStore : ConfigurationStore, IValidatingConfiguration
    {
        public void Validate(object oldConfig, object newConfig)
        {
        }
    }
}
