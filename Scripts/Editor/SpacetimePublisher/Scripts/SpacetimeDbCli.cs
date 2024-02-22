using System;
using UnityEngine;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace SpacetimeDB.Editor
{
    /// CLI action helper for PublisherWindowInit.
    /// Vanilla: Do the action -> return the result -> no more.
    public static class SpacetimeDbCli
    {
        #region Static Options
        private const CliLogLevel LOG_LEVEL = CliLogLevel.Info;
        
        public enum CliLogLevel
        {
            Info,
            Error,
        }
        #endregion // Static Options

        
        #region Init
        /// Install the SpacetimeDB CLI | https://spacetimedb.com/install 
        public static async Task<SpacetimeCliResult> InstallSpacetimeCliAsync()
        {
            if (LOG_LEVEL == CliLogLevel.Info)
                Debug.Log("Installing SpacetimeDB CLI tool...");
            
            SpacetimeCliResult result; 
            
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    result = await runCliCommandAsync("powershell -Command \"iwr " +
                        "https://windows.spacetimedb.com -UseBasicParsing | iex\"\n");
                    break;
                
                case RuntimePlatform.OSXEditor:
                    result = await runCliCommandAsync("brew install clockworklabs/tap/spacetime");
                    break;
                
                case RuntimePlatform.LinuxEditor:
                    result = await runCliCommandAsync("curl -sSf https://install.spacetimedb.com | sh");
                    break;
                
                default:
                    throw new NotImplementedException("Unsupported OS");
            }
            
            if (LOG_LEVEL == CliLogLevel.Info)
                Debug.Log($"Installed spacetimeDB CLI tool | {PublisherMeta.DOCS_URL}");
            
            return result;
        }
        #endregion // Init
        
        
        #region Core CLI
        /// Issue a cross-platform CLI cmd, where we'll start with terminal prefixes
        /// as the CLI "command" and some arg prefixes for compatibility.
        /// Usage: Pass an argSuffix, such as "spacetime version",
        ///        along with an optional cancel token
        private static async Task<SpacetimeCliResult> runCliCommandAsync(
            string argSuffix, 
            CancellationToken cancelToken = default)
        {
            string output = string.Empty;
            string error = string.Empty;
            Process process = new();
            CancellationTokenRegistration cancellationRegistration = default;

            try
            {
                string terminal = getTerminalPrefix(); // Determine terminal based on platform
                string argPrefix = getCommandPrefix(); // Determine command prefix (cmd /c, etc.)
                string fullParsedArgs = $"{argPrefix} \"{argSuffix}\"";

                process.StartInfo.FileName = terminal;
                process.StartInfo.Arguments = fullParsedArgs;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                
                // Input Logs
                if (LOG_LEVEL == CliLogLevel.Info)
                {
                    Debug.Log("CLI Input: \n```\n<color=yellow>" +
                        $"{terminal} {fullParsedArgs}</color>\n```\n");
                }

                process.Start();

                // Register cancellation token to safely handle process termination
                cancellationRegistration = cancelToken.Register(() => terminateProcessSafely(process));

                // Asynchronously read output and error
                Task<string> readOutputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> readErrorTask = process.StandardError.ReadToEndAsync();

                // Wait for the process to exit or be cancelled
                while (!process.HasExited)
                    await Task.Delay(100, cancelToken);

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
                if (!process.HasExited)
                    process.Kill();
                process.Dispose(); // No async ver for this Dispose
            }
            
            // Process results, log err (if any), return parsed Result 
            SpacetimeCliResult cliResult = new(output, error);
            logCliResults(cliResult);

            return new SpacetimeCliResult(output, error);
        }

        private static void terminateProcessSafely(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.CloseMainWindow();
                    if (!process.WaitForExit(5000)) // Wait up to 5 seconds
                        process.Kill(); // Force terminate the process if it hasn't exited
                }
            }
            catch (InvalidOperationException e)
            {
                // Process likely already exited or been disposed; safe to ignore, in most cases
                Debug.LogWarning($"Attempted to terminate a process: {e.Message}");
            }
        }

        private static void logCliResults(SpacetimeCliResult cliResult)
        {
            bool hasOutput = !string.IsNullOrEmpty(cliResult.CliOutput);
            bool hasLogLevelInfoNoErr = LOG_LEVEL == CliLogLevel.Info && !cliResult.HasCliErr;
            string prettyOutput = $"\n```\n<color=yellow>{cliResult.CliOutput}</color>\n```\n";
            
            if (hasOutput && hasLogLevelInfoNoErr)
                Debug.Log($"CLI Output: {prettyOutput}");

            if (cliResult.HasCliErr)
            {
                // There may be only a CliError and no CliOutput, depending on the type of error.
                if (!string.IsNullOrEmpty(cliResult.CliOutput))
                    Debug.LogError($"CLI Output (with verbose errors): {prettyOutput}");
                
                Debug.LogError($"CLI Error: {cliResult.CliError}\n" +
                    "(For +details, see output err above)");
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
            
        
        #region High Level CLI Actions
        /// isInstalled = !cliResult.HasCliError 
        public static async Task<SpacetimeCliResult> GetIsSpacetimeCliInstalledAsync()
        {
            SpacetimeCliResult cliResult = await runCliCommandAsync("spacetime version");

            // Info Logs
            bool isSpacetimeCliInstalled = !cliResult.HasCliErr;
            if (LOG_LEVEL == CliLogLevel.Info)
                Debug.Log($"{nameof(isSpacetimeCliInstalled)}=={isSpacetimeCliInstalled}");

            return cliResult;
        }
        
        /// Uses the `spacetime publish` CLI command, appending +args from UI elements
        public static async Task<PublishServerModuleResult> PublishServerModuleAsync(
            PublishRequest publishRequest,
            CancellationToken cancelToken)
        {
            string argSuffix = $"spacetime publish {publishRequest}";
            SpacetimeCliResult cliResult = await runCliCommandAsync(argSuffix, cancelToken);
            PublishServerModuleResult publishResult = new(cliResult);
            return onPublishServerModuleDone(publishResult);
        }
        
        /// Uses the `npm install -g wasm-opt` CLI command
        public static async Task<SpacetimeCliResult> InstallWasmOptPkgAsync()
        {
            const string argSuffix = "npm install -g wasm-opt";
            SpacetimeCliResult cliResult = await runCliCommandAsync(argSuffix);
            return onInstallWasmOptPkgDone(cliResult);
        }

        private static SpacetimeCliResult onInstallWasmOptPkgDone(SpacetimeCliResult cliResult)
        {
            // Success results in !CliError and "changed {numPkgs} packages in {numSecs}s
            return cliResult;
        }

        private static PublishServerModuleResult onPublishServerModuleDone(PublishServerModuleResult publishResult)
        {
            // Check for general CLI errs (that may contain false-positives for `spacetime publish`)
            bool hasGeneralCliErr = !publishResult.HasCliErr;
            if (LOG_LEVEL == CliLogLevel.Info)
                Debug.Log($"{nameof(hasGeneralCliErr)}=={hasGeneralCliErr}");

            if (publishResult.HasPublishErr)
            {
                // This may not necessarily be a major or breaking issue.
                // For example, !optimized builds will show as an "error" rather than warning.
                Debug.LogError($"Server module publish issue found | {publishResult}"); // json
            }
            else if (LOG_LEVEL == CliLogLevel.Info)
                Debug.Log($"Server module publish ({publishResult.PublishType}) success | {publishResult}"); // json
            
            return publishResult;
        }
        
        /// Uses the `spacetime identity list` CLI command
        public static async Task<GetIdentitiesResult> GetIdentitiesAsync() 
        {
            SpacetimeCliResult cliResult = await runCliCommandAsync("spacetime identity list");
            GetIdentitiesResult getIdentitiesResult = new(cliResult);
            return getIdentitiesResult;
        }
        
        /// Uses the `spacetime identity list` CLI command
        public static async Task<GetServersResult> GetServersAsync() 
        {
            SpacetimeCliResult cliResult = await runCliCommandAsync("spacetime server list");
            GetServersResult getServersResult = new(cliResult);
            return getServersResult;
        }
        
        /// Uses the `spacetime identity new` CLI command, then set as default.
        public static async Task<AddIdentityResult> AddIdentityAsync(AddIdentityRequest addIdentityRequest)
        {
            string argSuffix = $"spacetime identity new {addIdentityRequest}"; // Forced set as default
            SpacetimeCliResult cliResult = await runCliCommandAsync(argSuffix);
            AddIdentityResult addIdentityResult = new(cliResult);
            return addIdentityResult;
        }
        
        /// Uses the `spacetime server add` CLI command, then set as default.
        public static async Task<AddServerResult> AddServerAsync(AddServerRequest addServerRequest)
        {
            // Forced set as default. Forced --no-fingerprint for local.
            string argSuffix = $"spacetime server add {addServerRequest}";
            SpacetimeCliResult cliResult = await runCliCommandAsync(argSuffix);
            AddServerResult addServerResult = new(cliResult);
            return addServerResult;
        }
        
        /// Uses the `spacetime identity set-default` CLI command
        public static async Task<SpacetimeCliResult> SetDefaultIdentityAsync(string identityNicknameOrDbAddress)
        {
            string argSuffix = $"spacetime identity set-default {identityNicknameOrDbAddress}";
            SpacetimeCliResult cliResult = await runCliCommandAsync(argSuffix);
            return cliResult;
        } 

        /// Uses the `spacetime server new` CLI command
        public static async Task<SpacetimeCliResult> SetDefaultServerAsync(string serverNicknameOrHost)
        {
            string argSuffix = $"spacetime server set-default {serverNicknameOrHost}";
            SpacetimeCliResult cliResult = await runCliCommandAsync(argSuffix);
            return cliResult;
        }
        #endregion // High Level CLI Actions
    }
}