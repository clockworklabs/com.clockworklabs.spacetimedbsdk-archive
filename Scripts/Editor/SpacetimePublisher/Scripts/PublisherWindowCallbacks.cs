using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static SpacetimeDB.Editor.PublisherMeta;

namespace SpacetimeDB.Editor
{
    /// Handles direct UI callbacks, sending async Tasks to PublisherWindowActions.
    /// Subscribed to @ PublisherWindow.setOnActionEvents.
    /// Set @ setOnActionEvents(), unset at unsetActionEvents().
    /// This is essentially the middleware between UI and logic.
    public partial class PublisherWindow
    {
        #region Init from PublisherWindow.cs CreateGUI()
        /// Curry sync Actions from UI => to async Tasks
        private void setOnActionEvents()
        {
            if (topBannerBtn != null)
            {
                topBannerBtn.clicked += onTopBannerBtnClick;
            }
            if (serverSelectedDropdown != null)
            {
                // Show if !null
                serverSelectedDropdown.RegisterValueChangedCallback(
                    onServerSelectedDropdownChangedAsync);
            }
            if (serverAddNewShowUiBtn != null)
            {
                // Toggle reveals the "new server" groupbox UI
                serverAddNewShowUiBtn.clicked += onServerAddNewShowUiBtnClick;
            }
            if (serverNicknameTxt != null)
            {
                // Replace spaces with dashes
                serverNicknameTxt.RegisterValueChangedCallback(
                    onServerNicknameTxtChanged);
                
                // If using a reserved name, fill host + disable
                // Reveal the next Publish field, if ready
                serverNicknameTxt.RegisterCallback<FocusOutEvent>(
                    onServerNicknameFocusOut);
            }
            if (serverHostTxt != null)
            {
                // If valid, enable Add New Server btn
                serverHostTxt.RegisterCallback<FocusOutEvent>(
                    onServerHostTxtFocusOut);
            }
            if (serverAddBtn != null)
            {
                // Add new newServer
                serverAddBtn.clicked += onServerAddBtnClickAsync;
            }
            if (identitySelectedDropdown != null)
            {
                // Show if !null
                identitySelectedDropdown.RegisterValueChangedCallback(
                    onIdentitySelectedDropdownChangedAsync);
            }
            if (identityAddNewShowUiBtn != null)
            {
                // Toggle reveals the "new identity" groupbox UI
                identityAddNewShowUiBtn.clicked += onIdentityAddNewShowUiBtnClick;
            }
            if (identityNicknameTxt != null)
            {
                // Replace spaces with dashes
                identityNicknameTxt.RegisterValueChangedCallback(
                    onIdentityNicknameTxtChanged);

                identityNicknameTxt.RegisterCallback<FocusOutEvent>(
                    onIdentityNicknameFocusOut);
            }
            if (identityEmailTxt != null)
            {
                // Normalize email chars
                identityEmailTxt.RegisterValueChangedCallback(
                    onIdentityEmailTxtChanged);
 
                // If valid, enable Add New Identity btn
                identityEmailTxt.RegisterCallback<FocusOutEvent>(
                    onIdentityEmailTxtFocusOut);
            }
            if (identityAddBtn != null)
            {
                // Add new newIdentity
                identityAddBtn.clicked += onIdentityAddBtnClickAsync;
            }
            
            if (publishModulePathTxt != null)
            {
                // For init only
                publishModulePathTxt.RegisterValueChangedCallback(
                    onPublishModulePathTxtInitChanged);
  
                // If !empty, Reveal next UI group
                publishModulePathTxt.RegisterCallback<FocusOutEvent>(
                    onPublishModulePathTxtFocusOut);
            }
            if (publishPathSetDirectoryBtn != null)
            {
                // Show folder dialog -> Set path label
                publishPathSetDirectoryBtn.clicked += OnPublishPathSetDirectoryBtnClick;
            }
            if (publishModuleNameTxt != null)
            {
                // Suggest module name if empty
                publishModuleNameTxt.RegisterCallback<FocusOutEvent>(
                    onPublishModuleNameTxtFocusOut);
 
                // Replace spaces with dashes
                publishModuleNameTxt.RegisterValueChangedCallback(
                    onPublishModuleNameTxtChanged);
            }
            if (publishStartLocalServerBtn != null)
            {
                publishStartLocalServerBtn.clicked += onStartLocalServerBtnClick;
            }
            if (publishStopLocalServerBtn != null)
            {
                publishStopLocalServerBtn.clicked += onStopLocalServerBtnClick;
            }
            if (publishBtn != null)
            {
                // Start publishAsync chain
                publishBtn.clicked += onPublishBtnClickAsync;
            }
            if (publishCancelBtn != null)
            {
                // Cancel publishAsync chain
                publishCancelBtn.clicked += onCancelPublishBtnClick;
            }
            if (publishResultIsOptimizedBuildToggle != null)
            {
                // Show [Install Package] btn if !optimized
                publishResultIsOptimizedBuildToggle.RegisterValueChangedCallback(
                    onPublishResultIsOptimizedBuildToggleChanged);
            }
            if (installWasmOptBtn != null)
            {
                // Curry to an async Task => install `wasm-opt` npm pkg
                installWasmOptBtn.clicked += onInstallWasmOptBtnClick;
            }
            if (publishResultGenerateClientFilesBtn != null)
            {
                // Generate SDK via CLI `spacetime generate`
                publishResultGenerateClientFilesBtn.clicked += onPublishResultGenerateClientFilesBtnClick;
            }
            if (publishResultGetServerLogsBtn != null)
            {
                // Generate SDK via CLI `spacetime logs`
                publishResultGetServerLogsBtn.clicked += onGetServerLogsBtnClick;
            }
        }

