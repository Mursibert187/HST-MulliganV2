using System;
using System.Collections.Generic;
using System.Net.Http;
using HstMulligan.Core.Data;
using HstMulligan.Plugin.Config;

namespace HstMulligan.Plugin.Bindings
{
    internal static class SourcePipelineFactory
    {
        public static IMulliganDataSource Build(PluginSettings settings, HttpClient http)
        {
            var sources = new List<IMulliganDataSource>();

            // Pinned-deck feed takes priority when configured, because it's
            // the only source that can deliver per-this-exact-deck stats.
            if (!string.IsNullOrWhiteSpace(settings.PinnedDeckShortId)
                && !string.IsNullOrWhiteSpace(settings.PinnedDeckUrlTemplate))
            {
                sources.Add(new HsReplayDataSource(http, settings.PinnedDeckUrlTemplate));
            }

            if (!string.IsNullOrEmpty(settings.OfflineDatasetPath))
                sources.Add(new FileDataSource(settings.OfflineDatasetPath));

            switch ((settings.PrimarySource ?? "firestone").ToLowerInvariant())
            {
                case "hsreplay":
                    sources.Add(new HsReplayDataSource(http, settings.HsReplayUrlTemplate));
                    sources.Add(new FirestoneDataSource(http, settings.FirestoneUrlTemplate));
                    break;
                case "file":
                    break;
                default:
                    sources.Add(new FirestoneDataSource(http, settings.FirestoneUrlTemplate));
                    sources.Add(new HsReplayDataSource(http, settings.HsReplayUrlTemplate));
                    break;
            }

            IMulliganDataSource pipeline = sources.Count == 1
                ? sources[0]
                : new CompositeDataSource(sources);

            var ttl = TimeSpan.FromMinutes(Math.Max(1, settings.CacheTtlMinutes));
            return new CachingDataSource(pipeline, ttl);
        }
    }
}
