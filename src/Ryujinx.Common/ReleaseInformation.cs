using Ryujinx.Common.Utilities;
using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Ryujinx.Common
{
    // DO NOT EDIT, filled by CI
    public static class ReleaseInformation
    {
        private const string CanaryChannel = "canary";
        private const string ReleaseChannel = "release";

        private const string BuildVersion = "%%RYUJINX_BUILD_VERSION%%";
        public const string BuildGitHash = "%%RYUJINX_BUILD_GIT_HASH%%";
        private const string ReleaseChannelName = "%%RYUJINX_TARGET_RELEASE_CHANNEL_NAME%%";
        private const string ConfigFileName = "%%RYUJINX_CONFIG_FILE_NAME%%";

        public const string ReleaseChannelOwner = "%%RYUJINX_TARGET_RELEASE_CHANNEL_OWNER%%";
        public const string ReleaseChannelSourceRepo = "%%RYUJINX_TARGET_RELEASE_CHANNEL_SOURCE_REPO%%";
        public const string ReleaseChannelRepo = "%%RYUJINX_TARGET_RELEASE_CHANNEL_REPO%%";

        public static string ConfigName => !ConfigFileName.StartsWith("%%") ? ConfigFileName : "Config.json";

        public static bool IsValid =>
            !BuildGitHash.StartsWith("%%") &&
            !ReleaseChannelName.StartsWith("%%") &&
            !ReleaseChannelOwner.StartsWith("%%") &&
            !ReleaseChannelSourceRepo.StartsWith("%%") &&
            !ReleaseChannelRepo.StartsWith("%%") &&
            !ConfigFileName.StartsWith("%%");

        public static bool IsCanaryBuild => IsValid && ReleaseChannelName.Equals(CanaryChannel);
        
        public static bool IsReleaseBuild => IsValid && ReleaseChannelName.Equals(ReleaseChannel);

        public static string Version => IsValid ? BuildVersion : Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        public static string GetChangelogUrl(Version currentVersion, Version newVersion, ReleaseChannels.Channel releaseChannel) =>
            IsCanaryBuild 
                ? $"https://git.ryujinx.app/ryubing/ryujinx/-/compare/Canary-{currentVersion}...Canary-{newVersion}"
                : GetChangelogForVersion(newVersion, releaseChannel);
        
        public static string GetChangelogForVersion(Version version, ReleaseChannels.Channel releaseChannel) =>
            $"https://github.com/{releaseChannel}/releases/{version}";
        
        public static async Task<ReleaseChannels> GetReleaseChannelsAsync(HttpClient httpClient)
        {
            ReleaseChannelPair releaseChannelPair = JsonHelper.Deserialize(await httpClient.GetStringAsync("https://ryujinx.app/api/release-channels"), ReleaseChannelPairContext.Default.ReleaseChannelPair);
            return new ReleaseChannels(releaseChannelPair);
        }
    }

    public readonly struct ReleaseChannels
    {
        internal ReleaseChannels(ReleaseChannelPair channelPair)
        {
            Stable = new Channel(channelPair.Stable);
            Canary = new Channel(channelPair.Canary);
        }

        public readonly Channel Stable;
        public readonly Channel Canary;
        
        public readonly struct Channel
        {
            public Channel(string raw)
            {
                string[] parts = raw.Split('/');
                Owner = parts[0];
                Repo = parts[1];
            }
            
            public readonly string Owner;
            public readonly string Repo;

            public override string ToString() => $"{Owner}/{Repo}";

            public string GetLatestReleaseApiUrl() =>
                $"https://api.github.com/repos/{ToString()}/releases/latest";
        }
    }
    
    [JsonSerializable(typeof(ReleaseChannelPair))]
    partial class ReleaseChannelPairContext : JsonSerializerContext;

    class ReleaseChannelPair
    {
        [JsonPropertyName("stable")]
        public string Stable { get; set; }
        [JsonPropertyName("canary")]
        public string Canary { get; set; }
    }
}
