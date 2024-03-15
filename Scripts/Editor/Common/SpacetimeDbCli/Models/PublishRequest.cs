namespace SpacetimeDB.Editor
{
    /// Info passed from the UI to CLI during the CLI `spacetime publish`
    /// Print ToString to get the CLI "--project-path {path} {module-name}"
    public class PublishRequest
    {
        /// Usage: "my-server-module-name"
        public string ServerModuleName { get; }

        /// Usage: "absolute/path/to/server/module/dir"
        public string ServerModulePath { get; }
        
        /// When true, appends -c to clear the db data
        public bool ClearDbData { get; }

        /// Returns what's sent to the CLI: "{clearDbStr}--project-path {path} {module-name}"
        public override string ToString()
        {
            string clearDbDataStr = ClearDbData ? "-c " : "";
            return $"{clearDbDataStr}--project-path \"{ServerModulePath}\" {ServerModuleName}";
        }
        

        public PublishRequest(
            string serverModuleName, 
            string serverModulePath,
            bool clearDbData)
        {
            this.ServerModuleName = serverModuleName;
            this.ServerModulePath = serverModulePath;
            this.ClearDbData = clearDbData;
        }
    }
}