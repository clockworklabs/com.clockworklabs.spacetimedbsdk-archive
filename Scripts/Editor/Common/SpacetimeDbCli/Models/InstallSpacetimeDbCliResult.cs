using System;
using System.IO;
using System.Text.RegularExpressions;
using SpacetimeDB.Editor;
using UnityEngine;

/// Result of installing SpacetimeDbCli via https://spacetimedb.com/install 
public class InstallSpacetimeDbCliResult : SpacetimeCliResult
{
    public bool IsInstalled { get; }
    
    /// - WINDOWS:
    ///   - Without admin: `%USERPROFILE%\.spacetime\spacetime.exe` [Most Likely]
    ///   - With admin: `%WINDIR%\..\.spacetime\spacetime.exe`
    ///
    /// - MAC: TODO
    ///
    /// - LINUX: TODO
    public string PathToExe { get; }
    public string PathToExeDir { get; }

    /// Cross-platform compatible with "PATH" (Mac|Linux) and "Path" env vars
    public string GetNormalizedPathToSpacetimeDir()
    {
        string normalizedPath = PathToExeDir
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        // If running on Windows and the path starts with a network path (\\), ensure it's preserved.
        if (Path.DirectorySeparatorChar == '\\' && 
            normalizedPath.StartsWith("\\") && 
            !normalizedPath.StartsWith(@"\\"))
        {
            normalizedPath = "\\" + normalizedPath;
        }

        return normalizedPath;
    }

    public InstallSpacetimeDbCliResult(SpacetimeCliResult cliResult)
        : base(cliResult.CliOutput, cliResult.CliError)
    {
        this.IsInstalled = CliOutput.Contains("spacetime is installed into");
        if (!IsInstalled)
        {
            return;
        }
        
        bool isWindows64 = Environment.OSVersion.Platform == PlatformID.Win32NT;
        bool isMac = Environment.OSVersion.Platform == PlatformID.MacOSX;
        bool isLinux = Environment.OSVersion.Platform == PlatformID.Unix;
        
        // Get install path from, as exampled, "spacetime is installed into C:/Users/foouser/SpacetimeDB/spacetime.exe"
        string pattern = isWindows64 
            ? @"(?<=installed into )(.+spacetime.exe)"
            : isMac ? @"(?<=installed into )(.+spacetime)" 
            : isLinux ? @"(?<=installed into )(.+spacetime)" 
            : throw new Exception("Unsupported OS");
        
        Match match = Regex.Match(CliOutput, pattern);

        if (match.Success)
        {
            this.PathToExe = match.Success ? match.Value : string.Empty;
            this.PathToExeDir = Path.GetDirectoryName(PathToExe);
        }
        
        Debug.Log($"Installed SpacetimeDB CLI to: `{PathToExe}`");
    }
}