        /// Cleanup: This should parity the opposite of setOnActionEvents()
        private void unsetOnActionEvents()
        {
            if (topBannerBtn != null)
            {
                topBannerBtn.clicked -= onTopBannerBtnClick;
            }
            if (serverSelectedDropdown != null) 
            {
                serverSelectedDropdown.UnregisterValueChangedCallback(
                    onServerSelectedDropdownChangedAsync);
            }
            if (serverAddNewShowUiBtn != null) 
            {
                serverAddNewShowUiBtn.clicked -= onServerAddNewShowUiBtnClick;
            }
            if (serverNicknameTxt != null) 
            {
                serverNicknameTxt.UnregisterValueChangedCallback(
                    onServerNicknameTxtChanged);

                serverNicknameTxt.UnregisterCallback<FocusOutEvent>(
                    onServerNicknameFocusOut);
            }
            if (serverHostTxt != null)
            {
                serverHostTxt.UnregisterCallback<FocusOutEvent>(
                    onServerHostTxtFocusOut);
            }
            if (serverAddBtn != null)
            {
                serverAddBtn.clicked -= onServerAddBtnClickAsync;
            }
            if (identitySelectedDropdown != null)
            {
                identitySelectedDropdown.RegisterValueChangedCallback(
                    onIdentitySelectedDropdownChangedAsync);
            }
            if (identityNicknameTxt != null)
            {
                identityNicknameTxt.UnregisterValueChangedCallback(
                    onIdentityNicknameTxtChanged);

                identityNicknameTxt.UnregisterCallback<FocusOutEvent>(
                    onIdentityNicknameFocusOut);
            }
            if (identityEmailTxt != null)
            {
                identityEmailTxt.UnregisterValueChangedCallback(
                    onIdentityEmailTxtChanged);

                identityEmailTxt.UnregisterCallback<FocusOutEvent>(
                    onIdentityEmailTxtFocusOut);
            }
            if (identityAddBtn != null)
            {
                identityAddBtn.clicked -= onIdentityAddBtnClickAsync;
            }
            if (publishModulePathTxt != null)
            {
                // For init only; likely already unsub'd itself
                publishModulePathTxt.UnregisterValueChangedCallback(
                    onPublishModulePathTxtInitChanged);

                publishModulePathTxt.UnregisterCallback<FocusOutEvent>(
                    onPublishModulePathTxtFocusOut);
            }
            if (publishPathSetDirectoryBtn != null)
            {
                publishPathSetDirectoryBtn.clicked -= OnPublishPathSetDirectoryBtnClick;
            }
            if (publishModuleNameTxt != null)
            {
                publishModuleNameTxt.UnregisterCallback<FocusOutEvent>(
                    onPublishModuleNameTxtFocusOut);

                publishModuleNameTxt.UnregisterValueChangedCallback(onPublishModuleNameTxtChanged);
            }
            if (publishStartLocalServerBtn != null)
            {
                publishStartLocalServerBtn.clicked -= onStartLocalServerBtnClick;
            }
            if (publishStopLocalServerBtn != null)
            {
                publishStopLocalServerBtn.clicked -= onStopLocalServerBtnClick;
            }
            if (publishBtn != null)
            {
                publishBtn.clicked -= onPublishBtnClickAsync;
            }
            if (publishResultIsOptimizedBuildToggle != null)
            {
                publishResultIsOptimizedBuildToggle.UnregisterValueChangedCallback(
                    onPublishResultIsOptimizedBuildToggleChanged);
            }
            if (installWasmOptBtn != null)
            {
                installWasmOptBtn.clicked -= onInstallWasmOptBtnClick;
            }
            if (publishResultGenerateClientFilesBtn != null)
            {
                publishResultGenerateClientFilesBtn.clicked -= onPublishResultGenerateClientFilesBtnClick;
            }
            if (publishResultGetServerLogsBtn != null)
            {
                publishResultGetServerLogsBtn.clicked -= onGetServerLogsBtnClick;
            }
        }
        
        
        private async void onStopLocalServerBtnClick()
        {
            try
            {
                await stopLocalServer();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                throw;
            }
        }

