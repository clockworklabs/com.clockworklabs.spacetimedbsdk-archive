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
            FalsePositiveNpmNotice,
        }
        

        public InstallWasmResult(SpacetimeCliResult cliResult)
            : base(cliResult)
        {
            // Err? Care of several false-positives!
            if (cliResult.HasCliErr)
            {
                // Check for false-positive err
                bool isFalsePositiveNpmNotice = cliResult.CliError.TrimStart().StartsWith("npm notice");
                bool isMissingNpm = SpacetimeDbCli.CheckCmdNotFound(cliResult.CliError, expectedCmd: "npm");

                if (isFalsePositiveNpmNotice)
                {
                    this.InstallWasmError = InstallWasmErrorType.FalsePositiveNpmNotice;
                }
                else if (isMissingNpm)
                {
                    this.InstallWasmError = InstallWasmErrorType.NpmNotRecognized; // Critical err
                    return;
                }
            }

            // Success?
            string trimmedOutput = cliResult.CliOutput.TrimStart();
            this.IsSuccessfulInstall =
                trimmedOutput.StartsWith("changed") ||
                trimmedOutput.StartsWith("added");

            if (this.IsSuccessfulInstall)
            {
                return;
            }
            

        }
    }
}