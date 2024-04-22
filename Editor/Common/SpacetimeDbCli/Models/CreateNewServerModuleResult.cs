namespace SpacetimeDB.Editor
{
    /// Extends SpacetimeCliResult to catch specific `spacetime init` results
    public class CreateNewServerModuleResult : SpacetimeCliResult
    {
        /// Success == !CliError
        public bool IsSuccessfulInit { get; }
        
        /// Detects "missing cargo" (Rust's pkg mgr) from CLI output warning
        public bool HasCargo { get; }
        
        
        // missing cargo
        public CreateNewServerModuleResult(SpacetimeCliResult cliResult)
            : base(cliResult)
        {
            this.IsSuccessfulInit = !HasCliErr;
            this.HasCargo = !cliResult.CliOutput.Contains("missing cargo");
        }
    }
}