using UnityEngine;

/// Static metadata for SpacetimeDB editor scripts
public static class SpacetimeMeta
{
    public const string SDK_PACKAGE_NAME = "com.clockworklabs.spacetimedbsdk";
    public const string COMMON_DIR_PATH = "Packages/" + SDK_PACKAGE_NAME + "/Scripts/Editor/Common";
    
    /// Path to common SpacetimeDB Editor USS styles
    public static string PathToCommonUss => $"{COMMON_DIR_PATH}/CommonStyles.uss";
    
    // Colors pulled from docs, often coincided with UI elements >>
    public const string ACTION_COLOR_HEX = "#FFEA30"; // Corn Yellow
    public const string ERROR_COLOR_HEX = "#FDBE01"; // Golden Orange
    public const string SUCCESS_COLOR_HEX = "#4CF490"; // Sea Green (from docs)
    public const string INPUT_TEXT_COLOR = "#B6C0CF"; // Hazel Grey
}
