using System.IO;

/// Static metadata for SpacetimeDB editor scripts. Also see:
/// - PublisherMeta.cs
/// - ReducerMeta.cs
public static class SpacetimeMeta
{
    #region Names & Paths
    public static string INSTALL_CARGO_URL = "https://www.rust-lang.org/tools/install";
    public static string DOTNET_8PLUS_URL = "https://dotnet.microsoft.com/download";
    public static string DOTNET_INSTALL_WASM_CMD = "dotnet workload install wasm-experimental";
    public const string DEFAULT_RUST_MODULE_PROJ_FILE = "Cargo.toml";
    public const string DEFAULT_CS_MODULE_PROJ_FILE = "StdbModule.csproj";
    
    public const ushort DEFAULT_PORT = 3000;
    public const string LOCAL_SERVER_NAME = "local";
    public const string TESTNET_SERVER_NAME = "testnet";
    
    /// The CLI, itself, sets this default server.
    /// 4/17 it's `local`, but there's a pre-approved open PR to have this set to `testnet`:
    /// https://github.com/clockworklabs/SpacetimeDB/pull/1078 
    public const string NEW_INSTALL_SERVER_CLI_DEFAULT = TESTNET_SERVER_NAME;
    
    public const string TESTNET_HOST_URL = "https://" + TESTNET_SERVER_NAME + ".spacetimedb.com";
    public static string LOCAL_HOST_URL => $"http://127.0.0.1:{DEFAULT_PORT}";
    
    public const string SDK_PACKAGE_NAME = "com.clockworklabs.spacetimedbsdk";
    public static string SPACETIMEDB_EDITOR_DIR_PATH => Path.Join("Packages", SDK_PACKAGE_NAME, "Editor"); 
    public static string COMMON_DIR_PATH => Path.Join(SPACETIMEDB_EDITOR_DIR_PATH, "Common");
    
    /// Path to common SpacetimeDB Editor USS styles
    public static string PathToCommonUss => Path.Join(COMMON_DIR_PATH, "CommonStyles.uss");
    
    /// Useful for adding new servers and you want the "Host" to be an alias
    /// to prevent forcing the user to memorize full urls and default ports
    /// - Parses local, localhost: http://127.0.1:3000
    /// - Parses testnet: https://testnet.spacetimedb.com
    public static string GetHostFromKnownServerName(string serverName)
    {
        return serverName switch
        {
            "local" => LOCAL_HOST_URL,
            "localhost" => LOCAL_HOST_URL,
            "testnet" => TESTNET_HOST_URL,
            _ => serverName
        };
    }
        
    public enum ModuleLang
    {
        CSharp,
        Rust,
    }
    #endregion // Names & Paths
    

    #region Colors & Formatting
    public static readonly string[] PROGRESS_BAR_SPINNER_CHARS =
    {
        "<mspace=8>[    ]</mspace>", 
        "<mspace=8>[=   ]</mspace>", 
        "<mspace=8>[==  ]</mspace>", 
        "<mspace=8>[=== ]</mspace>", 
        "<mspace=8>[ ===]</mspace>", 
        "<mspace=8>[  ==]</mspace>", 
        "<mspace=8>[   =]</mspace>", 
        "<mspace=8>[    ]</mspace>",
    };
    // public static readonly string[] PROGRESS_BAR_SPINNER_CHARS = { "◴", "◷", "◶", "◵" }; // Unicode !works in UI Builder yet

    // Colors pulled from docs, often coincided with UI elements >>
    public const string ACTION_COLOR_HEX = "#FFEA30"; // Corn Yellow
    public const string ERROR_COLOR_HEX = "#FDBE01"; // Golden Orange
    public const string SUCCESS_COLOR_HEX = "#4CF490"; // Sea Green
    public const string INPUT_TEXT_COLOR = "#B6C0CF"; // Hazel Grey
    
    public enum StringStyle
    {
        Action,
        Error,
        Success,
    }
        
    public static string GetStyledStr(StringStyle style, string str)
    {
        return style switch
        {
            StringStyle.Action => $"<color={ACTION_COLOR_HEX}>{str}</color>",
            StringStyle.Error => $"<color={ERROR_COLOR_HEX}>{str}</color>",
            StringStyle.Success => $"<color={SUCCESS_COLOR_HEX}>{str}</color>",
        };
    }
    #endregion // Colors & Formatting
    

    #region Editor Pref Keys
    public const string EDITOR_PREFS_MODULE_NAME_KEY = "SPACETIMEDB_MODULE_NAME"; 
    #endregion // Editor Pref Keys
}
