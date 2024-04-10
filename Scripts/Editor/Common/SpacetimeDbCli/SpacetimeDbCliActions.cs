using System;
using System.Threading;
using System.Threading.Tasks;
using static SpacetimeDB.Editor.SpacetimeDbCli;

namespace SpacetimeDB.Editor
{
    /// Common CLI action helper for UI Builder windows.
    /// - Vanilla: Do the action -> return the result -> no more.
    /// - Technically anything here accepts a CancellationToken; just add to arg + pass along!
    /// - (!) Looking for more actions? See `SpacetimeDbPublisherCli.cs`
    public static class SpacetimeDbCliActions
    {
        #region High Level CLI Actions
        /// isInstalled = !cliResult.HasCliError 
        public static async Task<SpacetimeCliResult> GetIsSpacetimeCliInstalledAsync()
        {
            string argSuffix = "spacetime version";
            SpacetimeCliResult cliResult = await runCliCommandAsync(argSuffix);
            return cliResult;
        }
        
        /// Uses the `spacetime identity list` CLI command
        public static async Task<GetIdentitiesResult> GetIdentitiesAsync()
        {
            string argSuffix = "spacetime identity list";
            SpacetimeCliResult cliResult = await runCliCommandAsync(argSuffix);
            GetIdentitiesResult getIdentitiesResult = new(cliResult);
            return getIdentitiesResult;
        }
        
        /// Uses the `spacetime identity list` CLI command
        public static async Task<GetServersResult> GetServersAsync() 
        {
            string argSuffix = "spacetime server list";
            SpacetimeCliResult cliResult = await runCliCommandAsync(argSuffix);
            GetServersResult getServersResult = new(cliResult);
            return getServersResult;
        }
        
        /// Uses the `spacetime list {identity}` CLI command.
        /// (!) This only returns the addresses.
        ///     For nicknames, see the chained call: GetDbAddressesWithNicknames
        public static async Task<GetDbAddressesResult> GetDbAddressesAsync(string identity)
        {
            string argSuffix = $"spacetime list {identity}";
            SpacetimeCliResult cliResult = await runCliCommandAsync(argSuffix);
            GetDbAddressesResult getDbAddressesResult = new(cliResult);
            return getDbAddressesResult;
        }
        
        /// [Slow] Uses the `spacetime describe {moduleName} [--as-identity {identity}]` CLI command
        public static async Task<GetEntityStructureResult> GetEntityStructureAsync(
            string moduleName,
            string asIdentity = null)
        {
            // Append ` --as-identity {identity}`?
            string asIdentitySuffix = string.IsNullOrEmpty(asIdentity) ? "" : $" --as-identity {asIdentity}";
            string argSuffix = $"spacetime describe {moduleName}{asIdentitySuffix}";
            
            SpacetimeCliResult cliResult = await runCliCommandAsync(argSuffix);
            GetEntityStructureResult getEntityStructureResult = new(cliResult);
            return getEntityStructureResult;
        }

        /// Uses the `spacetime logs` CLI command.
        /// <param name="serverName"></param>
        public static async Task<SpacetimeCliResult> GetLogsAsync(string serverName)
        {
            string argSuffix = $"spacetime logs {serverName}";
            SpacetimeCliResult cliResult = await runCliCommandAsync(argSuffix);
            return cliResult;
        }
        
        /// <summary>Uses the `spacetime server ping` CLI command.</summary>
        /// <param name="cancelToken">If left default, set to 200ms timeout</param>
        public static async Task<PingServerResult> PingServerAsync(CancellationToken cancelToken = default)
        {
            if (cancelToken == default)
            {
                cancelToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(200)).Token;
            }
            
            const string argSuffix = "spacetime server ping";
            SpacetimeCliResult cliResult = await runCliCommandAsync(argSuffix, cancelToken);
            PingServerResult pingServerResult = new(cliResult);
            return pingServerResult;
        }
        
        /// Uses the `spacetime start` CLI command. Runs in background in a detached service.
        /// Does not await online: Better for an init before a queued command.
        public static async Task StartDetachedLocalServer()
        {
            const string argSuffix = "spacetime start";
            startDetachedCliProcess(argSuffix);
        }
        
        /// Cross-platform kills process by port num (there's no universal `stop` command)
        public static async Task<SpacetimeCliResult> ForceStopLocalServerAsync(ushort port = SpacetimeMeta.DEFAULT_PORT)
        {
            string argSuffix = GetKillCommand(port);
            SpacetimeCliResult cliResult = await runCliCommandAsync(argSuffix);
            return cliResult;
        }
        
        /// <summary>Uses the `spacetime server ping` CLI command.</summary>
        /// <param name="serverName">This is most likely "local" || "testnet"</param>
        public static async Task<SpacetimeCliResult> CreateFingerprintAsync(string serverName)
        {
            string argSuffix = $"spacetime server fingerprint {serverName} --force";
            SpacetimeCliResult cliResult = await runCliCommandAsync(argSuffix);
            return cliResult;
        }
        #endregion // High Level CLI Actions
        
        
        #region Compounded Actions
        /// Uses the `spacetime start` CLI command. Runs in background in a detached service.
        /// Awaits up to 2s for the server to come online.
        public static async Task<PingServerResult> StartDetachedLocalServerWaitUntilOnlineAsync()
        {
            await StartDetachedLocalServer();
            
            // Await success, pinging the CLI every 100ms to ensure online. Max 2 seconds.
            return await PingServerUntilOnlineAsync();
        }
        
        /// <param name="cancelToken">If left default, set to 200ms timeout (2 attempts @ 1 per 100ms)</param>
        public static async Task<PingServerResult> PingServerUntilOnlineAsync(CancellationToken cancelToken = default)
        {
            // If default, set to 200ms timeout
            using CancellationTokenSource globalTimeoutCts = cancelToken == default
                ? new CancellationTokenSource(TimeSpan.FromMilliseconds(200)) 
                : CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
            
            try
            {
                while (!globalTimeoutCts.Token.IsCancellationRequested)
                {
                    using CancellationTokenSource pingIterationCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                    try
                    {
                        // Attempt to ping the server with a per-iteration timeout
                        PingServerResult pingServerResult = await PingServerAsync(pingIterationCts.Token);
                        bool isOnline = !pingServerResult.HasCliErr;

                        if (isOnline)
                        {
                            return pingServerResult;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // If the ping iteration was cancelled, we simply continue to the next iteration until global timeout.
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Global timeout cancellation: Timed out!
            }

            // Timed out
            return new PingServerResult(new SpacetimeCliResult("", "Canceled"));
        }
        #endregion // Compounded Actions
    }
}