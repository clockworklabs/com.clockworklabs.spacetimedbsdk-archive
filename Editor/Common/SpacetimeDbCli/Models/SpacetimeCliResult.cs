using System.Collections.Generic;

namespace SpacetimeDB.Editor
{
    /// Result from SpacetimeDbCli.runCliCommandAsync
    public class SpacetimeCliResult
    {
        public SpacetimeCliRequest CliRequest { get; }

        /// Raw, unparsed CLI output
        public string CliOutput { get; }
        
        /// This is the official error thrown by the CLI; it may not necessarily
        /// be as helpful as a friendlier error message likely within CliOutput.
        /// (!) Sometimes, this may not even be a "real" error. Double check output!
        public string CliError { get; }
        
        public List<string> ErrsFoundFromCliOutput { get; }
        public bool HasErrsFoundFromCliOutput => 
            ErrsFoundFromCliOutput?.Count > 0;

        /// Common, caught CLI Errs such as "NoFingerprint" || "Canceled"
        public CliErrorCode RawCliErrorCode { get; }
        public enum CliErrorCode
        {
            Unknown,
            NoFingerprint,
            StaleIdentityMismatchingKey,
            Canceled,
        }
        
        /// Did we pass a CancellationToken and cancel the operation?
        public bool Canceled { get; private set; }
        
        /// HasRawCliError || caught ErrsFoundFromCliOutput
        public bool HasCliErr { get; }
        
        /// CLI-triggered exit code -- nothing to do with SpacetimeDB
        public bool HasRawCliErr { get; }
        
        public SpacetimeCliResult(
            string cliOutput, 
            string cliError, 
            SpacetimeCliRequest cliRequest = null)
        {
            this.CliRequest = cliRequest;
            
            // To prevent strange log formatting when paths are present, we replace `\` with `/`
            this.CliOutput = cliOutput?.Replace("\\", "/");
            this.CliError = cliError?.Replace("\\", "/");
            this.HasRawCliErr = !string.IsNullOrWhiteSpace(CliError); 
            
            this.ErrsFoundFromCliOutput = getErrsFoundFromCliOutput();
            this.HasCliErr = HasRawCliErr || ErrsFoundFromCliOutput?.Count > 0;

            if (!HasRawCliErr)
            {
                return;
            }
            
            // ----------------------------
            // Raw CLI Errors
            if (CliError == "Canceled")
            {
                this.Canceled = true;
                this.RawCliErrorCode = CliErrorCode.Canceled;
            }
            else if (CliError.Contains("No fingerprint"))
            {
                this.RawCliErrorCode = CliErrorCode.NoFingerprint;
            }
            else if (CliError.Contains("Token not signed by this instance"))
            {
                this.RawCliErrorCode = CliErrorCode.StaleIdentityMismatchingKey;
            }
        }

        private List<string> getErrsFoundFromCliOutput()
        {
            List<string> errsFound = new();
            string[] lines = CliOutput.Split('\n');
            
            foreach (string line in lines)
            {
                bool foundErr = line.Contains(": error CS");
                if (foundErr)
                {
                    errsFound.Add(line);
                }
            }
            return errsFound;
        }

        protected SpacetimeCliResult(SpacetimeCliResult cliResult)
        {
            this.CliRequest = cliResult.CliRequest;
            this.CliOutput = cliResult.CliOutput;
            this.CliError = cliResult.CliError;

            this.Canceled = cliResult.Canceled;
            this.RawCliErrorCode = cliResult.RawCliErrorCode;
            this.ErrsFoundFromCliOutput = cliResult.ErrsFoundFromCliOutput;
            this.HasCliErr = cliResult.HasCliErr;
            this.HasRawCliErr = cliResult.HasRawCliErr;
        }
    }
}
