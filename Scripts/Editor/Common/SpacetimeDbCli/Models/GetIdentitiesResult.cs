using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SpacetimeDB.Editor
{
    /// Result of `spacetime identity list`
    public class GetIdentitiesResult : SpacetimeCliResult
    {
        public List<SpacetimeIdentity> Identities { get; private set; }
        public bool HasIdentity => Identities?.Count > 0;
        public bool HasIdentitiesButNoDefault => HasIdentity && 
            Identities.Exists(id => id.IsDefault);
        
        
        public GetIdentitiesResult(SpacetimeCliResult cliResult)
            : base(cliResult.CliOutput, cliResult.CliError)
        {
            // Example raw list result below. Read from bottom-up.
            // Ignore the top hashes (TODO: What are top hashes?)
            // ###########################################################################################
            /*
             DEFAULT  IDENTITY                                                          NAME            
                      1111111111111111111111111111111111111111111111111111111111111111                  
                      2222222222222222222222222222222222222222222222222222222222222222                  
                      3333333333333333333333333333333333333333333333333333333333333333                  
                      4444444444444444444444444444444444444444444444444444444444444444                  
                      5555555555555555555555555555555555555555555555555555555555555555                  
                      6666666666666666666666666666666666666666666666666666666666666666  Nickname1 
                 ***  7777777777777777777777777777777777777777777777777777777777777777  Nickname2
             */
            // ###########################################################################################
            
            // Initialize the list to store nicknames
            this.Identities = new List<SpacetimeIdentity>();
            
            // Split the input string into lines considering the escaped newline characters
            string[] lines = CliOutput.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries); 

            // Corrected regex pattern to ensure it captures the nickname following the hash and spaces
            // This pattern assumes the nickname is the last element in the line after the hash
            const string pattern = @"(?:\*\*\*\s+)?\b[a-fA-F0-9]{64}\s+(.+)$";

            foreach (string line in lines)
            {
                Match match = Regex.Match(line, pattern);
                if (!match.Success || match.Groups.Count <= 1)
                    continue;
                
                // Extract potential match
                string potentialNickname = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(potentialNickname))
                    onIdentityFound(line, potentialNickname);
            }
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