using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Assertions.Must;
using UnityEngine.UIElements;
using static SpacetimeDB.Editor.PublisherMeta;

namespace SpacetimeDB.Editor
{
    /// Unlike PublisherWindowCallbacks, these are not called *directly* from UI.
    /// Runs an action -> Processes isSuccess -> calls success || fail @ PublisherWindowCallbacks.
    /// PublisherWindowCallbacks should handle try/catch (except for init chains).
    public partial class PublisherWindow
    {
        #region Init from PublisherWindow.CreateGUI
        /// Installs CLI tool, shows identity dropdown, gets identities.
        /// Initially called by PublisherWindow @ CreateGUI.
        private async Task initDynamicEventsFromPublisherWindow()
        {
            await ensureSpacetimeCliInstalledAsync();
            await getServersSetDropdown();
            await pingLocalServerSetBtnsAsync();
            // => Continues @ onGetServersSetDropdownSuccess()
            //     => Localhost? pingLocalServerSetBtnsAsync()
            // => Continues @ onGetSetIdentitiesSuccessEnsureDefault()
            // => Continues @ onEnsureIdentityDefaultSuccess()
            // => Finishes @ revealPublishResultCacheIfHostExists()
        }
        
        /// Initially called by PublisherWindow @ CreateGUI
        /// - Set to the initial state as if no inputs were set.
        /// - This exists so we can show all ui elements simultaneously in the
        ///   ui builder for convenience.
        /// - (!) If called from CreateGUI, after a couple frames,
        ///       any persistence from `ViewDataKey`s may override this.
        private void resetUi()
        {
            resetInstallCli();
            resetServer();
            resetIdentity();
            resetPublish();
            resetPublishResultCache();
            
            // Hide all foldouts and labels from Identity+ (show Server)
            toggleFoldoutRipple(startRippleFrom: FoldoutGroupType.Identity, show:false);
        }

        private void resetPublish()
        {
            // Hide publish
            hideUi(publishGroupBox);
            hideUi(publishCancelBtn);
            hideUi(publishInstallProgressBar);
            fadeOutUi(publishStatusLabel);
            resetPublishAdvanced();
            
            hideUi(publishLocalBtnsHoriz);
            toggleLocalServerStartOrStopBtnGroup(isOnline: false);
        }

        private void resetPublishAdvanced()
        {
            publishModuleDebugModeToggle.SetEnabled(false);
            publishModuleDebugModeToggle.value = false;
            publishModuleClearDataToggle.value = false;
        }

        private void resetIdentity()
        {
            hideUi(identityAddNewShowUiBtn);
            hideUi(identityNewGroupBox);
            resetIdentityDropdown();
            identitySelectedDropdown.value = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Action, "Discovering ..."); 
            identityAddBtn.SetEnabled(false);
        }

        private void resetServer()
        {
            hideUi(serverAddNewShowUiBtn);
            hideUi(serverNewGroupBox);
            serverNicknameTxt.value = "";

            serverHostTxt.value = "";
            serverHostTxt.isReadOnly = false;
            
            resetServerDropdown();
        }

        private void resetInstallCli()
        {
            hideUi(installCliGroupBox);
            hideUi(installCliProgressBar);
            hideUi(installCliStatusLabel);
        }

        /// Check for install => Install if !found -> Throw if err
        private async Task ensureSpacetimeCliInstalledAsync()
        {
            // Check if Spacetime CLI is installed => install, if !found
            SpacetimeCliResult cliResult = await SpacetimeDbCliActions.GetIsSpacetimeCliInstalledAsync();
            
            // Process result -> Update UI
            bool isSpacetimeCliInstalled = !cliResult.HasCliErr;
            if (isSpacetimeCliInstalled)
            {
                onSpacetimeCliAlreadyInstalled();
                return;
            }
            
            await installSpacetimeDbCliAsync();
        }

        private void setInstallSpacetimeDbCliUi()
        {
            // Command !found: Update status => Install now
            _ = startProgressBarAsync(
                installCliProgressBar, 
                barTitle: "Installing SpacetimeDB CLI ...",
                initVal: 4,
                valIncreasePerSec: 4,
                autoHideOnComplete: true);

            hideUi(installCliStatusLabel);
            showUi(installCliGroupBox);
        }

        private async Task installSpacetimeDbCliAsync()
        {
            setInstallSpacetimeDbCliUi();
            
            // Run CLI cmd
            InstallSpacetimeDbCliResult installResult = await SpacetimeDbCli.InstallSpacetimeCliAsync();
            
            // Process result -> Update UI
            bool isSpacetimeDbCliInstalled = installResult.IsInstalled;
            if (!isSpacetimeDbCliInstalled)
            {
                // Critical error: Spacetime CLI !installed and failed install attempt
                onInstallSpacetimeDbCliFail();
                return;
            }
            
            await onInstallSpacetimeDbCliSuccess();
        }

        /// We may need to restart Unity, due to env var refreshes, 
        /// since child Processes use Unity's launched env vars
        private async Task onInstallSpacetimeDbCliSuccess()
         {
            // Validate
            installCliProgressBar.title = "Validating SpacetimeDB CLI Installation ...";
            
            SpacetimeCliResult validateCliResult = await SpacetimeDbCliActions.GetIsSpacetimeCliInstalledAsync();
            bool isNotRecognizedCmd = validateCliResult.HasCliErr && validateCliResult.CliError.Contains("'spacetime' is not recognized");
            if (isNotRecognizedCmd)
            {
                // This is only a "partial" error: We probably installed, but the env vars didn't refresh
                // We need to restart Unity to refresh the spawned child Process env vars since manual refresh failed
                onInstallSpacetimeDbCliSoftFail(); // Throws
                return;
            }

            hideUi(installCliGroupBox);
        }

        /// Set common fail UI, shared between hard and soft fail funcs
        private void onInstallSpacetimeDbFailUi()
        {
            showUi(installCliStatusLabel);
            showUi(installCliGroupBox);
            hideUi(installCliProgressBar);
        }

