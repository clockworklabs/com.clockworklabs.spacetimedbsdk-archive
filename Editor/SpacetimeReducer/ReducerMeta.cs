using System.IO;
using static SpacetimeMeta;

namespace SpacetimeDB.Editor
{
    /// Static metadata for ReducerWindow
    public static class ReducerMeta
    {
        public const string TOP_BANNER_CLICK_LINK = "https://spacetimedb.com/docs/modules";

        public static string REDUCER_DIR_PATH => Path.Join(SPACETIMEDB_EDITOR_DIR_PATH, "SpacetimeReducer");
        public static string PathToUxml => Path.Join(REDUCER_DIR_PATH, "ReducerWindowComponents.uxml");
        public static string PathToUss => Path.Join(REDUCER_DIR_PATH, "ReducerWindowStyles.uss");
        
        /// (!) "--as-identity" is deprecated for "identity" for reducer calls
        public const string CALL_AS_IDENTITY_CMD = "--as-identity";
    }
}