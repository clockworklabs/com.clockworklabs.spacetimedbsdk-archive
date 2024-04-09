using System.Diagnostics;
using System.Text.RegularExpressions;
using UnityEditor;

namespace SpacetimeDB.Editor
{
    /// Result from SpacetimeDbPublisherCli.StartLocalServerAsync
    public class StreamingLocalServer : SpacetimeStreamingCliResult
    {
        public bool StartedServer { get; private set; }
        
        /// Eg: "127.0.0.1:3000" 
        public string IpAddress { get; private set; }

        /// Eg: "http://127.0.0.1:3000" 
        public string FullHostUrl { get; private set; }
        
        /// Eg: 3000
        public ushort Port { get; private set; }
        
        
        public StreamingLocalServer(SpacetimeStreamingCliResult streamingCliResult)
            : base(streamingCliResult.Request)
        {
            // (!) Props will be updated @ streamling err/output log overrides
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private void OnBeforeAssemblyReload() =>
            StopCancelDispose();

        /// Watch for "Starting SpacetimeDB listening on {host}"
        protected override void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            // #################################################
            // Starting SpacetimeDB listening on 127.0.0.1:3000
            // #################################################
            const string pattern = @"listening on (?<ip>\d{1,3}(?:\.\d{1,3}){3}):(?<port>\d+)";
            Match match = Regex.Match(e.Data, pattern);
            if (!match.Success)
            {
                return;
            }
            
            this.IpAddress = match.Groups["ip"].Value;
            
            string portString = match.Groups["port"].Value;
            this.Port = ushort.Parse(portString);

            this.FullHostUrl = $"http://{IpAddress}:{portString}";
            this.StartedServer = !string.IsNullOrEmpty(IpAddress);
            
            base.OnOutputDataReceived(sender, e);
        }

        public override string ToString() => FullHostUrl;
    }
}