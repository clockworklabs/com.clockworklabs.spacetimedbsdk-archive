using System;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace SpacetimeDB.Editor
{
    /// Common CLI action helper for UI Builder windows.
    /// - Vanilla: Do the action -> return the result -> no more.
    /// - Technically anything here accepts a CancellationToken; just add to arg + pass along!
    /// - (!) Looking for more actions? See `SpacetimeDbPublisherCli.cs`
    /// - (!) Looking for publisher actions? See `SpacetimePublisherActionsCli.cs`
    /// - (!) Looking for reducers actions? See `SpacetimeReducerActionsCli.cs`
    public static class SpacetimeDbCli
    {
        #region Static state/opts
        private const CliLogLevel CLI_LOG_LEVEL = CliLogLevel.Info;
        
        public enum CliLogLevel
        {
            Info,
            Error,
        }

        /// If you *just* installed SpacetimeDB CLI, this will update PATH in the spawned Process.
        /// Prevents restarting Unity to refresh paths (UX).
        public static string NewlyInstalledCliEnvDirPath { get; private set; }

        private static bool _autoResolvedBugIsTryingAgain;
        #endregion // Static state/opts

        
        #region Init
        /// Install the SpacetimeDB CLI | https://spacetimedb.com/install
        /// After installed, the default server is normally `local`, but there's a pre-approved open PR
        /// to change the default to `testnet` soon: https://github.com/clockworklabs/SpacetimeDB/pull/1078 
        public static async Task<InstallSpacetimeDbCliResult> InstallSpacetimeCliAsync()
        {
            if (CLI_LOG_LEVEL == CliLogLevel.Info)
            {
                Debug.Log("<b>Installing SpacetimeDB CLI tool ...</b>");
            }

            string argSuffix = null;
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    argSuffix = "powershell -Command \"iwr https://windows.spacetimedb.com -UseBasicParsing | iex";
                    break;
                
                case RuntimePlatform.OSXEditor:
                    argSuffix = "brew install clockworklabs/tap/spacetime";
                    break;
                
                case RuntimePlatform.LinuxEditor:
                    break;
                
                default:
                    throw new NotImplementedException("Unsupported OS");
            }
            
            SpacetimeCliResult cliResult = await runCliCommandAsync(argSuffix);
            InstallSpacetimeDbCliResult installResult = new(cliResult);
            
            // Update PATH env var override to prevent having to restart Unity for the next cmd
            if (installResult.IsInstalled)
            {
                NewlyInstalledCliEnvDirPath = installResult.GetNormalizedPathToSpacetimeDir();
            }
            
            return installResult;
        }
        #endregion // Init
        
        
        #region Core CLI
        private static Process createCliProcess(
            string terminal,
            string fullParsedArgs,
            bool detachedProcess = false)
        {
            Process cliProcess = new();
            
            ProcessStartInfo startInfo = cliProcess.StartInfo;
            startInfo.FileName = terminal;
            startInfo.Arguments = fullParsedArgs;
            startInfo.CreateNoWindow = true;

            if (detachedProcess)
            {
                startInfo.RedirectStandardError = false;
                startInfo.RedirectStandardOutput = false;
                startInfo.UseShellExecute = true;
            }
            else
            {
                // Redirect I/O to Unity
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                
                // If we *just* installed SpacetimeDB CLI, PATH is not yet refreshed
                checkUpdatePathForNewSpacetimeDbCliInstall(startInfo);
            }

            return cliProcess;
        }

        /// If we *just* installed SpacetimeDB CLI (NewlyInstalledCliEnvDirPath),
        /// PATH is not yet refreshed: Update PATH env vars
        private static void checkUpdatePathForNewSpacetimeDbCliInstall(ProcessStartInfo startInfo)
        {
            bool hasEnvPathOverride = !string.IsNullOrEmpty(NewlyInstalledCliEnvDirPath);
            if (!hasEnvPathOverride)
            {
                return;
            }
        
            string keyName = Application.platform == RuntimePlatform.WindowsEditor ? "Path" : "PATH"; 
            string pathAddendum = Path.PathSeparator + NewlyInstalledCliEnvDirPath;
            startInfo.EnvironmentVariables[keyName] += pathAddendum;
        }
        
        private static void logInput(string terminal, string fullParsedArgs)
        {
            if (CLI_LOG_LEVEL != CliLogLevel.Info)
            {
                return;
            }
                
            Debug.Log($"CLI Input: `<color=yellow>{terminal} {fullParsedArgs}</color>`\n");
        }
        
        /// Starts a detached Process, allowing a domain reload to not freeze Unity or kill the process.
        public static SpacetimeCliRequest startDetachedCliProcess(string argSuffix)
        {
            // Args
            string terminal = getTerminalPrefix(); // Determine terminal based on platform
            string argPrefix = getCommandPrefix(); // Determine command prefix (cmd /c, etc.)
            string fullParsedArgs = $"{argPrefix} \"{argSuffix.Trim()}\"";
            
            // Process + StartInfo
            Process asyncCliProcess = createCliProcess(terminal, fullParsedArgs, detachedProcess: true);
            logInput(terminal, fullParsedArgs);
            
            // Package request to pass along to result for easier debugging / err handling
            SpacetimeCliRequest cliRequest = new(
                terminal, 
                argPrefix, 
                argSuffix, 
                runInBackground: false, 
                asyncCliProcess.StartInfo);
            
            try
            {
                asyncCliProcess.Start();
                asyncCliProcess.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to {nameof(startDetachedCliProcess)}: {e.Message}");
                throw;
            }

            return cliRequest;
        }

        /// Issue a cross-platform CLI cmd, where we'll start with terminal prefixes
        /// as the CLI "command" and some arg prefixes for compatibility.
        /// Usage: Pass an argSuffix, such as "spacetime version",
        ///        along with an optional cancel token
        /// - Supports cancellations and timeouts via CancellationToken (create a CancellationTokenSource)
        public static async Task<SpacetimeCliResult> runCliCommandAsync(
            string argSuffix,
            CancellationToken cancelToken = default,
            bool runInBackground = false,
            bool logErrs = true)
        {
            // Args
            string terminal = getTerminalPrefix(); // Determine terminal based on platform
            string argPrefix = getCommandPrefix(); // Determine command prefix (cmd /c, etc.)
            string fullParsedArgs = $"{argPrefix} \"{argSuffix}\"";
            
            // Process + StartInfo
            Process cliProcess = createCliProcess(terminal, fullParsedArgs);

            // Cancellation Token + Async CLI Result (set early to prepare streaming logs *before* proc start)
            CancellationTokenRegistration cancellationRegistration = default;
            logInput(terminal, fullParsedArgs);
            
            // Set output+err logs just once after done (!async)
            string output = string.Empty;
            string error = string.Empty;

            try
            {
                cliProcess.Start();

                // Register cancellation token to safely handle process termination
                cancellationRegistration = cancelToken.Register(() => terminateProcessSafely(cliProcess));

                // Asynchronously read output and error
                Task<string> readOutputTask = cliProcess.StandardOutput.ReadToEndAsync();
                Task<string> readErrorTask = cliProcess.StandardError.ReadToEndAsync();

                // Wait for the process to exit or be cancelled
                if (!runInBackground)
                {
                    while (!cliProcess.HasExited)
                        await Task.Delay(100, cancelToken);    
                }

                // Await the read tasks to ensure output and error are captured
                output = await readOutputTask;
                error = await readErrorTask;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("CLI Warning: Canceled");
                error = "Canceled";
            }
            catch (Exception e)
            {
            }
            finally
            {
                // Heavy cleanup
                await cancellationRegistration.DisposeAsync();
                if (!cliProcess.HasExited)
                {
                    cliProcess.Kill();
                }

                cliProcess.Dispose(); // No async ver for this Dispose
            }
            
            // Package request to pass along to result for easier debugging / err handling
            SpacetimeCliRequest cliRequest = new(
                terminal, 
                argPrefix, 
                argSuffix, 
                runInBackground: true, 
                cliProcess.StartInfo);
            
            // Process results, log err (if any), return parsed Result 
            SpacetimeCliResult cliResult = new(output, error, cliRequest);
            logCliResults(cliResult, logErrs);

            // Can we auto-resolve this issue and try again?
            if (cliResult.HasCliErr && cliResult.CliError != "Canceled" && !_autoResolvedBugIsTryingAgain)
            {
                bool isResolvedTryAgain = await autoResolveCommonCliErrors(cliResult);
                if (isResolvedTryAgain)
                {
                    return await runCliCommandAsync(argSuffix, cancelToken);
                }
            }
            _autoResolvedBugIsTryingAgain = false;
            
            return cliResult;
        }

        /// <summary>
        /// Attempts to detect + resolve common cli errors:
        /// 1. "Error: Cannot list identities for server without a saved fingerprint: {serverName}"
        /// (!) We'll only attempt to auto-resolve fingerprint issues for !local
        /// </summary>
        /// <returns>isResolvedTryAgain</returns>
        private static async Task<bool> autoResolveCommonCliErrors(SpacetimeCliResult cliResult)
        {
            // TODO: Break this up if we catch too many
            _autoResolvedBugIsTryingAgain = true;
            bool isResolvedTryAgain = false;
            
            string cliError = cliResult.CliError;
            bool isFingerprintErr = cliError.Contains("without a saved fingerprint");
            if (!isFingerprintErr)
            {
                return isResolvedTryAgain;
            }
            
            bool isLocalFingerprintErr = cliError.Contains("local");
            bool isTestnetFingerprintErr = cliError.Contains("testnet");

            if (isLocalFingerprintErr)
            {
                #region Local Fingerprint Graveyard
                // // (!) Too much to consider for localhost fingerprint issues to handle automatically:
                // // - Localhost server may not be running
                // // - Localhost server may be running on a different port than the default 3000
                // // - If you run localhost on default port when running on a different, it'll fail
                // isResolvedTryAgain = await fixLocalFingerprintErr(cliResult);
                #endregion // Local Fingerprint Graveyard
            }
            else if (isTestnetFingerprintErr)
            {
                isResolvedTryAgain = await fixTestnetFingerprintErr(cliError);
            }

            return isResolvedTryAgain;
        }

        private static async Task<bool> fixTestnetFingerprintErr(string cliError)
        {
            // Attempt to create a fingerprint for the server
            SpacetimeCliResult fingerprintResult = await SpacetimeDbCliActions
                .CreateFingerprintAsync(SpacetimeMeta.TESTNET_SERVER_NAME);

            bool isResolvedTryAgain = !fingerprintResult.HasCliErr;
            return isResolvedTryAgain;
        }

        /// Attempt to fix the local fingerprint error by temporarily running a local server on default port
        /// This one's a bit tricky since we need to start the server, create a fingerprint, and then stop the server
        private static async Task<bool> fixLocalFingerprintErr(SpacetimeCliResult cliResult)
        {
            // See if the default server is running on the default port
            const string localServerName = SpacetimeMeta.LOCAL_SERVER_NAME;
            PingServerResult pingResult = await SpacetimeDbCliActions.PingServerAsync(localServerName);
            if (!pingResult.IsServerOnline)
            {
                // Temporarily start the server on the default port
                await runCliCommandAsync("spacetime start local");
            }
            
            // Attempt just once to create a fingerprint for the server
            // This would only fail if port 3000 is in use by a 3rd-party app
            SpacetimeCliResult fingerprintResult = await SpacetimeDbCliActions
                .CreateFingerprintAsync(localServerName);

            bool isResolvedTryAgain = !fingerprintResult.HasCliErr;
            return isResolvedTryAgain;
        }

        private static void terminateProcessSafely(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.CloseMainWindow();
                    if (!process.WaitForExit(5000)) // Wait up to 5 seconds
                    {
                        process.Kill(); // Force terminate the process if it hasn't exited
                    }
                }
            }
            catch (InvalidOperationException e)
            {
                // Process likely already exited or been disposed; safe to ignore, in most cases
                Debug.LogWarning($"Attempted to terminate a process: {e.Message}");
            }
        }

        private static void logCliResults(SpacetimeCliResult cliResult, bool logErrs)
        {
            bool hasOutput = !string.IsNullOrEmpty(cliResult.CliOutput);
            bool hasLogLevelInfoNoErr = CLI_LOG_LEVEL == CliLogLevel.Info && !cliResult.HasCliErr;
            string prettyOutput = $"\n```\n<color=yellow>{cliResult.CliOutput}</color>\n```\n";

            if (hasOutput && hasLogLevelInfoNoErr)
            {
                Debug.Log($"CLI Output: {prettyOutput}");
            }

            if (cliResult.HasCliErr)
            {
                // There may be only a CliError and no CliOutput, depending on the type of error
                string prettyOutputOrNull = hasOutput ? prettyOutput : "<Empty>";
                Debug.Log($"CLI Output: {prettyOutputOrNull}");

                bool isWarning = cliResult.CliError is "Canceled";
                if (isWarning)
                {
                    Debug.LogWarning($"CLI Warning: {cliResult.CliError}");
                }
                else if (!logErrs)
                {
                    // Err, bug instructed to skip logs - it'll still be in the CliError
                    return;
                }
                else
                {
                    // Error
                    Debug.LogError($"CLI Error: {cliResult.CliError}");
                }

                // Separate the errs found from the CLI output so the user doesn't need to dig
                bool logCliResultErrsSeparately = cliResult.ErrsFoundFromCliOutput?.Count is > 0 and < 5;
                
                if (cliResult.HasErrsFoundFromCliOutput & logCliResultErrsSeparately) // If not too many
                {
                    for (int i = 0; i < cliResult.ErrsFoundFromCliOutput.Count; i++)
                    {
                        string err = cliResult.ErrsFoundFromCliOutput[i];
                        Debug.LogError($"CLI Error Summary[{i}]: {err}");
                    }
                }
            }
        }

        private static string getCommandPrefix()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    return "/c";
                
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.LinuxEditor:
                    return "-c";
                
                default:
                    Debug.LogError("Unsupported OS");
                    return null;
            }
        }

        /// Return either "cmd.exe" || "/bin/bash"
        private static string getTerminalPrefix()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    return "cmd.exe";
                
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.LinuxEditor:
                    return "/bin/bash";
                
                default:
                    Debug.LogError("Unsupported OS");
                    return null;
            }
        }
        #endregion // Core CLI
        
        
        #region CLI Utils
        /// Cross-platform kill cmd for SpacetimeDB Local Server (or technically any port)
        /// TODO: Needs +review for safety; ran through ChatGPT a couple times
        public static string GetKillCommand(ushort port)
        {
            bool isWin = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows);
            
            // For safety, ensures pid != 0
            if (isWin)
            {
                // Windows command to find by port and kill
                return $"netstat -aon | findstr :{port} && for /f \"tokens=5\" %a " +
                       $"in ('netstat -aon ^| findstr :{port}') " +
                       $"do if not %a==0 taskkill /F /PID %a";
            }
            
            // macOS/Linux command to find by port and kill
            // TODO: Mac|Linux needs testing
            return $"lsof -ti:{port} | grep -v '^0$' | xargs -r kill -9";
        }

        /// Cross-platform checks for a command not being found. Examples:
        /// - Windows: "`npm` is not recognized"
        /// - Unix: "npm: command not found"
        public static bool CheckCmdNotFound(string cliErrorStr, string expectedCmd = "")
        {
            return Environment.OSVersion.Platform switch
            {
                PlatformID.Win32NT => cliErrorStr.Contains($"{expectedCmd}' is not recognized"),
                PlatformID.Unix => cliErrorStr.Contains($"{expectedCmd}: command not found"),
                _ => false
            };
        }
        #endregion // CLI Utils
    }
}