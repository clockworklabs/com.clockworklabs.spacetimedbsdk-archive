namespace SpacetimeDB.Editor
{
    /// Result from SpacetimeDbCli.runCliCommandAsync
    public class SpacetimeCliResult
    {
        /// Raw, unparsed CLI output
        public string CliOutput { get; private set; }
        
        /// This is the official error thrown by the CLI; it may not necessarily
        /// be as helpful as a friendlier error message likely within CliOutput.
        /// (!) Sometimes, this may not even be a "real" error. Double check output!
        public string CliError { get; private set; }
        
        /// Did we pass a CancellationToken and cancel the operation?
        public bool Cancelled { get; private set; }
        
        /// (!) While this may be a CLI error, it could be a false positive
        /// for what you really want to do. For example, `spacetime publish`
        /// will succeed, but throw a CliError for `wasm-opt` not found (unoptimized build).
        public bool HasCliErr => !string.IsNullOrWhiteSpace(CliError);
        
        
        public SpacetimeCliResult(string cliOutput, string cliError)
        {
            this.CliOutput = cliOutput;
            
            // To prevent strange log formatting when paths are present, we replace `\` with `/`
            this.CliError = cliError?.Replace("\\", "/");

            if (CliError == "Canceled")
                this.Cancelled = true;
        }
        
        public SpacetimeCliResult(SpacetimeCliResult cliResult)
        {
            this.CliOutput = cliResult.CliOutput;
            
            // To prevent strange log formatting when paths are present, we replace `\` with `/`
            this.CliError = cliResult.CliError?.Replace("\\", "/");

            if (CliError == "Canceled")
                this.Cancelled = true;
        }
    }
}
