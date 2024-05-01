using System.Text.RegularExpressions;

namespace SpacetimeDB.Editor
{
    /// Extends SpacetimeCliResult to catch specific `dotnet -version`
    /// results to check for .NET 8+
    public class CheckHasDotnet8PlusResult : SpacetimeCliResult
    {
        /// Success if output starts with 8+
        public bool HasDotnet8Plus { get; }

        
        public CheckHasDotnet8PlusResult(SpacetimeCliResult cliResult)
            : base(cliResult)
        {
            // ########
            // 8.0.2.04 << Match
            // ########

            const string pattern = @"^\d+";
            Match match = Regex.Match(cliResult.CliOutput, pattern);

            if (match.Success)
            {
                this.HasDotnet8Plus = int.TryParse(match.Value, out int majorVer) && majorVer >= 8;
            }
        }
    }
}