using System.Threading;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace SpacetimeDB.Editor
{
    /// CLI action middleware between PublisherWindow and SpacetimeDbCli 
    /// Vanilla: Do the action -> return the result -> no more.
    /// (!) Didn't find what you were looking for here? Check `SpacetimeDbCli.cs`
    public static class SpacetimeDbPublisherCli
    {
        #region Static Options
        private const SpacetimeDbCli.CliLogLevel PUBLISHER_CLI_LOG_LEVEL = SpacetimeDbCli.CliLogLevel.Info;
        #endregion // Static Options

        
        #region High Level CLI Actions
        /// Uses the `spacetime publish` CLI command, appending +args from UI elements
        public static async Task<PublishResult> PublishServerModuleAsync(
            PublishRequest publishRequest,
            CancellationToken cancelToken)
        {
            string argSuffix = $"spacetime publish {publishRequest}";
            SpacetimeCliResult cliResult = await SpacetimeDbCli.runCliCommandAsync(argSuffix, cancelToken);
            PublishResult publishResult = new(cliResult);
            return onPublishServerModuleDone(publishResult);
        }
        
        /// Uses the `npm install -g wasm-opt` CLI command
        public static async Task<SpacetimeCliResult> InstallWasmOptPkgAsync()
        {
            const string argSuffix = "npm install -g wasm-opt";
            SpacetimeCliResult cliResult = await SpacetimeDbCli.runCliCommandAsync(argSuffix);
            return onInstallWasmOptPkgDone(cliResult);
        }

        private static SpacetimeCliResult onInstallWasmOptPkgDone(SpacetimeCliResult cliResult)
        {
            // Success results in !CliError and "changed {numPkgs} packages in {numSecs}s
            return cliResult;
        }

        private static PublishResult onPublishServerModuleDone(PublishResult publishResult)
        {
            // Check for general CLI errs (that may contain false-positives for `spacetime publish`)
            bool hasGeneralCliErr = !publishResult.HasCliErr;
            if (PUBLISHER_CLI_LOG_LEVEL == SpacetimeDbCli.CliLogLevel.Info)
                Debug.Log($"{nameof(hasGeneralCliErr)}=={hasGeneralCliErr}");

            if (publishResult.HasPublishErr)
            {
                // This may not necessarily be a major or breaking issue.
                // For example, !optimized builds will show as an "error" rather than warning.
                Debug.LogError($"Server module publish issue found | {publishResult}"); // json
            }
            else if (PUBLISHER_CLI_LOG_LEVEL == SpacetimeDbCli.CliLogLevel.Info)
                Debug.Log($"Server module publish ({publishResult.PublishType}) success | {publishResult}"); // json
            
            return publishResult;
        }
        
        /// Uses the `spacetime identity new` CLI command, then set as default.
        public static async Task<AddIdentityResult> AddIdentityAsync(AddIdentityRequest addIdentityRequest)
        {
            string argSuffix = $"spacetime identity new {addIdentityRequest}"; // Forced set as default
            SpacetimeCliResult cliResult = await SpacetimeDbCli.runCliCommandAsync(argSuffix);
            AddIdentityResult addIdentityResult = new(cliResult);
            return addIdentityResult;
        }
        
        /// Uses the `spacetime server add` CLI command, then set as default.
        public static async Task<AddServerResult> AddServerAsync(AddServerRequest addServerRequest)
        {
            // Forced set as default. Forced --no-fingerprint for local.
            string argSuffix = $"spacetime server add {addServerRequest}";
            SpacetimeCliResult cliResult = await SpacetimeDbCli.runCliCommandAsync(argSuffix);
            AddServerResult addServerResult = new(cliResult);
            return addServerResult;
        }
        
        /// Uses the `spacetime identity set-default` CLI command
        public static async Task<SpacetimeCliResult> SetDefaultIdentityAsync(string identityNicknameOrDbAddress)
        {
            string argSuffix = $"spacetime identity set-default {identityNicknameOrDbAddress}";
            SpacetimeCliResult cliResult = await SpacetimeDbCli.runCliCommandAsync(argSuffix);
            return cliResult;
        } 

        /// Uses the `spacetime server new` CLI command
        public static async Task<SpacetimeCliResult> SetDefaultServerAsync(string serverNicknameOrHost)
        {
            string argSuffix = $"spacetime server set-default {serverNicknameOrHost}";
            SpacetimeCliResult cliResult = await SpacetimeDbCli.runCliCommandAsync(argSuffix);
            return cliResult;
        }
        #endregion // High Level CLI Actions
    }
}