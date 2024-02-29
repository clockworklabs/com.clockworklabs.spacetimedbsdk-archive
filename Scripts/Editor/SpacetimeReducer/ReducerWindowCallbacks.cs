using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static SpacetimeDB.Editor.ReducerMeta;

namespace SpacetimeDB.Editor
{
    /// Handles direct UI callbacks, sending async Tasks to ReducerWindowActions.
    /// Subscribed to @ ReducerWindow.setOnActionEvents.
    /// Set @ setOnActionEvents(), unset at unsetActionEvents().
    /// This is essentially the middleware between UI and logic.
    public partial class ReducerWindow
    {
        #region Init from ReducerWindow.cs CreateGUI()
        /// Curry sync Actions from UI => to async Tasks
        private void setOnActionEvents()
        {
            topBannerBtn.clicked += onTopBannerBtnClick; // Launches Module docs website
            actionsRunBtn.clicked += onActionsRunBtnClick; // Run the reducer via CLI
            refreshReducersBtn.clicked += onRefreshReducersBtnClickAsync; // Refresh reducers tree view live from cli
            reducersTreeView.selectionChanged += onReducersTreeViewSelectionChanged; // Selected a reducer from tree
        }

        /// Cleanup: This should parity the opposite of setOnActionEvents()
        private void unsetOnActionEvents()
        {
            topBannerBtn.clicked -= onTopBannerBtnClick;
            actionsRunBtn.clicked -= onActionsRunBtnClick;
            refreshReducersBtn.clicked -= onRefreshReducersBtnClickAsync; // Refresh reducers tree view live from cli
            reducersTreeView.selectionChanged -= onReducersTreeViewSelectionChanged;
        }

        /// Cleanup when the UI is out-of-scope
        private void OnDisable() => unsetOnActionEvents();
        #endregion // Init from ReducerWindow.cs CreateGUI()
        
        
        #region Direct UI Callbacks
        /// Open link to SpacetimeDB Module docs
        private void onTopBannerBtnClick() =>
            Application.OpenURL(TOP_BANNER_CLICK_LINK);
        
        private void onReducersTreeViewSelectionChanged(IEnumerable<object> obj)
        {
            throw new NotImplementedException("TODO: onReducersTreeViewSelectionChanged");
        }

        private async void onRefreshReducersBtnClickAsync() =>
            await setReducersTreeViewAsync();
        
        private void onActionsRunBtnClick() => 
            throw new NotImplementedException("TODO: onActionsRunBtnClick");
        #endregion // Direct UI Callbacks
    }
}
