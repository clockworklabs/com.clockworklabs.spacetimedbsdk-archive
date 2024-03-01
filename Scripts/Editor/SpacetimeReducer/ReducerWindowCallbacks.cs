using System;
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
            actionTxt.RegisterValueChangedCallback(onActionTxtValueChanged); // Toggles the Run btn
            refreshReducersBtn.clicked += onRefreshReducersBtnClickAsync; // Refresh reducers tree view live from cli
            
            reducersTreeView.bindItem = onBindReducersTreeViewItem; // No need to unsub // Populates the Adds _entityStructure nickname to element
            reducersTreeView.makeItem = onMakeReducersTreeViewItem; // No need to unsub // Creates a new VisualElement within the tree view on new item
            reducersTreeView.selectedIndicesChanged += onReducerTreeViewIndicesChanged; // Selected multiple reducers from tree // TODO: Do we need this
            reducersTreeView.selectionChanged += onReducerTreeViewSelectionChanged; // Single reducer selected from tree
        }

        /// Cleanup: This should parity the opposite of setOnActionEvents()
        private void unsetOnActionEvents()
        { 
            topBannerBtn.clicked -= onTopBannerBtnClick;
            actionsRunBtn.clicked -= onActionsRunBtnClick;
            refreshReducersBtn.clicked -= onRefreshReducersBtnClickAsync; // Refresh reducers tree view live from cli
            
            reducersTreeView.selectedIndicesChanged -= onReducerTreeViewIndicesChanged; // Selected multiple reducers from tree // TODO: Do we need this
            reducersTreeView.selectionChanged -= onReducerTreeViewSelectionChanged; // Single reducer selected from tree
        }

        /// Cleanup when the UI is out-of-scope
        private void OnDisable() => unsetOnActionEvents();
        
        /// When a new item is added to a tree view, assign the VisualElement type
        private VisualElement onMakeReducersTreeViewItem() => new Label();
        
        /// Populates a tree view item with an label element; also assigns the name
        private void onBindReducersTreeViewItem(VisualElement element, int index)
        {
            Label label = (Label)element;
            label.text = _entityStructure.ReducersInfo[index].GetReducerName();
        }
        #endregion // Init from ReducerWindow.cs CreateGUI()
        
        
        #region Direct UI Callbacks
        /// When the action text val changes, toggle the Run button
        /// Considers Entity Arity
        private void onActionTxtValueChanged(ChangeEvent<string> evt)
        {
            bool hasVal = !string.IsNullOrEmpty(evt.newValue);
            if (hasVal)
            {
                actionsRunBtn.SetEnabled(hasVal);
                return;
            }
            
            // Has no val. First, is anything selected?
            int selectedIndex = reducersTreeView.selectedIndex;
            if (selectedIndex == -1)
            {
                actionsRunBtn.SetEnabled(false); // Nothing selected
                return;
            }
            
            // We can enable if # of aria is 0
            int numAria = _entityStructure.ReducersInfo[reducersTreeView.selectedIndex].ReducerEntity.Arity;
            actionsRunBtn.SetEnabled(numAria == 0);
        }
        
        /// Open link to SpacetimeDB Module docs
        private void onTopBannerBtnClick() =>
            Application.OpenURL(TOP_BANNER_CLICK_LINK);

        private async void onRefreshReducersBtnClickAsync()
        {
            // Sanity check
            if (string.IsNullOrEmpty(moduleNameTxt.value))
            {
                return;
            }
            
            await setReducersTreeViewAsync();
        }

        private void onActionsRunBtnClick() => 
            throw new NotImplementedException("TODO: onActionsRunBtnClick");
        #endregion // Direct UI Callbacks
    }
}
