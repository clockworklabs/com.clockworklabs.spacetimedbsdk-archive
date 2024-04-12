// using System;
// using System.Diagnostics;
// using System.Text;
//
// namespace SpacetimeDB.Editor
// {
//     /// Result from SpacetimeDbCli.runCliCommandAsync, but with a live async task that's not yet disposed.
//     /// - Start via StartProcess() when ready (*After* you subscribe to OnCli{Log} events).
//     /// - Don't forget to StopCancelDispose()!
//     [Obsolete("BUG: Domain refresh will both kill the process and freeze Unity")]
//     public class SpacetimeStreamingCliResult
//     {
//         public SpacetimeStreamingCliRequest Request { get; private set; }
//         
//         
//         #region Logs
//         /// Streaming output logs
//         public event Action<string> OnCliOutputReceived;
//
//         /// Streaming error logs
//         public event Action<string> OnCliErrorReceived;
//         
//         /// We're adding streaming output logs to this as they appear
//         public StringBuilder CliOutputBuilder { get; }
//
//         /// We're adding streaming err logs to this as they appear
//         public StringBuilder CliErrorBuilder { get; }
//         
//         public bool HasError => CliErrorBuilder?.Length > 0;
//         #endregion // Logs
//         
//         
//         /// Did we pass a CancellationToken and cancel the operation?
//         public bool Cancelled { get; private set; }
//         
//         
//         public SpacetimeStreamingCliResult(SpacetimeStreamingCliRequest request)
//         {
//             this.CliErrorBuilder = new StringBuilder();
//             this.CliOutputBuilder = new StringBuilder();
//             this.Request = request;
//             
//             Request.ActiveProcess.OutputDataReceived += OnOutputDataReceived;
//             Request.ActiveProcess.ErrorDataReceived += OnErrorDataReceived;
//         }
//         
//         public virtual void StartProcess()
//         {
//             try
//             {
//                 Request.ActiveProcess.Start();
//                 
//                 // Begin asynchronous read operations
//                 Request.ActiveProcess.BeginOutputReadLine();
//                 Request.ActiveProcess.BeginErrorReadLine();
//             }
//             catch (Exception e)
//             {
//                 StopCancelDispose();
//             }
//         }
//         
//         /// Warnings will trigger here, but will be curried to regular output
//         protected virtual void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
//         {
//             bool isRealErr = e.Data.Contains("error:");
//             if (!isRealErr)
//             {
//                 // Probably just a warning -- Curry to output
//                 OnOutputDataReceived(sender, e);
//                 return;
//             }
//             
//             this.CliErrorBuilder.AppendLine(e.Data);
//             this.OnCliErrorReceived?.Invoke(e.Data);
//
//             if (Request.StopOnError)
//             {
//                 StopCancelDispose();
//             }
//
//             bool isErrLogsLevel = Request.LogsErrorLevel == SpacetimeStreamingCliRequest.StreamingLogsErrorLevel.Error;
//             if (Request.ContinueFollowingLogsAfterDone && isErrLogsLevel)
//             {
//                 UnityEngine.Debug.LogError($"[{Request.ActiveProcess.MainWindowTitle}] {e.Data}");
//             }
//         }
//
//         /// Info + warnings
//         protected virtual void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
//         {
//             this.CliOutputBuilder.AppendLine(e.Data);
//             this.OnCliOutputReceived?.Invoke(e.Data);
//         }
//
//         
//         #region Cleanup
//
//         /// We either got a success || error. Light cleanup, based on Request prefs.
//         /// This includes unsubscribing to further streaming logs, unless Requested
//         public virtual void Done()
//         {
//             if (!Request.ContinueFollowingLogsAfterDone)
//             {
//                 UnsubToLogs();
//                 return;
//             }
//
//             // We're following the logs after done -- but specific ones?
//             if (Request.LogsErrorLevel == SpacetimeStreamingCliRequest.StreamingLogsErrorLevel.Error)
//             {
//                 // We want to continue following err logs only; NOT info/warn
//                 UnsubToOutputLogs();
//             }
//         }
//
//         /// Heavy cleanup
//         public virtual void StopCancelDispose()
//         {
//             try
//             {
//                 Request.CancelTokenSrc?.Cancel();
//                 Request.CancelTokenSrc?.Dispose();
//             }
//             catch
//             {
//             }
//
//             try
//             {
//                 if (Request.ActiveProcess is not null)
//                 {
//                     UnsubToLogs();
//                 
//                     if (!Request.ActiveProcess.HasExited)
//                     {
//                         Request.ActiveProcess.Kill();
//                     }
//                 
//                     Request.ActiveProcess.Dispose(); // No async ver for this Dispose
//                 }
//             }
//             catch
//             {
//             }
//             
//             Cancelled = true;
//         }
//
//         public void UnsubToOutputLogs()
//         {
//             OnCliOutputReceived = null;
//
//             try
//             {
//                 if (Request.ActiveProcess != null)
//                 {
//                     Request.ActiveProcess.OutputDataReceived -= OnOutputDataReceived;
//                 }
//             }
//             catch
//             {
//             }
//         }
//         
//         public void UnsubToErrorLogs()
//         {
//             OnCliErrorReceived = null;
//
//             try
//             {
//                 if (Request.ActiveProcess != null)
//                 {
//                     Request.ActiveProcess.ErrorDataReceived -= OnErrorDataReceived;
//                 }
//             }
//             catch
//             {
//             }
//         }
//
//         public void UnsubToLogs()
//         {
//             UnsubToOutputLogs();
//             UnsubToErrorLogs();
//         }
//
//         ~SpacetimeStreamingCliResult() => StopCancelDispose();
//         #endregion // Cleanup
//     }
// }