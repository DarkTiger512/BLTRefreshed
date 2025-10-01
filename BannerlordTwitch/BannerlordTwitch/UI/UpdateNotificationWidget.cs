using System;
using BannerlordTwitch.Util;

namespace BannerlordTwitch.UI
{
    public static class UpdateNotificationHelper
    {
        public static void ShowUpdateNotification(string latestVersion, string currentVersion, string downloadUrl)
        {
            var updateText = $"BLT Enhanced Edition v{latestVersion} is available! Current: v{currentVersion}";
            
            // Show in bottom-left notification area with notification sound
            Log.ShowInformation(updateText, null, Log.Sound.Notification1);
            
            // Also show in game log
            Log.LogFeedSystem(updateText);
            
            // Log to debug output
            Log.Trace($"Update available: {updateText} - Download: {downloadUrl}");
        }
        
        public static void ShowUpToDateNotification(string currentVersion)
        {
            var upToDateText = $"BLT Enhanced Edition is up to date (v{currentVersion})";
            
            // Show in bottom-left notification area with gentle sound
            Log.ShowInformation(upToDateText, null, Log.Sound.None);
            
            // Also show in game log
            Log.LogFeedSystem(upToDateText);
            
            // Log to debug output
            Log.Trace($"Up to date: {upToDateText}");
        }
    }
}