        private async void onStartLocalServerBtnClick()
        {
            try
            {
                await startLocalServer();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                throw;
            }
        }

        /// Cleanup when the UI is out-of-scope
        private void OnDisable() => unsetOnActionEvents();
        #endregion // Init from PublisherWindow.cs CreateGUI()
        
        
        #region Direct UI Callbacks
        /// Open link to SpacetimeDB Module docs
        private void onTopBannerBtnClick() =>
            Application.OpenURL(TOP_BANNER_CLICK_LINK);
        
        /// Normalize with no spacing
        private void onIdentityNicknameTxtChanged(ChangeEvent<string> evt) =>
            identityNicknameTxt.SetValueWithoutNotify(replaceSpacesWithDashes(evt.newValue));

        private void onServerNicknameTxtChanged(ChangeEvent<string> evt) =>
            serverNicknameTxt.SetValueWithoutNotify(replaceSpacesWithDashes(evt.newValue));

        /// Change spaces to dashes
        private void onPublishModuleNameTxtChanged(ChangeEvent<string> evt) =>
            publishModuleNameTxt.SetValueWithoutNotify(replaceSpacesWithDashes(evt.newValue));
        
        /// Normalize with email formatting
        private void onIdentityEmailTxtChanged(ChangeEvent<string> evt)
        {
            if (string.IsNullOrWhiteSpace(evt.newValue))
            {
                return;
            }

            bool isEmailFormat = tryFormatAsEmail(evt.newValue, out string email);
            if (isEmailFormat)
            {
                identityEmailTxt.SetValueWithoutNotify(email);
            }
            else
            {
                identityEmailTxt.SetValueWithoutNotify(evt.previousValue); // Revert non-email attempt
            }
        }
        
        private async void onServerSelectedDropdownChangedAsync(ChangeEvent<string> evt)
        {
            bool selectedAnything = serverSelectedDropdown.index >= 0;
            
            // The old val could've beeen a placeholder "<color=yellow>Searching ...</color>" val
            bool oldValIsPlaceholderStr = selectedAnything && evt.previousValue.Contains("<");
            bool isHidden = isHiddenUi(serverSelectedDropdown);
            
            // We have "some" server loaded by runtime code; show this dropdown
            if (!selectedAnything || oldValIsPlaceholderStr)
            {
                return;
            }

            if (isHidden)
            {
                showUi(serverSelectedDropdown);
            }

            // We changed from a known server to another known one.
            // We should change the CLI default.
            string serverNickname = evt.newValue;
            Debug.Log($"Selected server changed to {serverNickname} (from {evt.previousValue})");
            
            // Process via CLI => Set default, revalidate identities
            try
            {
                await setDefaultServerRefreshIdentitiesAsync(serverNickname);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error: {e}");
                throw;
            }
        }
        
