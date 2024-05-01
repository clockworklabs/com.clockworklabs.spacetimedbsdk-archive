namespace SpacetimeDB.Editor
{
    /// Extends SpacetimeCliResult to catch specific installation results
    public class InstallDotnet8PlusResult : SpacetimeCliResult
    {
        /// Success if output contains "wasi-experimental"
        public bool installedDotnet8Plus { get; }

        
        public InstallDotnet8PlusResult(SpacetimeCliResult cliResult)
            : base(cliResult)
        {
            // ########################################################################################
            // TODO example CLI result
            // ########################################################################################
            
            // this.installedDotnet8Plus = cliResult.CliOutput.Contains("TODO");
        }
    }
}