        /// Technically success, but we need to restart Unity to refresh PATH env vars
        /// Throws to prevent further execution in the init chain
        private void onInstallSpacetimeDbCliSoftFail()
        {
            onInstallSpacetimeDbFailUi();
            
            // TODO: Cross-platform refresh env vars without having to restart Unity (surprisingly advanced)
            string successButRestartMsg = "<b>Successfully Installed SpacetimeDB CLI:</b>\n" +
                "Please restart Unity to refresh the CLI env vars";

            serverSelectedDropdown.SetEnabled(false);
            serverSelectedDropdown.SetValueWithoutNotify("Awaiting PATH Update (Unity Restart)");
            installCliStatusLabel.text = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Success, successButRestartMsg);

            throw new Exception("Successful install, but Unity must restart (to refresh PATH env vars)");
        }
        
        /// Throws Exception
        private void onInstallSpacetimeDbCliFail()
        {
            onInstallSpacetimeDbFailUi();

            string errMsg = "<b>Failed to Install Spacetime CLI:</b>\nSee logs";
            installCliStatusLabel.text = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Error, errMsg);
            
            throw new Exception(errMsg);
        }

        /// Try to get get list of Servers from CLI.
        /// This should be called at init at runtime from PublisherWIndow at CreateGUI time.
        private async Task getServersSetDropdown()
        {
            // Run CLI cmd
            GetServersResult getServersResult = await SpacetimeDbCliActions.GetServersAsync();
            
            // Process result -> Update UI
            bool isSuccess = getServersResult.HasServer;
            if (!isSuccess)
            {
                onGetSetServersFail(getServersResult);
                return;
            }
            
            // Success
            await onGetServersSetDropdownSuccess(getServersResult);
        }
        #endregion // Init from PublisherWindow.CreateGUI
        
        
        /// Success:
        /// - Get server list and ensure it's default
        /// - Refresh identities, since they are bound per-server
        private async Task onGetServersSetDropdownSuccess(GetServersResult getServersResult)
        {
            await onGetSetServersSuccessEnsureDefaultAsync(getServersResult.Servers);
            await getIdentitiesSetDropdown(); // Process and reveal the next UI group
        }

        /// Try to get list of Identities from CLI.
        /// (!) Servers must already be set.
        private async Task getIdentitiesSetDropdown()
        {
            Debug.Log($"Gathering identities for selected '{serverSelectedDropdown.value}' server...");
            
            // Sanity check: Is there a selected server?
            bool hasSelectedServer = serverSelectedDropdown.index >= 0;
            if (!hasSelectedServer)
            {
                Debug.LogError("Tried to get identities before server is selected");
                return;
            }
            
            // Run CLI cmd
            GetIdentitiesResult getIdentitiesResult = await SpacetimeDbCliActions.GetIdentitiesAsync();
            
            // Process result -> Update UI
            bool isSuccess = getIdentitiesResult.HasIdentity;
            if (!isSuccess)
            {
                onGetSetIdentitiesFail();
                return;
            }
            
            // Success
            await onGetSetIdentitiesSuccessEnsureDefault(getIdentitiesResult.Identities);
        }
        
        /// Validates if we at least have a host name before revealing
        /// bug: If you are calling this from CreateGUI, openFoldout will be ignored.
        private void revealPublishResultCacheIfHostExists(bool? openFoldout)
        {
            // Sanity check: Ensure host is set
            bool hasVal = !string.IsNullOrWhiteSpace(publishResultHostTxt.value);
            if (!hasVal)
            {
                return;
            }

            // Reveal the publishAsync result info cache
            showUi(publishResultFoldout);
            
            if (openFoldout != null)
            {
                publishResultFoldout.value = (bool)openFoldout;
            }
        }
        
        /// (1) Suggest module name, if empty
        /// (2) Reveal publisher group
        /// (3) Ensure spacetimeDB CLI is installed async
        private async Task onPublishModulePathSetAsync()
        {
            // We just updated the path - hide old publishAsync result cache
            hideUi(publishResultFoldout);
            
            // Set the tooltip to equal the path, since it's likely cutoff
            publishModulePathTxt.tooltip = publishModulePathTxt.value;
            
            // Since we changed the path, we should wipe stale publishAsync info
            resetPublishResultCache();
            
            // ServerModulePathTxt persists: If previously entered, show the publishAsync group
            bool hasPathSet = !string.IsNullOrEmpty(publishModulePathTxt.value);
            if (hasPathSet)
            {
                try
                {
                    // +Ensures SpacetimeDB CLI is installed async
                    await revealPublisherGroupUiAsync();
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                    throw;
                }
            }
        }
        
        /// Dynamically sets a dashified-project-name placeholder, if empty
        private void suggestModuleNameIfEmpty()
        {
            // Set the server module name placeholder text dynamically, based on the project name
            // Replace non-alphanumeric chars with dashes
            bool hasName = !string.IsNullOrEmpty(publishModuleNameTxt.value);
            if (hasName)
            {
                return; // Keep whatever the user customized
            }

            // Generate dashified-project-name fallback suggestion
            publishModuleNameTxt.value = getSuggestedServerModuleName();
        }
        
        /// (!) bug: If NO servers are found, including the default, we'll regenerate them back.
        private void onGetSetServersFail(GetServersResult getServersResult)
        {
            if (!getServersResult.HasServer && !_isRegeneratingDefaultServers)
            {
                Debug.Log("[BUG] No servers found; defaults were wiped: " +
                    "regenerating, then trying again...");
                _isRegeneratingDefaultServers = true;
                _ = regenerateServers();         
                return;
            }
            
            // Hide dropdown, reveal new ui group
            Debug.Log("No servers found - revealing 'add new server' group");

            // UI: Reset flags, clear cohices, hide selected server dropdown box
            _isRegeneratingDefaultServers = false; // in case we looped around to a fail
            serverSelectedDropdown.choices.Clear();
            hideUi(serverSelectedDropdown);
            
            // Show "add new server" group box, focus nickname
            showUi(serverNewGroupBox);
            serverNicknameTxt.Focus();
            serverNicknameTxt.SelectAll();
        }

        /// When local and testnet are missing, it's 99% due to a bug:
        /// We'll add them back. Assuming default ports (3000) and testnet targets.
        private async Task regenerateServers()
        {
            Debug.Log("Regenerating default servers: [ local, testnet* ] *Becomes default");
            
            // UI
            serverStatusLabel.text = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Action, 
                "<b>Regenerating default servers:</b>\n[ local, testnet* ]");
            showUi(serverStatusLabel);

            AddServerRequest addServerRequest = null;
            
            // Run CLI cmd: Add `local` server (forces `--no-fingerprint` so it doesn't need to be running now)
            addServerRequest = new(SpacetimeMeta.LOCAL_SERVER_NAME, SpacetimeMeta.LOCAL_HOST_URL);
            _ = await SpacetimeDbPublisherCliActions.AddServerAsync(addServerRequest);
            
            // Run CLI cmd: Add `testnet` server (becomes default)
            addServerRequest = new(SpacetimeMeta.TESTNET_SERVER_NAME, SpacetimeMeta.TESTNET_HOST_URL);
            _ = await SpacetimeDbPublisherCliActions.AddServerAsync(addServerRequest);
            
            // Success - try again
            _ = getServersSetDropdown();
        }

        private void onGetSetIdentitiesFail()
        {
            // Hide dropdown, reveal new ui group
            Debug.Log("No identities found - revealing 'add new identity' group");
            
            // UI: Reset choices, hide dropdown+new identity btn
            identitySelectedDropdown.choices.Clear();
            hideUi(identitySelectedDropdown);
            hideUi(identityAddNewShowUiBtn);
            
            // UI: Reveal "add new identity" group, reveal foldout
            showUi(identityNewGroupBox);
            showUi(identityFoldout);
            
            // UX: Focus Nickname field
            identityNicknameTxt.Focus();
            identityNicknameTxt.SelectAll();
        }

        /// Works around UI Builder bug on init that will add the literal "string" type to [0]
        private void resetIdentityDropdown()
        {
            identitySelectedDropdown.choices.Clear();
            identitySelectedDropdown.value = "";
            identitySelectedDropdown.index = -1;
        }
        
        /// Works around UI Builder bug on init that will add the literal "string" type to [0]
        private void resetServerDropdown()
        {
            serverSelectedDropdown.choices.Clear();
            serverSelectedDropdown.value = "";
            serverSelectedDropdown.index = -1;
            serverSelectedDropdown.value = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Action, "Discovering ...");
            
            hideUi(serverConnectingStatusLabel);
        }
        
        /// Set the selected identity dropdown. If identities found but no default, [0] will be set. 
        private async Task onGetSetIdentitiesSuccessEnsureDefault(List<SpacetimeIdentity> identities)
        {
            // Logs for each found, with default shown
            foreach (SpacetimeIdentity identity in identities)
                Debug.Log($"Found identity: {identity}");
            
            // Setting will trigger the onIdentitySelectedDropdownChangedAsync event @ PublisherWindow
            foreach (SpacetimeIdentity identity in identities)
            {
                identitySelectedDropdown.choices.Add(identity.Nickname);

                if (identity.IsDefault)
                {
                    // Set the index to the most recently-added one
                    int recentlyAddedIndex = identitySelectedDropdown.choices.Count - 1;
                    identitySelectedDropdown.index = recentlyAddedIndex;
                }
            }
            
            // Ensure a default was found
            bool foundIdentity = identities.Count > 0;
            bool foundDefault = identitySelectedDropdown.index >= 0;
            if (foundIdentity && !foundDefault)
            {
                Debug.LogError("Found Identities, but no default " +
                    $"Falling back to [0]:{identities[0].Nickname} and setting via CLI...");
                identitySelectedDropdown.index = 0;
            
                // We need a default identity set
                string nickname = identities[0].Nickname;
                await setDefaultIdentityAsync(nickname);
            }

            // Process result -> Update UI
            await onEnsureIdentityDefaultSuccessAsync();
        }
        
        private async Task onEnsureIdentityDefaultSuccessAsync()
        {
            // Allow selection, show [+] new reveal ui btn
            identitySelectedDropdown.pickingMode = PickingMode.Position;
            showUi(identityAddNewShowUiBtn);
            
            // Hide UI
            hideUi(identityStatusLabel);
            hideUi(identityNewGroupBox);
            
            // Show this identity foldout + dropdown, which may have been hidden
            // if a server was recently changed
            showUi(identityFoldout);
            showUi(identitySelectedDropdown);
            
            // Show the next section + UX: Focus the 1st field
            showUi(publishFoldout);
            showUi(identityFoldout);
            toggleDebugModeIfNotLocalhost(); // Always false if called from init
            publishModuleNameTxt.Focus();
            publishModuleNameTxt.SelectNone();
            
            // Continue even further to the publish button+?
            bool readyToPublish = checkIsReadyToPublish();
            if (readyToPublish)
            {
                await revealPublisherGroupUiAsync();
            }
            
            // If we have a cached result, show that (minimized)
            _foundIdentity = true;
            revealPublishResultCacheIfHostExists(openFoldout: false);
        }

        private bool checkIsReadyToPublish() =>
            !string.IsNullOrEmpty(publishModuleNameTxt.value) &&
            !string.IsNullOrEmpty(publishModulePathTxt.value);

        /// Only allow --debug for !localhost (for numerous reasons, including a buffer overload bug)
        /// Always false if called from init (since it will be "Discovering ...")
        private void toggleDebugModeIfNotLocalhost()
        {
            bool isLocalhost = checkIsLocalhostServerSelected();
            publishModuleDebugModeToggle.SetEnabled(isLocalhost);
        }

        /// Set the selected server dropdown. If servers found but no default, [0] will be set.
        /// Also can be called by OnAddServerSuccess by passing a single server
        private async Task onGetSetServersSuccessEnsureDefaultAsync(List<SpacetimeServer> servers)
        {
            // Logs for each found, with default shown
            foreach (SpacetimeServer server in servers)
                Debug.Log($"Found server: {server}");
            
            // Setting will trigger the onIdentitySelectedDropdownChangedAsync event @ PublisherWindow
            for (int i = 0; i < servers.Count; i++)
            {
                SpacetimeServer server = servers[i];
                serverSelectedDropdown.choices.Add(server.Nickname);

                if (server.IsDefault)
                {
                    // Set the index to the most recently-added one
                    int recentlyAddedIndex = serverSelectedDropdown.choices.Count - 1;
                    serverSelectedDropdown.index = recentlyAddedIndex;
                }
            }
            
            // Ensure a default was found
            bool foundServer = servers.Count > 0;
            bool foundDefault = serverSelectedDropdown.index >= 0;
            if (foundServer && !foundDefault)
            {
                Debug.LogError("Found Servers, but no default: " +
                    $"Falling back to [0]:{servers[0].Nickname} and setting via CLI...");
                serverSelectedDropdown.index = 0;
            
                // We need a default server set
                string nickname = servers[0].Nickname;
                await SpacetimeDbPublisherCliActions.SetDefaultServerAsync(nickname);
            }

            // Process result -> Update UI
            onEnsureServerDefaultSuccess();
        }

        private void onEnsureServerDefaultSuccess()
        {
            // Allow selection, show [+] new reveal ui btn
            serverSelectedDropdown.pickingMode = PickingMode.Position;
            showUi(serverAddNewShowUiBtn);
            
            // Hide UI
            hideUi(serverStatusLabel);
            hideUi(serverNewGroupBox);
            
            // Show the next section
            showUi(identityFoldout);
            
            _foundServer = true;
        }

        /// This will reveal the group and initially check for the spacetime cli tool
        private async Task revealPublisherGroupUiAsync()
        {
            // Show and enable group, but disable the publishAsync btn
            // to check/install Spacetime CLI tool
            publishGroupBox.SetEnabled(true);
            publishBtn.SetEnabled(false);
            setPublishReadyStatusIfOnline();
            showUi(publishStatusLabel);
            showUi(publishGroupBox);
            toggleDebugModeIfNotLocalhost();
            
            // If localhost, show start|stop server btns async on separate thread
            if (_foundServer)
            {
                await pingLocalServerSetBtnsAsync();
            }
        }

        /// 1. Shows or hide localhost btns if localhost
        /// 2. If localhost:
        ///     a. Pings the local server to see if it's online
        ///     b. Shows either Start|Stop local server btn
        ///     c. If offline, disable Publish btn
        private async Task pingLocalServerSetBtnsAsync()
        {
            hideUi(publishLocalBtnsHoriz);
            
            bool isLocalServer = checkIsLocalhostServerSelected();
            if (isLocalServer)
            {
                showUi(publishLocalBtnsHoriz);
            }
            else
            {
                hideUi(publishLocalBtnsHoriz);
                return;
            }
            
            Debug.Log("Localhost server selected: Pinging for online status ...");
            
            // Run CLI cmd
            bool isOnline = await checkIsLocalServerOnlineAsync();
            
            Debug.Log("Local server online? " + isOnline);
            toggleLocalServerStartOrStopBtnGroup(isOnline);
        }

        /// <returns>isOnline (successful ping) with short timeout</returns>
        private async Task<bool> checkIsLocalServerOnlineAsync()
        {
            Assert.IsTrue(checkIsLocalhostServerSelected(), $"Expected {nameof(checkIsLocalhostServerSelected)}");

            // Run CLI command with short timeout
            _lastServerPingSuccess = await SpacetimeDbCliActions.PingServerAsync();
            
            // Process result
            bool isSuccess = _lastServerPingSuccess.IsServerOnline;
            return isSuccess;
        }

        /// This includes the Publish btn, disabling if !online
        private void toggleLocalServerStartOrStopBtnGroup(bool isOnline)
        {
            if (isOnline)
            {
                hideUi(publishStartLocalServerBtn);
                showUi(publishStopLocalServerBtn);
                
                setStopLocalServerBtnTxt();
                setPublishReadyStatusIfOnline();
            }
            else // Offline
            {
                showUi(publishStartLocalServerBtn);
                hideUi(publishStopLocalServerBtn);
                
                setLocalServerOfflinePublishLabel();
            }

            publishBtn.SetEnabled(isOnline);
        }

        private void setLocalServerOfflinePublishLabel()
        {
            publishStatusLabel.text = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Error, 
                "Local server offline");
            showUi(publishStatusLabel);
        }
        
        /// Sets status label to "Ready" and enables+shows Publisher btn
        /// +Hides the cancel btn
        private void setPublishReadyStatusIfOnline()
        {
            showUi(publishStatusLabel);

            if (_lastServerPingSuccess?.IsServerOnline == false)
            {
                setLocalServerOfflinePublishLabel();
            }
            else
            {
                // Make it look satisfying
                hideUi(publishStatusLabel, setOpacity0ForFadeIn: true);
                publishStatusLabel.text = SpacetimeMeta.GetStyledStr(
                    SpacetimeMeta.StringStyle.Success, "Ready");
                showUi(publishStatusLabel); // Fades in
            }
            
            publishBtn.SetEnabled(true);
            showUi(publishBtn);
            publishBtn.text = "Publish";
 
            hideUi(publishCancelBtn);
        }
        
        /// Be sure to try/catch this with a try/finally to dispose `_cts
        private async Task publishAsync()
        {
            setPublishStartUi();
            resetCancellationTokenSrc();

            bool enableDebugMode = publishModuleDebugModeToggle.enabledSelf && publishModuleClearDataToggle.value;
            
            PublishRequest publishRequest = new(
                publishModuleNameTxt.value, 
                publishModulePathTxt.value,
                new PublishRequest.AdvancedOpts(
                    publishModuleClearDataToggle.value,
                    enableDebugMode
                ));
            
            // Run CLI cmd [can cancel]
            PublishResult publishResult = await SpacetimeDbPublisherCliActions.PublishAsync(
                publishRequest,
                _publishCts.Token);

            // Process result -> Update UI
            bool isSuccess = publishResult.IsSuccessfulPublish;
            Debug.Log($"PublishAsync success: {isSuccess}");
            if (isSuccess)
            {
                onPublishSuccess(publishResult);
            }
            else
            {
                onPublishFail(publishResult);
            }
        }
        
        /// Critical err - show label
        private void onPublishFail(PublishResult publishResult)
        {
            _cachedPublishResult = null;
            updatePublishStatus(
                SpacetimeMeta.StringStyle.Error, 
                publishResult.StyledFriendlyErrorMessage 
                    ?? Utils.ClipString(publishResult.CliError, maxLength: 4000));
        }
        
        /// There may be a false-positive wasm-opt err here; in which case, we'd still run success.
        /// Caches the module name into EditorPrefs for other tools to use. 
        private void onPublishSuccess(PublishResult publishResult)
        {
            _cachedPublishResult = publishResult;
            
            // Success - reset UI back to normal
            setPublishReadyStatusIfOnline();
            setPublishResultGroupUi(publishResult);
            
            // Other editor tools may want to utilize this value,
            // since the CLI has no idea what you're "default" Module is
            EditorPrefs.SetString(
                SpacetimeMeta.EDITOR_PREFS_MODULE_NAME_KEY, 
                publishModuleNameTxt.value);
        }

        private void setPublishResultGroupUi(PublishResult publishResult)
        {
            // Hide old status -> Load the result data
            hideUi(publishResultStatusLabel);
            publishResultDateTimeTxt.value = $"{publishResult.PublishedAt:G} (Local)";
            publishResultHostTxt.value = publishResult.UploadedToHost;
            publishResultDbAddressTxt.value = publishResult.DatabaseAddressHash;
            
            // Set via ValueWithoutNotify since this is a hacky "readonly" Toggle (no official feat for this, yet)
            publishResultIsOptimizedBuildToggle.value = publishResult.IsPublishWasmOptimized;
            
            // Show install pkg button, to optionally optimize next publish
            if (publishResult.IsPublishWasmOptimized)
            {
                hideUi(installWasmOptBtn);
            }
            else
            {
                showUi(installWasmOptBtn);
            }

            resetGenerateUi();
            
            // Show the result group and expand the foldout
            revealPublishResultCacheIfHostExists(openFoldout: true);
        }

        /// Show progress bar, clamped to 1~100, updating every 1s
        /// Stops when reached 100, or if style display is hidden
        private async Task startProgressBarAsync(
            ProgressBar progressBar,
            string barTitle = "Running CLI ...",
            int initVal = 5, 
            int valIncreasePerSec = 5,
            bool autoHideOnComplete = true)
        {
            progressBar.title = barTitle;
            
            // Prepare the progress bar style and min/max
            const int maxVal = 99;
            progressBar.value = Mathf.Clamp(initVal, 1, maxVal);
            showUi(progressBar);
            
            while (progressBar.value < 100 && isShowingUi(progressBar))
            {
                // Wait for 1 second, then update the bar
                await Task.Delay(TimeSpan.FromSeconds(1));
                progressBar.value += valIncreasePerSec;
                
                // In case we reach 99%+, we'll add and retract a "." to show progress is continuing
                if (progressBar.value >= maxVal)
                {
                    progressBar.title = progressBar.title.Contains("...")
                        ? progressBar.title.Replace("...", "....")
                        : progressBar.title.Replace("....", "...");    
                }
            }
            
            if (autoHideOnComplete)
            {
                hideUi(progressBar);
            }
        }

        /// Hide CLI group
        private void onSpacetimeCliAlreadyInstalled()
        {
            hideUi(installCliProgressBar);
            hideUi(installCliGroupBox);
        }

        /// Show a styled friendly string to UI. Errs will enable publishAsync btn.
        private void updatePublishStatus(SpacetimeMeta.StringStyle style, string friendlyStr)
        {
            publishStatusLabel.text = SpacetimeMeta.GetStyledStr(style, friendlyStr);
            showUi(publishStatusLabel);

            if (style != SpacetimeMeta.StringStyle.Error)
            {
                return; // Not an error
            }

            // Error: Hide cancel btn, cancel token, show/enable pub btn
            hideUi(publishCancelBtn);
            _publishCts?.Dispose();
            
            showUi(publishBtn);
            publishBtn.SetEnabled(true);
        }
        
        /// Yields 1 frame to update UI fast
        private void setPublishStartUi()
        {
            // Reset result cache
            resetPublishResultCache();
            
            // Hide: Publish btn, label, result foldout 
            hideUi(publishResultFoldout);
            fadeOutUi(publishStatusLabel);
            hideUi(publishBtn);
            
            // Show: Cancel btn, show progress bar,
            showUi(publishCancelBtn);
            _ = startProgressBarAsync(
                publishInstallProgressBar,
                barTitle: "Publishing to SpacetimeDB ...",
                autoHideOnComplete: false);
        }

        /// Set 'installing' UI
        private void setinstallWasmOptPackageViaNpmUi()
        {
            // Hide UI
            publishBtn.SetEnabled(false);
            installWasmOptBtn.SetEnabled(false);
            
            // Show UI
            installWasmOptBtn.text = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Action, "Installing ...");
            showUi(installCliProgressBar);
            
            _ = startProgressBarAsync(
                installWasmOptProgressBar,
                barTitle: "Installing `wasm-opt` via npm ...",
                autoHideOnComplete: false);
        }
        
        /// Install `wasm-opt` npm pkg for a "set and forget" publishAsync optimization boost
        private async Task installWasmOptPackageViaNpmAsync()
        {
            setinstallWasmOptPackageViaNpmUi();
            
            // Run CLI cmd
            InstallWasmResult installWasmResult = await SpacetimeDbPublisherCliActions.InstallWasmOptPkgAsync();

            // Process result -> Update UI
            bool isSuccess = installWasmResult.IsSuccessfulInstall;
            if (isSuccess)
            {
                onInstallWasmOptPackageViaNpmSuccess();
            }
            else
            {
                onInstallWasmOptPackageViaNpmFail(installWasmResult);
            }
        }
        
        /// Success: Show installed txt, keep button disabled, but don't actually check
        /// the optimization box since *this* publishAsync is not optimized: Next one will be
        private void onInstallWasmOptPackageViaNpmSuccess() =>
            installWasmOptBtn.text = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Success, "Installed");

        private void onInstallWasmOptPackageViaNpmFail(SpacetimeCliResult cliResult)
        {
            installWasmOptBtn.SetEnabled(true);
            installWasmOptBtn.text = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Error, 
                $"<b>Failed:</b> Couldn't install wasm-opt\n{cliResult.CliError}");
        }

        /// UI: Disable btn + show installing status to id label
        private void setAddIdentityUi(string nickname)
        {
            identityAddBtn.SetEnabled(false);
            identityStatusLabel.text = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Action, $"Adding {nickname} ...");
            showUi(identityStatusLabel);
            fadeOutUi(publishStatusLabel);
            hideUi(publishResultFoldout);
        }
        
        private async Task addIdentityAsync(string nickname, string email)
        {
            // Sanity check
            if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(email))
            {
                return;
            }

            setAddIdentityUi(nickname);
            AddIdentityRequest addIdentityRequestRequest = new(nickname, email);
            
            // Run CLI cmd
            AddIdentityResult addIdentityResult = await SpacetimeDbPublisherCliActions.AddIdentityAsync(addIdentityRequestRequest);
            SpacetimeIdentity identity = new(nickname, isDefault:true);

            // Process result -> Update UI
            if (addIdentityResult.HasCliErr)
            {
                onAddIdentityFail(identity, addIdentityResult);
            }
            else
            {
                onAddIdentitySuccess(identity);
            }
        }
        
        /// Success: Add to dropdown + set default + show. Hide the [+] add group.
        /// Don't worry about caching choices; we'll get the new choices via CLI each load
        private async void onAddIdentitySuccess(SpacetimeIdentity identity)
        {
            Debug.Log($"Add new identity success: {identity.Nickname}");
            await onGetSetIdentitiesSuccessEnsureDefault(new List<SpacetimeIdentity> { identity });
        }
        
        private void onAddIdentityFail(SpacetimeIdentity identity, AddIdentityResult addIdentityResult)
        {
            identityAddBtn.SetEnabled(true);
            identityStatusLabel.text = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Error, 
                $"<b>Failed:</b> Couldn't add identity `{identity.Nickname}`\n" +
                addIdentityResult.StyledFriendlyErrorMessage);

            if (addIdentityResult.AddIdentityError == AddIdentityResult.AddIdentityErrorType.IdentityAlreadyExists)
            {
                identityNicknameTxt.Focus();
                identityNicknameTxt.SelectAll();
            }
            
            showUi(identityStatusLabel);
        }

        private void setAddServerUi(string nickname)
        {
            // UI: Disable btn + show installing status to id label
            serverAddBtn.SetEnabled(false);
            serverStatusLabel.text = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Action, $"Adding {nickname} ...");
            showUi(serverStatusLabel);
            
            // Hide the other sections (while clearing out their labels), since we rely on servers
            hideUi(identityStatusLabel);
            hideUi(identityFoldout);
            hideUi(publishFoldout);
            fadeOutUi(publishStatusLabel);
            hideUi(publishResultFoldout);
        }
        
        private async Task addServerAsync(string nickname, string host)
        {
            // Sanity check
            if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(host))
            {
                return;
            }

            setAddServerUi(nickname);
            AddServerRequest request = new(nickname, host);

            // Run the CLI cmd
            AddServerResult addServerResult = await SpacetimeDbPublisherCliActions.AddServerAsync(request);
            
            // Process result -> Update UI
            SpacetimeServer serverAdded = new(nickname, host, isDefault:true);

            if (addServerResult.HasCliErr)
            {
                onAddServerFail(serverAdded, addServerResult);
            }
            else
            {
                onAddServerSuccess(serverAdded);
            }
        }
        
        private void onAddServerFail(SpacetimeServer serverAdded, AddServerResult addServerResult)
        {
            serverAddBtn.SetEnabled(true);
            serverStatusLabel.text = SpacetimeMeta.GetStyledStr(SpacetimeMeta.StringStyle.Error, 
                $"<b>Failed:</b> Couldn't add `{serverAdded.Nickname}` server</b>\n" +
                addServerResult.StyledFriendlyErrorMessage);
                
            showUi(serverStatusLabel);
        }
        
        /// Success: Add to dropdown + set default + show. Hide the [+] add group.
        /// Don't worry about caching choices; we'll get the new choices via CLI each load
        private void onAddServerSuccess(SpacetimeServer server)
        {
            Debug.Log($"Add new server success: {server.Nickname}");
            _ = onGetSetServersSuccessEnsureDefaultAsync(new List<SpacetimeServer> { server });
        }

        private async Task setDefaultIdentityAsync(string idNicknameOrDbAddress)
        {
            // Sanity check
            if (string.IsNullOrEmpty(idNicknameOrDbAddress))
            {
                return;
            }

            // Run CLI cmd
            SpacetimeCliResult cliResult = await SpacetimeDbPublisherCliActions.SetDefaultIdentityAsync(idNicknameOrDbAddress);

            // Process result -> Update UI
            bool isSuccess = !cliResult.HasCliErr;
            if (!isSuccess)
            {
                Debug.LogError($"Failed to {nameof(setDefaultIdentityAsync)}: {cliResult.CliError}");
                return;
            }
            
            Debug.Log($"Changed default identity to: {idNicknameOrDbAddress}");
            identityAddNewShowUiBtn.text = "+";
        }

        private void resetPublishResultCache()
        {
            publishResultFoldout.value = false;
            publishResultDateTimeTxt.value = "";
            publishResultHostTxt.value = "";
            publishResultDbAddressTxt.value = "";
            
            publishResultIsOptimizedBuildToggle.value = false;
            hideUi(installWasmOptBtn);
            hideUi(installWasmOptProgressBar);
            
            hideUi(publishResultStatusLabel);
            
            publishResultGenerateClientFilesBtn.SetEnabled(true);
            publishResultGenerateClientFilesBtn.text = "Generate Client Typings";
            
            // Hacky readonly Toggle feat workaround
            publishResultIsOptimizedBuildToggle.SetEnabled(false);
            publishResultIsOptimizedBuildToggle.style.opacity = 1;
        }

        private void resetGetServerLogsUi()
        {
            publishResultGetServerLogsBtn.SetEnabled(true);
            publishResultGetServerLogsBtn.text = "Server Logs";
        }
        
        /// Toggles the group visibility of the foldouts. Labels also hide if !show.
        /// Toggles ripple downwards from top. Checks for nulls
        private void toggleFoldoutRipple(FoldoutGroupType startRippleFrom, bool show)
        {
            // ---------------
            // Server, Identity, Publish, PublishResult
            if (startRippleFrom <= FoldoutGroupType.Server)
            {
                if (show)
                {
                    showUi(serverFoldout);
                }
                else
                {
                    hideUi(serverStatusLabel);
                    hideUi(serverFoldout);
                }
            }
            
            // ---------------
            // Identity, Publish, PublishResult
            if (startRippleFrom <= FoldoutGroupType.Identity)
            {
                if (show)
                {
                   showUi(identityFoldout); 
                }
                else
                {
                    hideUi(identityFoldout); 
                    hideUi(identityStatusLabel);
                }
            }
            else
            {
                return;
            }

            // ---------------
            // Publish, PublishResult
            if (startRippleFrom <= FoldoutGroupType.Publish)
            {
                hideUi(publishFoldout);
                if (!show)
                {
                    fadeOutUi(publishStatusLabel);
                }
            }
            else
            {
                return;
            }

            // ---------------
            // PublishResult+
            if (startRippleFrom <= FoldoutGroupType.PublishResult)
            {
                hideUi(publishResultFoldout);
            }
        }
        
        /// UI: This invalidates identities, so we'll hide all Foldouts
        /// If local, we'll need extra time to ping (show status)
        private void setDefaultServerRefreshIdentitiesUi()
        {
            toggleFoldoutRipple(FoldoutGroupType.Identity, show: false);
            toggleSelectedServerProcessingEnabled(setEnabled: false);
            
            serverConnectingStatusLabel.text = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Action, "Connecting ...");
            showUi(serverConnectingStatusLabel);
        }

        /// Change to a *known* nicknameOrHost
        /// - Changes CLI default server
        /// - Revalidates identities, since they are bound per-server
        private async Task setDefaultServerRefreshIdentitiesAsync(string nicknameOrHost)
        {
            // Sanity check
            if (string.IsNullOrEmpty(nicknameOrHost))
            {
                return;
            }
            
            setDefaultServerRefreshIdentitiesUi(); // Hide all foldouts [..]

            // Run CLI cmd
            SpacetimeCliResult cliResult = await SpacetimeDbPublisherCliActions.SetDefaultServerAsync(nicknameOrHost);
            
            // Process result -> Update UI
            bool isSuccess = !cliResult.HasCliErr;
            if (!isSuccess)
            {
                onChangeDefaultServerFail(cliResult);
            }
            else
            {
                await onChangeDefaultServerSuccessAsync();
            }
            
            toggleSelectedServerProcessingEnabled(setEnabled: true);
        }

        /// Enables or disables the selected server dropdown + add new btn
        private void toggleSelectedServerProcessingEnabled(bool setEnabled)
        {
            serverSelectedDropdown.SetEnabled(setEnabled);
            serverAddNewShowUiBtn.SetEnabled(setEnabled);
        }
        
        private void onChangeDefaultServerFail(SpacetimeCliResult cliResult)
        {
            serverSelectedDropdown.SetEnabled(true);

            string clippedCliErr = Utils.ClipString(cliResult.CliError, maxLength: 4000);
            serverConnectingStatusLabel.text = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Error,
                $"<b>Failed to Change Servers:</b>\n{clippedCliErr}");
            showUi(serverConnectingStatusLabel);
        }
        
        /// Invalidate identities
        private async Task onChangeDefaultServerSuccessAsync()
        {
            await pingLocalServerSetBtnsAsync();
            
            // UI: Hide label fast so it doesn't look laggy
            hideUi(serverConnectingStatusLabel);
            
            await getIdentitiesSetDropdown(); // Process and reveal the next UI group
            
            serverSelectedDropdown.SetEnabled(true);
            serverAddNewShowUiBtn.text = "+";
            hideUi(serverConnectingStatusLabel);
            resetPublishResultCache(); // We don't want stale info from a different server's publish showing
        }

        /// Disable generate btn, show "GGenerating..." label
        private void setGenerateClientFilesUi()
        {
            hideUi(publishResultStatusLabel);
            publishResultGenerateClientFilesBtn.SetEnabled(false);
            publishResultGenerateClientFilesBtn.text = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Action,
                "Generating ...");
        }

        private async Task generateClientFilesAsync()
        {
            setGenerateClientFilesUi();
            
            // Prioritize result cache, if any - else use the input field
            string serverModulePath = _cachedPublishResult?.Request?.ServerModulePath 
                ?? publishModulePathTxt.value;
            
            Assert.IsTrue(!string.IsNullOrEmpty(serverModulePath),
                $"Expected {nameof(serverModulePath)}");

            if (generatedFilesExist())
            {
                // Wipe old files
                Directory.Delete(PathToAutogenDir, recursive:true);
            }
            
            GenerateRequest request = new(
                serverModulePath,
                PathToAutogenDir,
                deleteOutdatedFiles: true);

            GenerateResult generateResult = await SpacetimeDbPublisherCliActions
                .GenerateClientFilesAsync(request);

            bool isSuccess = generateResult.IsSuccessfulGenerate;
            if (isSuccess)
            {
                onGenerateClientFilesSuccess(serverModulePath);
            }
            else
            {
                onGenerateClientFilesFail(generateResult);
            }
        }

        /// Disable get logs btn, show action text
        private void setGetServerLogsAsyncUi()
        {
            publishResultGetServerLogsBtn.SetEnabled(false);
            publishResultGetServerLogsBtn.text = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Action, "Fetching ...");
        }
        
        /// Gets server logs of selected server name
        private async Task getServerLogsAsync()
        {
            setGetServerLogsAsyncUi();

            string serverName = publishModuleNameTxt.text;
            SpacetimeCliResult cliResult = await SpacetimeDbCliActions.GetLogsAsync(serverName);
        
            resetGetServerLogsUi();
            if (cliResult.HasCliErr)
            {
                Debug.LogError($"Failed to {nameof(getServerLogsAsync)}: {cliResult.CliError}");
                return;
            }

            onGetServerLogsSuccess(cliResult);
        }

        /// Output logs to console, with some basic style
        private void onGetServerLogsSuccess(SpacetimeCliResult cliResult)
        {
            string infoColor = SpacetimeMeta.INPUT_TEXT_COLOR;
            string warnColor = SpacetimeMeta.ACTION_COLOR_HEX;
            string errColor = SpacetimeMeta.ERROR_COLOR_HEX;
            
            // Just color the log types for easier reading
            string styledLogs = cliResult.CliOutput
                .Replace("INFO:", $"<color={infoColor}><b>INFO:</b></color>")
                .Replace("WARNING:", SpacetimeMeta.GetStyledStr(SpacetimeMeta.StringStyle.Action, "<b>WARNING:</b>"))
                .Replace("ERROR:", SpacetimeMeta.GetStyledStr(SpacetimeMeta.StringStyle.Action, "<b>ERROR:</b>"));

            Debug.Log($"<color={SpacetimeMeta.ACTION_COLOR_HEX}><b>Formatted Server Logs:</b></color>\n" +
                $"```bash\n{styledLogs}\n```");
        }

        private void onGenerateClientFilesFail(SpacetimeCliResult cliResult)
        {
            Debug.LogError($"Failed to generate client files: {cliResult.CliError}");

            resetGenerateUi();
            
            string clippedCliErr = Utils.ClipString(cliResult.CliError, maxLength: 4000);
            publishResultStatusLabel.text = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Error,
                $"<b>Failed to Generate:</b>\n{clippedCliErr}");
            
            showUi(publishResultStatusLabel);
        }

        private void onGenerateClientFilesSuccess(string serverModulePath)
        {
            Debug.Log($"Generated SpacetimeDB client files from:" +
                $"\n`{serverModulePath}`\n\nto:\n`{PathToAutogenDir}`");
         
            resetGenerateUi();
            publishResultStatusLabel.text = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Success,
                "Generated to dir: <color=white>Assets/Autogen/</color>");
            showUi(publishResultStatusLabel);
        }
        
        bool generatedFilesExist() => Directory.Exists(PathToAutogenDir);

        /// Shared Ui changes after success/fail, or init on ui reset
        private void resetGenerateUi()
        {
            publishResultGenerateClientFilesBtn.text = generatedFilesExist()
                ? "Regenerate Client Typings"
                : "Generate Client Typings";
            
            hideUi(publishResultStatusLabel);
            publishResultGenerateClientFilesBtn.SetEnabled(true);
        }

        /// Assuming !https
        private bool checkIsLocalhostServerSelected() =>
            serverSelectedDropdown.value.StartsWith(SpacetimeMeta.LOCAL_SERVER_NAME);

        private void setStartingLocalServerUi()
        {
            fadeOutUi(publishStatusLabel);
            publishStartLocalServerBtn.SetEnabled(false);
            publishStartLocalServerBtn.text = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Action, "Starting ...");
            fadeOutUi(publishStatusLabel);
        }
        
        /// <summary>Starts the local SpacetimeDB server; sets _localServer state.</summary>
        /// <returns>startedServer</returns>
        private async Task<bool> startLocalServer()
        {
            setStartingLocalServerUi();
            
            // Run async CLI cmd => wait for connection => Save to state cache
            PingServerResult pingResult = await SpacetimeDbCliActions.StartDetachedLocalServerWaitUntilOnlineAsync();
            if (pingResult.IsServerOnline)
            {
                _lastServerPingSuccess = pingResult;
            }
            
            // Process result -> Update UI
            if (!_lastServerPingSuccess.IsServerOnline)
            {
                onStartLocalServerFail();
                return false; // !startedServer 
            }
            
            onStartLocalServerSuccess();
            return true; // startedServer
        }

        private void onStartLocalServerSuccess()
        {
            Debug.Log($"Started local server on port `{_lastServerPingSuccess}`");
            
            hideUi(publishStartLocalServerBtn);
            
            // The server is now running: Show the button to stop it (with a slight delay to enable)
            setStopLocalServerBtnTxt();
            showUi(publishStopLocalServerBtn);
            publishStopLocalServerBtn.SetEnabled(false);
            _ = WaitEnableElementAsync(publishStopLocalServerBtn, TimeSpan.FromSeconds(1));
            
            setPublishReadyStatusIfOnline();
        }

        /// Sets stop server btn to "Stop {server}@{hostUrlWithoutHttp}"
        /// Pulls host from _lastServerPinged
        private void setStopLocalServerBtnTxt()
        {
            if (string.IsNullOrEmpty(_lastServerPingSuccess?.HostUrl))
            {
                // Fallback
                publishStopLocalServerBtn.text = "Stop Local Server";
                return;
            }
            
            string host = _lastServerPingSuccess.HostUrl.Replace("127.0.0.1", "localhost");
            publishStopLocalServerBtn.text = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Error, $"Stop {serverSelectedDropdown.value}@{host}");
        }

        /// The last ping was cached to _lastServerPinged
        private void onStartLocalServerFail()
        {
            Debug.LogError($"Failed to {nameof(startLocalServer)}");

            publishStartLocalServerBtn.text = "Start Local Server";
            publishStartLocalServerBtn.SetEnabled(true);
        }

        /// <returns>stoppedServer</returns>
        private async Task<bool> stopLocalServer()
        {
            if (_lastServerPingSuccess.Port == 0)
            {
                // TODO: Set port from ping
                _lastServerPingSuccess = await SpacetimeDbCliActions.PingServerAsync();
            }
            
            // Validate + Logs + UI
            Debug.Log($"Attempting to force stop local server running on port:{_lastKnownPort}");
            setStoppingLocalServerUi();
            
            // Run CLI cmd => Save to state cache
            SpacetimeCliResult cliResult = await SpacetimeDbCliActions.ForceStopLocalServerAsync(_lastKnownPort);
            
            // Process result -> Update UI
            bool isSuccess = !cliResult.HasCliErr;
            if (!isSuccess)
            {
                Debug.LogError($"Failed to {nameof(stopLocalServer)}: {cliResult.CliError}");
                throw new Exception("TODO: Handle a rare CLI error on stop server fail");
                return false; // !stoppedServer 
            }

            onStopLocalServerSuccess();
            return true; // stoppedServer
        }

        private void setStoppingLocalServerUi()
        {
            publishStopLocalServerBtn.SetEnabled(false);
            publishStopLocalServerBtn.text = SpacetimeMeta.GetStyledStr(
                SpacetimeMeta.StringStyle.Action, "Stopping ...");
            fadeOutUi(publishStatusLabel);
        }

        /// We stopped -> So now we want to show start (+disable publish)
        private void onStopLocalServerSuccess()
        {
            Debug.Log(SpacetimeMeta.GetStyledStr(SpacetimeMeta.StringStyle.Error, "Stopped local server"));
            
            hideUi(publishStopLocalServerBtn);
            
            publishStartLocalServerBtn.text = "Start Local Server";
            publishStartLocalServerBtn.SetEnabled(true);
            showUi(publishStartLocalServerBtn);
            
            setLocalServerOfflinePublishLabel();
            publishBtn.SetEnabled(false);
        }
    }
}