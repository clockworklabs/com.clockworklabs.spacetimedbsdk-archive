using System.Diagnostics;
using System.Threading;

namespace SpacetimeDB.Editor
{
    /// Request data for SpacetimeStreamingCli
    public class SpacetimeStreamingCliRequest
    {
        /// For `None`, instead set !ContinueFollowingLogsAfterDone
        public enum StreamingLogsErrorLevel
        {
            InfoAndWarn,
            Error,
        }

        /// Only applicable if ContinueFollowingLogsAfterDone.
        /// For `None`, instead set !ContinueFollowingLogsAfterDone
        public StreamingLogsErrorLevel LogsErrorLevel { get; set; } = StreamingLogsErrorLevel.Error;
        
        /// This process passsed EnableRaisingEvents, so it's live/async for streaming logs
        /// - Try: ActiveProcess.OutputDataReceived +- onOutputDataReceived;
        /// - Try: ActiveProcess.ErrorDataReceived +- onErrorDataReceived;
        /// - When done, call: StopCancelDispose();
        public Process ActiveProcess { get; }
        
        /// On err, dispose?
        public bool StopOnError { get; }

        /// Will we still continue to see logs/errs as new ones come in *after* success || err?
        /// - To unfollow, call result UnsubToLogs || UnsubToOutputLogs || UnsubToErrorLogs
        /// - To filter, change LogsErrorLevel
        public bool ContinueFollowingLogsAfterDone { get; }
        
        public CancellationTokenSource CancelTokenSrc { get; }

        
        public SpacetimeStreamingCliRequest(
            Process activeProcess, 
            bool stopOnError,
            bool continueFollowingLogsAfterDone,
            CancellationTokenSource cancelTokenSrc)
        {
            this.ActiveProcess = activeProcess;
            this.StopOnError = stopOnError;
            this.ContinueFollowingLogsAfterDone = continueFollowingLogsAfterDone;
            this.CancelTokenSrc = cancelTokenSrc;
        }
    }
}