using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SpacetimeDB.Editor
{
    /// Result of `spacetime identity list`
    public class GetIdentitiesResult : SpacetimeCliResult
    {
        public enum GetIdentitiesErrorCode
        {
            Unknown,
            
            /// Resolved via CLI cmd: `spacetime server fingerprint {serverName}`
            NoSavedFingerprint,
        }
        
        public List<SpacetimeIdentity> Identities { get; }
        public bool HasIdentity { get; }
        public bool HasIdentitiesButNoDefault { get; }
        
        
        public GetIdentitiesResult(SpacetimeCliResult cliResult)
            : base(cliResult.CliOutput, cliResult.CliError)
        {
            // Example raw list result below. Notice how the top hash has no associated Nickname.
            // ###########################################################################################
            /*
             DEFAULT  IDENTITY                                                          NAME            
                      1111111111111111111111111111111111111111111111111111111111111111                  
                      2222222222222222222222222222222222222222222222222222222222222222  Nickname2
                      3333333333333333333333333333333333333333333333333333333333333333  Nickname3       
             */
            // ###########################################################################################
            
            // Initialize the list to store nicknames
            this.Identities = new List<SpacetimeIdentity>();
            
            // Split the input string into lines considering the escaped newline characters
            string[] lines = CliOutput.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries); 
            const string pattern = @"(?:\*\*\*\s+)?\b[a-fA-F0-9]{64}\s+(.+)$"; // Captures nicknames

            foreach (string line in lines)
            {
                Match match = Regex.Match(line, pattern);
                if (!match.Success || match.Groups.Count <= 1)
                {
                    continue;
                }

                // Extract potential match
                string potentialNickname = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(potentialNickname))
                {
                    onIdentityFound(line, potentialNickname);
                }
            }
            
            this.HasIdentity = Identities?.Count > 0;;
            this.HasIdentitiesButNoDefault = HasIdentity && 
                !Identities.Exists(id => id.IsDefault);
        }
        
        /// Set identityNicknames and isDefault
        private void onIdentityFound(string line, string nickname)
        {
            // Determine if the newIdentity is marked as default by checking if the line contains ***
            bool isDefault = line.Contains("***");
            SpacetimeIdentity identity = new(nickname, isDefault);
            Identities.Add(identity);
        }
    }
}