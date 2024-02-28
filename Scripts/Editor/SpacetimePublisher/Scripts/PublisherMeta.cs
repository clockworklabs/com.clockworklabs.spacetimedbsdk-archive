namespace SpacetimeDB.Editor
{
    using static SpacetimeMeta;
    
    /// Static metadata for PublisherWindow
    public static class PublisherMeta
    {
        public enum StringStyle
        {
            Action,
            Error,
            Success,
        }
        
        public enum FoldoutGroupType
        {
            Server,
            Identity,
            Publish,
            PublishResult,
        }

        public const string TOP_BANNER_CLICK_LINK = "https://spacetimedb.com/docs/modules";
        public const string DOCS_URL = "https://spacetimedb.com/install";
        public const string PUBLISHER_DIR_PATH = "Packages/" + SDK_PACKAGE_NAME + "/Scripts/Editor/SpacetimePublisher";
        public static string PathToUxml => $"{PUBLISHER_DIR_PATH}/PublisherWindowComponents.uxml";
        public static string PathToUss => $"{PUBLISHER_DIR_PATH}/PublisherWindowStyles.uss";
        
        public static string GetStyledStr(StringStyle style, string str)
        {
            return style switch
            {
                StringStyle.Action => $"<color={ACTION_COLOR_HEX}>{str}</color>",
                StringStyle.Error => $"<color={ERROR_COLOR_HEX}>{str}</color>",
                StringStyle.Success => $"<color={SUCCESS_COLOR_HEX}>{str}</color>",
            };
        }
    }
}