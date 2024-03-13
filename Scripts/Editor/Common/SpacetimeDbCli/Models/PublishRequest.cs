namespace SpacetimeDB.Editor
{
    /// Info passed from the UI to CLI during the CLI `spacetime publish
    /// Print ToString to get the CLI "--project-path {path} {module-name}"
    public class PublishRequest
    {
        /// Usage: "my-server-module-name"
        public string ServerModuleName { get; private set; }

        /// Usage: "absolute/path/to/server/module/dir"
        public string ServerModulePath { get; private set; }

        /// Returns what's sent to the CLI: "--project-path {path} {module-name}"
        public override string ToString() => 
            $"--project-path \"{ServerModulePath}\" {ServerModuleName}";
        

        public PublishRequest(string serverModuleName, string serverModulePath)
        {
            this.ServerModuleName = serverModuleName;
            this.ServerModulePath = serverModulePath;
        }
    }
}