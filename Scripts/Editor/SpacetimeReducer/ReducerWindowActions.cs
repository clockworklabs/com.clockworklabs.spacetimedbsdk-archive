using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using static SpacetimeDB.Editor.ReducerMeta;

namespace SpacetimeDB.Editor
{
    /// Unlike ReducerWindowCallbacks, these are not called *directly* from UI.
    /// Runs an action -> Processes isSuccess -> calls success || fail @ ReducerWindowCallbacks.
    /// ReducerWindowCallbacks should handle try/catch (except for init chains).
    public partial class ReducerWindow
    {
        #region Init from ReducerWindow.CreateGUI
        /// Gets selected server + identity. On err, refers to PublisherWindow
        /// Initially called by ReducerWindow @ CreateGUI.
        private async Task initDynamicEventsFromReducerWindow()
        {
            // Ensure CLI installed -> Show err (refer to PublisherWindow), if not
            SpacetimeCliResult isSpacetimeDbCliInstalledResult = await SpacetimeDbCli.GetIsSpacetimeCliInstalledAsync();
            bool isCliInstalled = !isSpacetimeDbCliInstalledResult.HasCliErr;
            if (!isCliInstalled)
            {
                showErrorWrapper("<b>SpacetimeDB CLI is not installed:</b>\n" +
                    "Setup via top menu `Window/SpacetimeDB/Publisher`");
                return;
            }
            
            // Load selected server+identity
            GetIdentitiesResult getIdentitiesResult = await SpacetimeDbCli.GetIdentitiesAsync();
            if (!getIdentitiesResult.HasIdentity || getIdentitiesResult.HasIdentitiesButNoDefault)
            {
                showErrorWrapper("<b>Failed to get identities:</b>\n" +
                    "Setup via top menu `Window/SpacetimeDB/Publisher`");
                return;
            }

            SpacetimeIdentity defaultIdentity = getIdentitiesResult.Identities
                .First(id => id.IsDefault);
            identityTxt.value = defaultIdentity.Nickname;
            
            GetServersResult getServersResult = await SpacetimeDbCli.GetServersAsync();
            if (!getServersResult.HasServer || getServersResult.HasServersButNoDefault)
            {
                showErrorWrapper("<b>Failed to get servers:</b>\n" +
                    "Setup via top menu `Window/SpacetimeDB/Publisher`");
                return;
            }
            
            SpacetimeServer defaultServer = getServersResult.Servers
                .First(server => server.IsDefault);
            serverTxt.value = defaultServer.Nickname;

            // Load reducers
            // Show Actions foldout
        }
        
        /// Initially called by ReducerWindow @ CreateGUI
        /// - Set to the initial state as if no inputs were set.
        /// - This exists so we can show all ui elements simultaneously in the
        ///   ui builder for convenience.
        /// - (!) If called from CreateGUI, after a couple frames,
        ///       any persistence from `ViewDataKey`s may override this.
        private void resetUi()
        {
            serverTxt.value = "";
            identityTxt.value = "";
            reducersTreeView.SetEnabled(true);
            resetActionsFoldoutUi();
        }

        private void resetActionsFoldoutUi()
        {
            actionsFoldout.style.display = DisplayStyle.None;
            actionsSyntaxHintLabel.style.display = DisplayStyle.None;
            actionsRunBtn.SetEnabled(false);
        }
        #endregion // Init from ReducerWindow.CreateGUI


        /// Wraps the entire body in an error message, generally when there's
        /// a cli/server/identity error that should be configured @ PublisherWindow (not here).
        /// Wraps text in error style color
        private void showErrorWrapper(string friendlyError)
        {
            throw new NotImplementedException($"TODO: Hide body -> show err: {friendlyError}");
        }
    }
}