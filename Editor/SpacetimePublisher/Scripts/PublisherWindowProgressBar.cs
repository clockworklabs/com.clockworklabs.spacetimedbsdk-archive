using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using static SpacetimeDB.Editor.SpacetimeWindow;

namespace SpacetimeDB.Editor
{
    public partial class PublisherWindow
    {
        #region State Tracking
        /// Allows for cleanly cancelling a progress bar when hidden
        private CancellationTokenSource _progressBarCts;
        #endregion // State Tracking
        
        
        /// Show progress bar, clamped to 1~100, updating every 1s
        /// Stops when reached 100, or if style display is hidden
        /// (!) Never await this if `autoHideOnComplete = false` or you'll await indefinitely
        private async Task startProgressBarAsync(
            ProgressBar progressBar,
            string barTitle = "Running CLI ...",
            int initVal = 5, 
            int valIncreasePerSec = 5,
            bool autoHideOnComplete = true)
        {
            progressBar.title = barTitle;
            progressBar.value = Mathf.Clamp(initVal, 1, 99);
            ShowUi(progressBar);

            _progressBarCts = new CancellationTokenSource();

            try
            {
                _ = spinProgressBarAsync(
                    progressBar, 
                    _progressBarCts.Token, 
                    speedSecs: valIncreasePerSec / 30f);

                while (progressBar.value <= 100 && IsShowingUi(progressBar))
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), _progressBarCts.Token);
                    progressBar.value += valIncreasePerSec;
                }

                if (autoHideOnComplete)
                {
                    HideUi(progressBar);
                }
            }
            catch (TaskCanceledException)
            {
            }
            finally
            {
                if (autoHideOnComplete)
                {
                    HideUi(progressBar);
                }
            }
        }

        /// <summary>
        /// Prepend spinner chars to the progress bar title for an animation loop
        /// </summary>
        /// <param name="progressBar">The progress bar to update.</param>
        /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
        /// <param name="speedSecs">Time in seconds between spinner updates.</param>
        private async Task spinProgressBarAsync(
            ProgressBar progressBar,
            CancellationToken cancellationToken,
            float speedSecs = 0.2f)
        {
            string[] spinner = SpacetimeMeta.PROGRESS_BAR_SPINNER_CHARS;
            int spinnerIndex = 0;

            while (!cancellationToken.IsCancellationRequested && IsShowingUi(progressBar))
            {
                // Retrieve the current title each time, allowing for external updates.
                string currentTitleWithoutSpinner = GetCurrentTitleWithoutSpinner(progressBar.title, spinner);
                progressBar.title = $"{spinner[spinnerIndex]} {currentTitleWithoutSpinner}";

                await Task.Delay(TimeSpan.FromSeconds(speedSecs), cancellationToken);
                spinnerIndex = (spinnerIndex + 1) % spinner.Length; // Loops if reached end
            }
        }

        /// <summary>
        /// Extracts the current title by removing any existing spinner character.
        /// </summary>
        /// <param name="currentTitle">The current title with spinner.</param>
        /// <param name="spinner">Array of spinner characters.</param>
        /// <returns>The title without the spinner character.</returns>
        private string GetCurrentTitleWithoutSpinner(string currentTitle, string[] spinner)
        {
            // Assuming the spinner is always at the start followed by a space, strip it off.
            foreach (var spin in spinner)
            {
                if (currentTitle.StartsWith(spin + " "))
                {
                    return currentTitle.Substring(spin.Length + 1);
                }
            }
            return currentTitle;  // Return original if no spinner was found.
        }
        
        
        // Call this method when you need to hide the UI and cancel ongoing operations
        private void hideProgressBarAndCancel(ProgressBar progressBar)
        {
            if (_progressBarCts is { IsCancellationRequested: false })
            {
                // Debug.Log($"Cancelling progressBar {progressBar.name}");
                _progressBarCts.Cancel();
            }
            
            HideUi(progressBar);
        }
    }
}