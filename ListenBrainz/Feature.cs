using Emby.Features;
using System;
using System.Collections.Generic;
using System.Text;

namespace ListenBrainz
{
    public class Feature : IFeatureFactory
    {
        public const string StaticId = "listenbrainz";

        public List<FeatureInfo> GetFeatureInfos(string language)
        {
            return new List<FeatureInfo>
            {
                new FeatureInfo
                {
                    Id = StaticId,
                    Name = Plugin.StaticName,
                    FeatureType = FeatureType.User
                }
            };
        }
    }
}
