using System.Text.RegularExpressions;

namespace SpacetimeDB.Editor
{
    /// Extends SpacetimeCliResult to catch specific `spacetime server ping` results
    public class PingServerResult : SpacetimeCliResult
    {
        public bool IsServerOnline { get; }
           
        /// Eg: "127.0.0.1:3000" 
        public string IpAddress { get; }
        
        /// Eg: "http://127.0.0.1:3000" 
        public string FullHostUrl { get; }
        
        /// Eg: 3000
        public ushort Port { get; }
        
        
        public PingServerResult(SpacetimeCliResult cliResult) 
            : base(cliResult)
        {
            // #################################################
            // Server is online: http://127.0.0.1:3000
            // #################################################
            const string pattern = @"online: (?<ip>\d{1,3}(?:\.\d{1,3}){3}):(?<port>\d+)";
            Match match = Regex.Match(cliResult.CliOutput, pattern);
            if (!match.Success)
            {
                return;
            }
            
            this.IpAddress = match.Groups["ip"].Value;
            
            string portString = match.Groups["port"].Value;
            this.Port = ushort.Parse(portString);
        
            this.FullHostUrl = $"http://{IpAddress}:{portString}";
            this.IsServerOnline = !string.IsNullOrEmpty(IpAddress);
        }
        
        public override string ToString() => FullHostUrl;
    }
}