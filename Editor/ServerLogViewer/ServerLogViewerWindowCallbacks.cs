using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static SpacetimeDB.Editor.ServerLogViewerMeta;
using static SpacetimeDB.Editor.SpacetimeWindow;

namespace SpacetimeDB.Editor
{
    /// Handles direct UI callbacks, sending async Tasks to ServerLogViewerWindowWindowActions.
    /// Subscribed to @ ServerLogViewerWindow.setOnActionEvents.
    /// Set @ setOnActionEvents(), unset at unsetActionEvents().
    /// This is essentially the middleware between UI and logic.
    public partial class ServerLogViewerWindow
    {
        #region Init from ServerLogViewerWindow.cs CreateGUI()
        /// Curry sync Actions from UI => to async Tasks
        private void setOnActionEvents()
        {
            if (getServerLogsBtn != null)
            {
                getServerLogsBtn.clicked += onGetServerLogsBtnClick;
            }
        }

        /// Cleanup: This should parity the opposite of setOnActionEvents()
        private void unsetOnActionEvents()
        {
            if (getServerLogsBtn != null)
            {
                getServerLogsBtn.clicked -= onGetServerLogsBtnClick;
            }
        }
        
        /// Cleanup when the UI is out-of-scope
        private void OnDisable() => unsetOnActionEvents();
        #endregion // Init from ServerLogViewerWindow.cs CreateGUI()

        
        #region Direct UI Callbacks
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
        #endregion // Direct UI Callbacks
    }
}
