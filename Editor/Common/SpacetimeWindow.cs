using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace SpacetimeDB.Editor
{
    /// Common static utils class for a SpacetimeWindow editor tool
    public static class SpacetimeWindow
    {
        /// Clips input to maxLength. If we clipped anything,
        /// we'll replace the last 3 characters with "..."
        public static string ClipString(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input) || maxLength <= 0)
            {
                return string.Empty;
            }

            if (input.Length > maxLength)
            {
                return input[..(maxLength - 3)] + "...";
            }

            return input;
        }
        
        /// If missing, install SpacetimeDB CLI
        public static async Task EnsureHasSpacetimeDbCli()
        {
            SpacetimeCliResult cliResult = await SpacetimeDbCliActions.GetIsSpacetimeCliInstalledAsync();
            
            bool isInstalled = !cliResult.HasCliErr;
            if (isInstalled)
            {
                return;
            }
            
            // Not yet installed >>
            await InstallSpacetimeDbCliShowModalProgressBarAsync();
        }
        
        /// If you're installing outside of the Publisher tool, use this
        public static async Task InstallSpacetimeDbCliShowModalProgressBarAsync()
        {
            string title = "Installing SpacetimeDB CLI ...";
            EditorUtility.DisplayProgressBar(title, "ETA < 30s", 1f);

            try
            {
                await SpacetimeDbCli.InstallSpacetimeCliAsync();
            }
            catch (Exception e)
            {
                throw; // Already logged @ inner
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
        
        public static string ReplaceSpacesWithDashes(string str) =>
            str?.Replace(" ", "-");
        
        /// Remove ALL whitespace from string
        public static string SuperTrim(string str) =>
            str?.Replace(" ", "");
        
        /// Great for adding a cooldown to a button, for example after a successful cancel
        public static async Task WaitEnableElementAsync(VisualElement element, TimeSpan timespan)
        {
            await Task.Delay(timespan);
            element.SetEnabled(true);
        }
        
        /// Hide a visual element via DisplayStyle.None
        /// - (!) Ripples the UI, as if removing it completely
        /// - (!) Does not trigger transition animations
        /// - setOpacity0 to make it fade in on ShowUi(), if transition animation props set
        public static void HideUi(VisualElement element) =>
            element.style.display = DisplayStyle.None;
        
        /// Show the UI via DisplayStyle.Flex + set opacity to 100%, triggering `transition` animations
        /// - (!) Ripples the UI as if it was just dragged into view
        /// - Optionally, useVisibilityNotDisplay to use `.visible` instead of `.style.display`
        /// if you initially hid via hideUiNoRipple()
        public static void ShowUi(VisualElement element, bool useVisibilityNotDisplay = false)
        {
            // Don't mess with opacity if it's !enabled or a btn
            bool skipOpacity = element is Button || !element.enabledSelf;
            if (!skipOpacity)
            {
                element.style.opacity = 0;
            }
            
            if (useVisibilityNotDisplay)
            {
                element.visible = true;
                return;
            }
            
            element.style.display = DisplayStyle.Flex;
            
            if (!skipOpacity)
            {
                element.style.opacity = 1;
            }
        }
        
        /// Sets opacity to 0, triggering `transition` properties, if set
        /// - (!) Does not ripple the UI, as if it's still there
        public static void FadeOutUi(VisualElement element) =>
            element.style.opacity = 0;
        
        /// <returns>True if: DisplayStyle.None || 0 opacity || !visible</returns>
        public static bool IsHiddenUi(VisualElement element) =>
            element.resolvedStyle.display == DisplayStyle.None ||
            element.resolvedStyle.opacity == 0 ||
            !element.visible;

        public static bool IsShowingUi(VisualElement element) =>
            element.resolvedStyle.display == DisplayStyle.Flex;
        
        /// Copies to clipboard + logs
        public static void CopyToClipboard(string str)
        {
            GUIUtility.systemCopyBuffer = str;
            UnityEngine.Debug.Log($"Copied to clipboard: {str}");
        }

        public static string NormalizePath(string pathToProjFile) =>
            pathToProjFile.Replace(@"\", "/");
    }
}