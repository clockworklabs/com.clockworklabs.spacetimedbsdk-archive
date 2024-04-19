using System.Diagnostics;
using UnityEngine;

namespace SpacetimeDB.Editor
{
    /// Request before calling a Process, often additionally included
    /// in the result (for debugging / err handling)
    public class SpacetimeCliRequest
    {
        /// "cmd" || "bin/bash"?
        public string TerminalCmd { get; }
        
        /// "-c" (Unix) || "/c" (Win)
        public string ArgPrefix { get; }
        
        /// The main command to run. Eg: `spacetime start`
        public string ArgSuffix { get; }
        
        /// Is this an attached child Process, but running in the
        /// background until Unity exits?
        public bool RunInBackground { get; }

        /// What opts did we use the run the Process?
        public ProcessStartInfo ProcessStartInfo { get; }
        
        /// Is this running completely in its own Process, separate from Unity
        /// (not a child Process)? Eg: `spacetime start` will generally use this.
        public bool IsDetachedProcess { get; }
        
        /// {MainCmd} \"{argSuffix}\"
        public string FullParsedArgs { get; }

        /// "PATH" (Win) || "Path" (Unix)?
        public string PathKeyName { get; }
        
        /// If we just installed SpacetimeDB or some other PATH val,
        /// did we update the ProcesssStartInfo.EnvironmentVariables? 
        public bool UpdatedPathEnvVar { get; }
        

        public SpacetimeCliRequest(
            string terminalCmd, 
            string argPrefix,
            string argSuffix,
            bool runInBackground,
            ProcessStartInfo processStartInfo)
        {
            this.TerminalCmd = terminalCmd;
            this.ArgPrefix = argPrefix;
            this.ArgSuffix = argSuffix;
            this.RunInBackground = runInBackground;
            this.ProcessStartInfo = processStartInfo;
            this.IsDetachedProcess = processStartInfo.UseShellExecute;
            this.FullParsedArgs = ProcessStartInfo.Arguments;
            this.PathKeyName = Application.platform == RuntimePlatform.WindowsEditor ? "Path" : "PATH";

            // (!) Mysterious observation: Simply getting .ContainsKey below will trigger
            // a detached process to fail without `!IsDetachedProcess &&`
            this.UpdatedPathEnvVar = !IsDetachedProcess && ProcessStartInfo
                .EnvironmentVariables.ContainsKey(PathKeyName);
        }
    }
}
