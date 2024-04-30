using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
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
            serverLogsTxt.value = string.Empty;
        }
        #endregion // Init from ServerLogViewerWindow.CreateGUI
        
        
        private async Task getServerLogsAsync()
        {
            throw new NotImplementedException();
        }
    }
}