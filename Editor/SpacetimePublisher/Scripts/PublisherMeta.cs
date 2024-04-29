using System.IO;

namespace SpacetimeDB.Editor
{
    using static SpacetimeMeta;
    
    /// Static metadata for PublisherWindow
    /// Looking for more? See SpacetimeMeta.cs
    public static class PublisherMeta
    {
        public enum FoldoutGroupType
        {
            Server,
            Identity,
            Publish,
            PublishResult,
        }

        public const string TOP_BANNER_CLICK_LINK = "https://spacetimedb.com/docs/modules";
        public const string INSTALL_DOCS_URL = "https://spacetimedb.com/install";
        public const string INSTALL_WASM_OPT_URL = "https://github.com/WebAssembly/binaryen/releases";
        
        public static string PUBLISHER_DIR_PATH => Path.Join(SPACETIMEDB_EDITOR_DIR_PATH, "SpacetimePublisher");
        public static string PathToUxml => Path.Join(PUBLISHER_DIR_PATH, "PublisherWindowComponents.uxml");
        public static string PathToUss => Path.Join(PUBLISHER_DIR_PATH, "PublisherWindowStyles.uss");
        public static string PathToAutogenDir => Path.Join(UnityEngine.Application.dataPath, "StdbAutogen");
    }
}