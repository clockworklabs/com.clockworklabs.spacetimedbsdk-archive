using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static SpacetimeDB.Editor.ServerLogViewerMeta;
using static SpacetimeDB.Editor.SpacetimeWindow;

namespace SpacetimeDB.Editor
{
    /// Unlike ServerLogViewerWindowCallbacks, these are not called *directly* from UI.
    /// Runs an action -> Processes isSuccess -> calls success || fail @ ServerLogViewerWindowCallbacks.
    /// ServerLogViewerWindowCallbacks should handle try/catch (except for init chains).
    public partial class ServerLogViewerWindow
    {
        #region Init from ServerLogViewerWindow.CreateGUI
        /// Ensures SpacetimeDB CLI is installed
        private async Task initDynamicEventsFromServerLogViewerWindow()
        {
            await SpacetimeWindow.EnsureHasSpacetimeDbCli();
            await setSelectedServerDropdownAsync();
            await setSelectedIdentityDropdownAsync();
            
            //// TODO: If `spacetime list` ever returns db names (not just addresses),
            //// TODO: Auto list them in dropdown
            setModuleNameTxt();
        }

        /// Pulls from publisher, if any
        private void setModuleNameTxt()
        {
            // Other editor tools may want to utilize this value,
            // since the CLI has no idea what you're "default" Module is
            moduleNameTxt.value = EditorPrefs.GetString(
                SpacetimeMeta.EDITOR_PREFS_MODULE_NAME_KEY, 
                defaultValue: "");
        }

        /// Initially called by ServerLogViewerWindow @ CreateGUI
        /// - Set to the initial state as if no inputs were set.
        /// - This exists so we can show all ui elements simultaneously in the
        ///   ui builder for convenience.
        /// - (!) If called from CreateGUI, after a couple frames,
        ///       any persistence from `ViewDataKey`s may override this.
        private void resetUi()
        {
            getServerLogsBtn.SetEnabled(true);
            getServerLogsBtn.text = "Get Server Logs";
            selectedServerDropdown.choices.Clear();
            serverLogsLabel.text = string.Empty;
        }
        
        private async Task setSelectedServerDropdownAsync()
        {
            GetServersResult getServersResult = await SpacetimeDbCliActions.GetServersAsync();

            bool isSuccess = getServersResult.HasServer && !getServersResult.HasServersButNoDefault;
            if (!isSuccess)
            {
                serverLogsLabel.text = "<b>Failed to get servers:</b>\n" +
                                       "Setup via top menu `Window/SpacetimeDB/Publisher`";
                return;
            }

            onSetSelectedServerDropdownSuccess(getServersResult);
        }
        
        
        /// Set the selected default identity from CLI
        private async Task setSelectedIdentityDropdownAsync()
        {
            GetIdentitiesResult getIdentitiesResult = await SpacetimeDbCliActions.GetIdentitiesAsync();

            bool isSuccess = getIdentitiesResult.HasIdentity && !getIdentitiesResult.HasIdentitiesButNoDefault;
            if (!isSuccess)
            {
                serverLogsLabel.text = "<b>Failed to get identities:</b>\n" +
                                       "Setup via top menu `Window/SpacetimeDB/Publisher`";
                return;
            }

            onSetSelectedIdentityDropdownSuccess(getIdentitiesResult);
        }

        private void onSetSelectedIdentityDropdownSuccess(GetIdentitiesResult getIdentitiesResult)
        {
            // Populate dropdown; set default
            foreach (SpacetimeIdentity identity in getIdentitiesResult.Identities)
            {
                if (!identity.IsDefault)
                {
                    selectedIdentityDropdown.choices.Add(identity.Nickname);
                    continue;
                }

                // Default: Add to top, then set val
                selectedIdentityDropdown.choices.Insert(0, identity.Nickname);
                selectedIdentityDropdown.value = identity.Nickname;
            }
        }

        private void onSetSelectedServerDropdownSuccess(GetServersResult getServersResult)
        {
            // Populate dropdown; set default
            foreach (SpacetimeServer server in getServersResult.Servers)
            {
                if (!server.IsDefault)
                {
                    selectedServerDropdown.choices.Add(server.Nickname);
                    continue;
                }

                // Default: Add to top, then set val
                selectedServerDropdown.choices.Insert(0, server.Nickname);
                selectedServerDropdown.value = server.Nickname;
            }
        }
        #endregion // Init from ServerLogViewerWindow.CreateGUI
        
        
        /// Gets server logs => Log raw => Show slightly styled below
        private async Task getServerLogsAsync()
        {
            string serverName = selectedServerDropdown.value;
            string identityName = selectedIdentityDropdown.value;
            string moduleName = moduleNameTxt.value;
            
            SpacetimeCliResult cliResult = await SpacetimeDbCliActions.GetLogsAsync(
                moduleName,
                serverName,
                identityName,
                maxNumLines: -1); // TODO: Want to expose this to UI?
            
            bool isSuccess = !cliResult.HasCliErr;
            if (!isSuccess)
            {
                onGetServerLogsFail(cliResult);
                return;
            }

            onGetServerLogsSuccess(cliResult);
        }

        private void onGetServerLogsFail(SpacetimeCliResult cliResult)
        {
            Debug.LogError($"{nameof(getServerLogsAsync)} {cliResult.CliError}");
            serverLogsLabel.text = $"<b>Failed to get server logs:</b>\n{cliResult.CliError}";
        }

        /// SpacetimeDbCli will have already logged the raw, copyable output
        /// TODO: Currently, readonly TextField (copyable text) !supports rich text.
        /// ^ Since we already dump the copyable logs, lets show pretty ones here.
        /// ^ When rich text feat arrives to TextField, swap the Label to readonly TextField
        private void onGetServerLogsSuccess(SpacetimeCliResult cliResult)
        {
            serverLogsLabel.text = SpacetimeDbCliActions.PrettifyServerLogs(cliResult.CliOutput);
        }
    }
}