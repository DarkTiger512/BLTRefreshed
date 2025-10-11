using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using TaleWorlds.Library;

namespace BannerlordTwitch.Util
{
    public class UpdateChecker
    {
        private const string GITHUB_TAGS_API_URL = "https://api.github.com/repos/DarkTiger512/BLTRefreshed/tags";
        private const string GITHUB_RELEASES_URL = "https://github.com/DarkTiger512/BLTRefreshed/releases/tag/";
        
        // Get version from assembly info (automatically updated by build system from BLTProperties.targets)
        private static readonly string CURRENT_VERSION = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        
        private static readonly HttpClient httpClient = new HttpClient();
        
        public static async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            try
            {
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "BLT-Enhanced-Edition");
                
                var response = await httpClient.GetStringAsync(GITHUB_TAGS_API_URL);
                var latestTag = ParseLatestTag(response);
                
                if (latestTag != null && IsNewerVersion(latestTag.Version, CURRENT_VERSION))
                {
                    return new UpdateInfo
                    {
                        IsUpdateAvailable = true,
                        LatestVersion = latestTag.Version,
                        CurrentVersion = CURRENT_VERSION,
                        DownloadUrl = GITHUB_RELEASES_URL + latestTag.TagName
                    };
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - update checking should be non-critical
                Log.Trace($"Update check failed: {ex.Message}");
            }
            
            return new UpdateInfo { IsUpdateAvailable = false, CurrentVersion = CURRENT_VERSION };
        }
        
        private static GitHubTag ParseLatestTag(string json)
        {
            try
            {
                // Parse the tags array and find the latest semantic version
                var tagMatches = System.Text.RegularExpressions.Regex.Matches(json, @"""name""\s*:\s*""([^""]+)""");
                
                GitHubTag latestTag = null;
                Version latestVersion = null;
                
                foreach (System.Text.RegularExpressions.Match match in tagMatches)
                {
                    var tagName = match.Groups[1].Value;
                    var versionString = tagName.TrimStart('v'); // Remove 'v' prefix if present
                    
                    if (Version.TryParse(versionString, out Version version))
                    {
                        if (latestVersion == null || version > latestVersion)
                        {
                            latestVersion = version;
                            latestTag = new GitHubTag
                            {
                                TagName = tagName,
                                Version = versionString
                            };
                        }
                    }
                }
                
                return latestTag;
            }
            catch (Exception ex)
            {
                Log.Info($"Failed to parse GitHub tags: {ex.Message}");
            }
            
            return null;
        }
        
        private static bool IsNewerVersion(string latest, string current)
        {
            try
            {
                var latestVersion = new Version(latest);
                var currentVersion = new Version(current);
                return latestVersion > currentVersion;
            }
            catch
            {
                return false;
            }
        }
    }
    
    public class UpdateInfo
    {
        public bool IsUpdateAvailable { get; set; }
        public string LatestVersion { get; set; }
        public string CurrentVersion { get; set; }
        public string DownloadUrl { get; set; }
    }
    
    public class GitHubTag
    {
        public string TagName { get; set; }
        public string Version { get; set; }
    }
}