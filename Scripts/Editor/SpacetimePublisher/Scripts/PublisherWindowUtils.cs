using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpacetimeDB.Editor
{
    /// Validations, trimming, special formatting
    public partial class PublisherWindow 
    {
        /// Checked at OnFocusOut events to ensure both nickname+email txt fields are valid.
        /// Toggle identityAddBtn enabled based validity of both.
        private void checkIdentityReqsToggleIdentityBtn()
        {
            bool isNicknameValid = !string.IsNullOrWhiteSpace(identityNicknameTxt.value);
            bool isEmailValid = checkIsValidEmail(identityEmailTxt.value);
            identityAddBtn.SetEnabled(isNicknameValid && isEmailValid);
        }
        
        /// Checked at OnFocusOut events to ensure both nickname-host txt fields are valid.
        /// Toggle serverAddBtn enabled based validity of both.
        private void checkServerReqsToggleServerBtn()
        {
            bool isHostValid = checkIsValidUrl(serverHostTxt.value);
            bool isNicknameValid = !string.IsNullOrWhiteSpace(serverNicknameTxt.value);
            serverAddBtn.SetEnabled(isNicknameValid && isHostValid);
        }
        
        private void resetCancellationTokenSrc()
        {
            _publishCts?.Dispose();
            _publishCts = new CancellationTokenSource();
        }

        /// <returns>
        /// dashified-project-name, suggested based on your project name.
        /// Swaps out `client` keyword with `server`.</returns>
        private string getSuggestedServerModuleName()
        {
            // Prefix "unity-", dashify the name, replace "client" with "server (if found).
            // Use Unity's productName
            string unityProjectName = $"unity-{Application.productName.ToLowerInvariant()}";
            string projectNameDashed = Regex
                .Replace(unityProjectName, @"[^a-z0-9]", "-")
                .Replace("client", "server");

            return projectNameDashed;
        }
        
        /// Great for adding a cooldown to a button, for example after a successful cancel
        private static async Task WaitEnableElementAsync(VisualElement element, TimeSpan timespan)
        {
            await Task.Delay(timespan);
            element.SetEnabled(true);
        }
        
        private static string replaceSpacesWithDashes(string str) =>
            str?.Replace(" ", "-");
        
        /// Remove ALL whitespace from string
        private static string superTrim(string str) =>
            str?.Replace(" ", "");

        /// This checks for valid email chars for OnChange events
        private static bool tryFormatAsEmail(string input, out string formattedEmail)
        {
            formattedEmail = null;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            // Simplified regex pattern to allow characters typically found in emails
            const string emailCharPattern = @"^[a-zA-Z0-9@._+-]+$"; // Allowing "+" (email aliases)
            if (!Regex.IsMatch(input, emailCharPattern))
            {
                return false;
            }

            formattedEmail = input;
            return true;
        }

        /// Useful for FocusOut events, checking the entire email for being valid.
        /// At minimum: "a@b.c"
        private static bool checkIsValidEmail(string emailStr)
        {
            // No whitespace, contains "@" contains ".", allows "+" (alias), contains chars in between
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(emailStr, pattern);
        }

        /// Useful for FocusOut events, checking the entire host for being valid.
        /// At minimum, must start with "http".
        private static bool checkIsValidUrl(string url) => url.StartsWith("http");

        /// Hide a visual element via DisplayStyle.None
        /// - (!) Ripples the UI, as if removing it completely
        /// - (!) Does not trigger transition animations
        private static void hideUi(VisualElement element) =>
            element.style.display = DisplayStyle.None;
        
        /// Show the UI via DisplayStyle.Flex + set opacity to 100%, triggering `transition` animations
        /// - (!) Ripples the UI as if it was just dragged into view
        /// - Optionally, useVisibilityNotDisplay to use `.visible` instead of `.style.display`
        /// if you initially hid via hideUiNoRipple()
        private static void showUi(VisualElement element, bool useVisibilityNotDisplay = false)
        {
            element.style.opacity = 0;
            
            if (useVisibilityNotDisplay)
            {
                element.visible = true;
                return;
            }
            
            element.style.display = DisplayStyle.Flex;
            element.style.opacity = 1;
        }
        
        /// Hide a visual element via setting visible to false
        /// - (!) Does not ripple the UI, as if it's still there
        /// - (!) Does not trigger transition animations
        /// - Show again via showUi(element, useVisibilityNotDisplay: true)
        private static void hideUiNoRipple(VisualElement element) =>
            element.visible = false;
        
        /// Sets opacity to 0, triggering `transition` properties, if set
        /// - (!) Does not ripple the UI, as if it's still there
        private static void fadeOutUi(VisualElement element) =>
            element.style.opacity = 0;
        
        /// <returns>True if: DisplayStyle.None || 0 opacity || !visible</returns>
        public bool isHiddenUi(VisualElement element) =>
            rootVisualElement.style.display == DisplayStyle.None ||
            rootVisualElement.style.opacity == 0 ||
            !rootVisualElement.visible;
        
        public bool isShowingUi(VisualElement element) =>
            rootVisualElement.style.display == DisplayStyle.Flex ||
            rootVisualElement.style.opacity == 1 ||
            rootVisualElement.visible;
    }
}