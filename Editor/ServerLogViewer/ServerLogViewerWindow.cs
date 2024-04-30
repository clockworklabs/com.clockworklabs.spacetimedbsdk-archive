using System;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using static SpacetimeDB.Editor.ServerLogViewerMeta;
using static SpacetimeDB.Editor.SpacetimeWindow;

namespace SpacetimeDB.Editor
{
    /// Binds style and click events to the Spacetime Server Log Viewer Window
    /// Note the dynamic init sequence logic @ initDynamicEventsFromServerLogViewerWindow()
    public partial class ServerLogViewerWindow : EditorWindow
    {
        #region UI Visual Elements
        private DropdownField selectedServerDropdown;
        private DropdownField selectedIdentityDropdown;
        private TextField moduleNameTxt;
            
        private Button getServerLogsBtn;
        
        /// (!) Don't use ViewDataKey since the data is too large
        /// TODO: Currently, readonly TextField (copyable text) !supports rich text.
        /// ^ Since we already dump the copyable logs, lets show pretty ones here.
        /// ^ When rich text feat arrives to TextField, swap the Label to readonly TextField
        private Label serverLogsLabel;
        
        private VisualElement errorCover;
        #endregion // UI Visual Elements
        
        
        #region Init
        /// Show the publisher window via top Menu item
        [MenuItem("Window/SpacetimeDB/Server Log Viewer #&v")] // (SHIFT+ALT+V)
        public static void ShowServerLogViewerWindow()
        {
            ServerLogViewerWindow window = GetWindow<ServerLogViewerWindow>();
            window.titleContent = new GUIContent("Server Logs");
        }

        /// Add style to the UI window; subscribe to click actions.
        /// High-level event chain handler.
        /// (!) Persistent vals loaded from a ViewDataKey prop will NOT
        ///     load immediately here; await them elsewhere.
        public async void CreateGUI()
        {
            // Init styles, bind fields to ui, validate integrity
            initVisualTreeStyles();
            setUiElements();
            sanityCheckUiElements();

            // Reset the UI (since all UI shown in UI Builder), sub to click/interaction events
            resetUi(); // (!) ViewDataKey persistence loads sometime *after* CreateGUI().
            setOnActionEvents(); // @ ServerLogViewerWindowCallbacks.cs

            try
            {
                // Async init chain: Ensure CLI is installed -> Load default servers ->
                // Load default identities -> Load cached publish result, if any
                await initDynamicEventsFromServerLogViewerWindow(); // @ ServerLogViewerWindowActions
            }
            catch (Exception e)
            {
                Debug.LogError($"Error: {e}");
                throw;
            }
        }

        private void initVisualTreeStyles()
        {
            // Load visual elements and stylesheets
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PathToUxml);
            StyleSheet commonStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>(SpacetimeMeta.PathToCommonUss);
            StyleSheet serverLogViewerStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>(PathToUss);
            
            // Sanity check, before applying styles (since these are all loaded via implicit paths)
            // Ensure all elements and styles were found
            Assert.IsNotNull(visualTree, "Failed to load ServerLogViewerWindow: " +
                $"Expected {nameof(visualTree)} (UXML) to be at: {PathToUxml}");
            
            Assert.IsNotNull(commonStyles, "Failed to load ServerLogViewerWindow: " +
                $"Expected {nameof(commonStyles)} (USS) to be at: '{SpacetimeMeta.PathToCommonUss}'");
            
            Assert.IsNotNull(serverLogViewerStyles, "Failed to load ServerLogViewerWindow: " +
                $"Expected {nameof(serverLogViewerStyles)} (USS) to be at: '{PathToUss}'");
            
            // Clone the visual tree (UXML)
            visualTree.CloneTree(rootVisualElement);
            
            // apply style (USS)
            rootVisualElement.styleSheets.Add(commonStyles);
            rootVisualElement.styleSheets.Add(serverLogViewerStyles);
        }

        /// All VisualElement field names should match their #newIdentity in camelCase
        private void setUiElements()
        {
            selectedServerDropdown = rootVisualElement.Q<DropdownField>(nameof(selectedServerDropdown));
            selectedIdentityDropdown = rootVisualElement.Q<DropdownField>(nameof(selectedIdentityDropdown));
            moduleNameTxt = rootVisualElement.Q<TextField>(nameof(moduleNameTxt));

            getServerLogsBtn = rootVisualElement.Q<Button>(nameof(getServerLogsBtn));
            serverLogsLabel = rootVisualElement.Q<Label>(nameof(serverLogsLabel));
        }

        /// Changing implicit names can easily cause unexpected nulls
        /// All VisualElement field names should match their #newIdentity in camelCase
        private void sanityCheckUiElements()
        {
            try
            {
                Assert.IsNotNull(selectedServerDropdown, $"Expected `#{nameof(selectedServerDropdown)}`");
                Assert.IsNotNull(selectedIdentityDropdown, $"Expected `#{nameof(selectedIdentityDropdown)}`");
                Assert.IsNotNull(moduleNameTxt, $"Expected `#{nameof(moduleNameTxt)}`");

                Assert.IsNotNull(getServerLogsBtn, $"Expected `#{nameof(getServerLogsBtn)}`");
                Assert.IsNotNull(serverLogsLabel, $"Expected `#{nameof(serverLogsLabel)}`");
            }
            catch (Exception e)
            {
                // Show err cover
                errorCover = rootVisualElement.Q<VisualElement>(nameof(errorCover));
                if (errorCover != null)
                {
                    ShowUi(errorCover);
                }

                Debug.LogError($"Error: {e}");
                throw;
            }
        }
        #endregion // Init
    }
}