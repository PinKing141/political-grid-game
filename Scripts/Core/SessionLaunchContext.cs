namespace PolisGrid.Core
{
    public static class SessionLaunchContext
    {
        public const string DefaultSavePath = "user://savegame.json";

        private static string _pendingLoadPath = string.Empty;

        public static void Clear()
        {
            _pendingLoadPath = string.Empty;
        }

        public static void RequestLoadCampaign(string savePath = DefaultSavePath)
        {
            _pendingLoadPath = string.IsNullOrWhiteSpace(savePath) ? DefaultSavePath : savePath;
        }

        public static bool TryConsumeLoadCampaignPath(out string savePath)
        {
            if (string.IsNullOrWhiteSpace(_pendingLoadPath))
            {
                savePath = string.Empty;
                return false;
            }

            savePath = _pendingLoadPath;
            _pendingLoadPath = string.Empty;
            return true;
        }
    }
}