        /// This is hidden, by default, until a first newIdentity is added
        private async void onIdentitySelectedDropdownChangedAsync(ChangeEvent<string> evt)
        {
            bool selectedAnything = identitySelectedDropdown.index >= 0;
            bool isHidden = isHiddenUi(identitySelectedDropdown);
            
            // We have "some" newIdentity loaded by runtime code; show this dropdown
            if (!selectedAnything)
            {
                return;
            }

            if (isHidden)
            {
                showUi(identitySelectedDropdown);
            }

            // We changed from a known identity to another known one.
            // We should change the CLI default.
            try
            {
                await setDefaultIdentityAsync(evt.newValue);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error: {e}");
                throw;
            }
        }
        
        /// Used for init only, for when the persistent ViewDataKey
        private async void onPublishModulePathTxtInitChanged(ChangeEvent<string> evt)
        {
            await onPublishModulePathSetAsync();
            revealPublishResultCacheIfHostExists(openFoldout: null);
            publishModulePathTxt.UnregisterValueChangedCallback(onPublishModulePathTxtInitChanged);
        }
        
        /// Toggle newIdentity btn enabled based on email + nickname being valid
        private void onIdentityNicknameFocusOut(FocusOutEvent evt) =>
            checkIdentityReqsToggleIdentityBtn();
        
        /// Toggle newServer btn enabled based on email + nickname being valid
        private void onServerNicknameFocusOut(FocusOutEvent evt)
        {
            // Check for known aliases
            string normalizedHost = SpacetimeMeta.GetHostFromKnownServerName(serverNicknameTxt.text);
            bool isKnownAlias = normalizedHost != serverNicknameTxt.text;
            if (isKnownAlias)
            {
                serverHostTxt.value = normalizedHost;
                serverHostTxt.isReadOnly = true;
            }
            else
            {
                serverHostTxt.isReadOnly = false;
            }
            
            checkServerReqsToggleServerBtn();
        }
        
        /// Toggle newIdentity btn enabled based on nickname + email being valid
        private void onIdentityEmailTxtFocusOut(FocusOutEvent evt) =>
            checkIdentityReqsToggleIdentityBtn();
        
        /// Toggle newServer btn enabled based on nickname + host being valid
        private void onServerHostTxtFocusOut(FocusOutEvent evt) =>
            checkServerReqsToggleServerBtn();
        
        /// Toggle next section if !null
        private async void onPublishModulePathTxtFocusOut(FocusOutEvent evt)
        {
            // Prevent inadvertent UI showing too early, frozen on modal file picking
            if (_isFilePicking)
            {
                return;
            }

            bool hasPathSet = !string.IsNullOrEmpty(publishModulePathTxt.value);
            if (hasPathSet)
            {
                // Since we just changed the path, wipe old publishAsync info cache
                resetPublishResultCache();
                
                // Normalize, then reveal the next UI group
                publishModulePathTxt.value = superTrim(publishModulePathTxt.value);
                await revealPublisherGroupUiAsync();
            }
            else
            {
                hideUi(publishGroupBox);
            }
        }
        
        /// Explicitly declared and curried so we can unsubscribe
        /// There will *always* be a value for nameTxt
        private void onPublishModuleNameTxtFocusOut(FocusOutEvent evt) =>
            suggestModuleNameIfEmpty();

        /// Curry to an async Task to install `wasm-opt` npm pkg
        private async void onInstallWasmOptBtnClick()
        {
            try
            {
                await installWasmOptPackageViaNpmAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error: {e.Message}");
                throw;
            }
        }
        
        /// Run CLI cmd `spacetime logs`
        private async void onGetServerLogsBtnClick()
        {
            try
            {
                await getServerLogsAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error: {e}");
                throw;
            }
        }        
        
        /// Run CLI cmd `spacetime generate`
        private async void onPublishResultGenerateClientFilesBtnClick()
        {
            try
            {
                await generateClientFilesAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error: {e}");
                throw;
            }
        }
        
