namespace SpacetimeDB.Editor
{
    /// Extends SpacetimeCliResult to catch specific `npm i -g wasm-opt` results
    public class InstallWasmResult : SpacetimeCliResult
    {
        /// Detects false-positive CliError:
        /// Success if CliOutput "changed {x} packages in {y}s"
        public bool IsSuccessfulInstall { get; }

        public InstallWasmErrorType InstallWasmError { get; } 
            
        public enum InstallWasmErrorType
        {
            Unknown,
            NpmNotRecognized,
        }
        

        public InstallWasmResult(SpacetimeCliResult cliResult)
            : base(cliResult)
        {
            this.IsSuccessfulInstall = cliResult.CliOutput
                .TrimStart()
                .StartsWith("changed ");

            if (this.IsSuccessfulInstall)
            {
                return;
            }
            
            bool missingNpm = SpacetimeDbCli.CheckCmdNotFound(cliResult.CliError, "npm");
            if (missingNpm)
            {
                this.InstallWasmError = InstallWasmErrorType.NpmNotRecognized;
            }
            else
            {
                this.InstallWasmError = InstallWasmErrorType.Unknown;
            }
        }
    }
}