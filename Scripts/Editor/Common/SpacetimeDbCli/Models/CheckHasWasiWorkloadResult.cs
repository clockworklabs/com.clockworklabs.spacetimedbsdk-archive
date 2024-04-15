namespace SpacetimeDB.Editor
{
    /// Extends SpacetimeCliResult to catch specific `dotnet workload list`
    /// results to check for `wasi-experimental
    public class CheckHasWasiWorkloadResult : SpacetimeCliResult
    {
        /// Success if output contains "wasi-experimental"
        public bool HasWasiWorkload { get; }

        
        public CheckHasWasiWorkloadResult(SpacetimeCliResult cliResult)
            : base(cliResult)
        {
            // ########################################################################################
            // Installed Workload Id      Manifest Version      Installation Source
            //     --------------------------------------------------------------------
            // wasi-experimental          8.0.4/8.0.100         SDK 8.0.200
            //
            // Use `dotnet workload search` to find additional workloads to install.
            // ########################################################################################
            
            this.HasWasiWorkload = cliResult.CliOutput.Contains("wasi-experimental");
        }
    }
}