        /// Toggles the "new server" group UI
        private void onServerAddNewShowUiBtnClick()
        {
            bool isHidden = isHiddenUi(serverNewGroupBox);
            if (isHidden)
            {
                // Show + UX: Focus the 1st field
                showUi(serverNewGroupBox);
                serverAddNewShowUiBtn.text = SpacetimeMeta.GetStyledStr(
                    SpacetimeMeta.StringStyle.Success, "-"); // Show opposite, styled
                serverNicknameTxt.Focus();
                serverNicknameTxt.SelectAll();
            }
            else
            {
                // Hide
                hideUi(serverNewGroupBox);
                serverAddNewShowUiBtn.text = "+"; // Show opposite
            }
        }
        
        /// Toggles the "new identity" group UI
        private void onIdentityAddNewShowUiBtnClick()
        {
            bool isHidden = isHiddenUi(identityNewGroupBox);
            if (isHidden)
            {
                // Show + UX: Focus the 1st field
                showUi(identityNewGroupBox);
                identityAddNewShowUiBtn.text = SpacetimeMeta.GetStyledStr(
                    SpacetimeMeta.StringStyle.Success, "-"); // Show opposite, styled
                identityNicknameTxt.Focus();
                identityNicknameTxt.SelectAll();
            }
            else
            {
                // Hide
                hideUi(identityNewGroupBox);
                identityAddNewShowUiBtn.text = "+"; // Show opposite
            }
        }
        
        /// Show folder dialog -> Set path label
        private async void OnPublishPathSetDirectoryBtnClick()
        {
            string pathBefore = publishModulePathTxt.value;
            // Show folder panel (modal FolderPicker dialog)
            _isFilePicking = true;
            
            string selectedPath = EditorUtility.OpenFolderPanel(
                "Select Server Module Dir", 
                Application.dataPath, 
                "");
            
            _isFilePicking = false;
            
            // Canceled or same path?
            bool pathChanged = selectedPath == pathBefore;
            if (string.IsNullOrEmpty(selectedPath) || pathChanged)
            {
                return;
            }

            // Path changed: set path val + reveal next UI group
            publishModulePathTxt.value = selectedPath;
            await onPublishModulePathSetAsync();
        }
        
        /// Show [Install Package] btn if !optimized
        private void onPublishResultIsOptimizedBuildToggleChanged(ChangeEvent<bool> evt)
        {
            bool isOptimized = evt.newValue;
            if (isOptimized)
            {
                hideUi(installWasmOptBtn);                
            }
            else
            {
                showUi(installWasmOptBtn);
            }
        }
        
        private async void onIdentityAddBtnClickAsync()
        {
            string nickname = identityNicknameTxt.value;
            string email = identityEmailTxt.value;
            
            try
            {
                await addIdentityAsync(nickname, email);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error: {e}");
                throw;
            }
        }

        /// AKA AddServerBtnClick
        private async void onServerAddBtnClickAsync()
        {
            string nickname = serverNicknameTxt.value;
            string host = serverHostTxt.value;
            
            try
            {
                await addServerAsync(nickname, host);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error: {e}");
                throw;
            }
        }

        private async void onCancelPublishBtnClick()
        {
            Debug.Log("Warning: Cancelling Publish...");

            try
            {
                _publishCts.Cancel();
                _publishCts.Dispose();
            }
            catch (ObjectDisposedException e)
            {
                // Already disposed - np
            }

            // Hide UI: Progress bar, cancel btn
            hideUi(publishInstallProgressBar);
            hideUi(publishCancelBtn);

            // Show UI: Canceled status, publish btn
            publishStatusLabel.text = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Error, "Canceled");
            showUi(publishStatusLabel);
            showUi(publishBtn);
            
            // Slight cooldown, then enable publish btn
            publishBtn.SetEnabled(false);

            try
            {
                await WaitEnableElementAsync(publishBtn, TimeSpan.FromSeconds(1));
            }
            catch (Exception e)
            {
                Debug.LogError($"Error: {e}");
                throw;
            }
        }

        /// Curried to an async Task, wrapped this way so
        /// we can unsubscribe and for better err handling 
        private async void onPublishBtnClickAsync()
        {
            setPublishStartUi();
            
            try
            {
                await publishAsync();
            }
            catch (TaskCanceledException e)
            {
                publishCancelBtn.SetEnabled(false);
            }
            finally
            {
                hideUi(publishInstallProgressBar);
                _publishCts?.Dispose();
            }
        }
        #endregion // Direct UI Callbacks
    }
}
