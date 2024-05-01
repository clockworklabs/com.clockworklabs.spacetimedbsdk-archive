using System.IO;
using static SpacetimeMeta;

namespace SpacetimeDB.Editor
{
    /// Static metadata for ServerLogViewerWindow
    /// Looking for more? See SpacetimeMeta.cs
    public static class ServerLogViewerMeta
    {
        public static string SERVER_LOG_VIEWER_DIR_PATH => Path.Join(SPACETIMEDB_EDITOR_DIR_PATH, "ServerLogViewer");
        public static string PathToUxml => Path.Join(SERVER_LOG_VIEWER_DIR_PATH, "ServerLogWindowComponents.uxml");
        public static string PathToUss => Path.Join(SERVER_LOG_VIEWER_DIR_PATH, "ServerLogWindowStyles.uss");
    }
}