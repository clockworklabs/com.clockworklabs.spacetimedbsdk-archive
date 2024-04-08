using System.Text.RegularExpressions;

namespace SpacetimeDB.Editor
{
    /// Result from SpacetimeDbPublisherCli.StartLocalServerAsync
    public class StartLocalServerResult : SpacetimeCliResult
    {
        public bool StartedServer { get; }
        
        /// Eg: "127.0.0.1:3000" 
        public string IpAddress { get; }

        /// Eg: "http://127.0.0.1:3000" 
        public string FullHostUrl { get; }
        
        /// Eg: 3000
        public ushort Port { get; }
        
        public StartLocalServerResult(SpacetimeCliResult cliResult) : base(cliResult)
        {
            if (cliResult.HasCliErr)
            {
                return;
            }

            // #################################################
            // Starting SpacetimeDB listening on 127.0.0.1:3000
            // #################################################
            const string pattern = @"listening on (?<ip>\d{1,3}(?:\.\d{1,3}){3}):(?<port>\d+)";
            Match match = Regex.Match(cliResult.CliOutput, pattern);
            if (!match.Success)
            {
                return;
            }
            
            this.IpAddress = match.Groups["ip"].Value;
            
            string portString = match.Groups["port"].Value;
            this.Port = ushort.Parse(portString);

            this.FullHostUrl = $"http://{IpAddress}:{portString}";
            this.StartedServer = !string.IsNullOrEmpty(IpAddress);
        }

        public override string ToString() => FullHostUrl;
    }
}