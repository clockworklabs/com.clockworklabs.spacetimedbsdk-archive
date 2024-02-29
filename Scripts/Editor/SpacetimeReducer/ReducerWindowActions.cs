using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

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
            await ensureCliInstalledAsync();
            await setSelectedServerTxtAsync();
            await setSelectedIdentityTxtAsync();
            
            //// TODO: If `spacetime list` ever returns db names (not just addresses),
            //// TODO: Auto list them in dropdown
            // await setSelectedModuleTxtAsync();
            
            await setReducersTreeViewAsync();

            // Load entities into TreeView
            throw new NotImplementedException("TODO: Load entities into TreeView");

            // Show Actions foldout
            throw new NotImplementedException("TODO: Show Actions foldout");
        }

        /// Loads reducer names into #reducersTreeView -> Enable
        /// Doc | https://docs.unity3d.com/2022.3/Documentation/Manual/UIE-uxml-element-TreeView.html
        private async Task setReducersTreeViewAsync()
        {
            string moduleName = moduleTxt.value;
            GetEntityStructureResult entityStructureResult = await SpacetimeDbCli.GetEntityStructure(moduleName);
            
            bool isSuccess = entityStructureResult is { HasEntityStructure: true };
            if (!isSuccess)
            {
                Debug.Log("Warning: Searched for reducers; found none");
                return;
            }
            
            // Success: Load entity names into reducer tree view - cache _entityStructure state
            // TODO: +with friendly styled syntax hint children
            _entityStructure = entityStructureResult.EntityStructure;
            reducersTreeView.makeItem = () => new Label(); // Creates a new Label for each item
            reducersTreeView.bindItem = bindReducersTreeViewItem;

            // Enable the TreeView
            reducersTreeView.SetEnabled(true);
        }

        /// Must use the Index (not the ID) with GetItemDataForIndex (of T)
        private void bindReducersTreeViewItem(VisualElement element, int index)
        {
            Label label = element as Label;
            bool isValid = 
                label is not null && 
                index >= 0 && 
                index < _entityStructure.ReducersInfo.Count;
            
            if (!isValid)
                return;
            
            ReducerInfo reducerInfo = _entityStructure.ReducersInfo[index];
            if (reducerInfo is null)
                return;

            // Set TreeViewItem element
            label.text = reducerInfo.GetReducerName();
        }

        private async Task setSelectedServerTxtAsync()
        {
            GetServersResult getServersResult = await SpacetimeDbCli.GetServersAsync();
            
            bool isSuccess = getServersResult.HasServer && !getServersResult.HasServersButNoDefault;
            if (!isSuccess)
            {
                showErrorWrapper("<b>Failed to get servers:</b>\n" +
                    "Setup via top menu `Window/SpacetimeDB/Publisher`");
                return;
            }
            
            // Success
            SpacetimeServer defaultServer = getServersResult.Servers
                .First(server => server.IsDefault);
            serverTxt.value = defaultServer.Nickname;
        }

        /// Load selected identities => set readonly identity txt
        private async Task setSelectedIdentityTxtAsync()
        {
            GetIdentitiesResult getIdentitiesResult = await SpacetimeDbCli.GetIdentitiesAsync();

            bool isSuccess = getIdentitiesResult.HasIdentity && !getIdentitiesResult.HasIdentitiesButNoDefault;
            if (!isSuccess)
            {
                showErrorWrapper("<b>Failed to get identities:</b>\n" +
                    "Setup via top menu `Window/SpacetimeDB/Publisher`");
                return;
            }

            // Success
            SpacetimeIdentity defaultIdentity = getIdentitiesResult.Identities
                .First(id => id.IsDefault);
            identityTxt.value = defaultIdentity.Nickname;
        }

        private async Task ensureCliInstalledAsync()
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
            
            // Success: Do nothing!
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
        /// Wraps text in error style color.
        /// Throws.
        private void showErrorWrapper(string friendlyError)
        {
            throw new NotImplementedException($"TODO: Hide body -> show err: {friendlyError}");
        }
    }
}