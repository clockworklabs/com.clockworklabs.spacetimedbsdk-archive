using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using static SpacetimeDB.Editor.ReducerMeta;

namespace SpacetimeDB.Editor
{
    /// Binds style and click events to the SpacetimeDB Reducer Window
    /// Note the dynamic init sequence logic @ initDynamicEventsFromReducerWindow()
    /// (!) This window assumes that SpacetimeDB tool is already installed.
    /// (!) This window has view-only access to servers/identities.
    /// (!) Errors involving cli installation, identity and servers will refer to Publisher window.
    public partial class ReducerWindow : EditorWindow
    {
        #region Window State
        private EntityStructure _entityStructure; // For reducersTreeView, set @ setReducersTreeViewAsync()
        #endregion // Window State
        
        
        #region UI Visual Elements
        // ##################################################################
        // Use `camelCase` naming conventions to utilize nameof and match UI.
        // This is due to implicit bindings, making it less error-prone.
        // ##################################################################
        private Button topBannerBtn;

        private TextField serverTxt;
        private TextField identityTxt;
        private TextField moduleTxt;

        private TreeView reducersTreeView;
        private Label reducersLoadingLabel;
        
        private Foldout actionsFoldout;
        private TextField actionTxt;
        private Label actionsSyntaxHintLabel;
        private Button actionsRunBtn;
        #endregion // UI Visual Elements
        
        
        #region Init
        /// Show the Reducer window via top Menu item
        [MenuItem("Window/SpacetimeDB/Reducer #&d")] // (SHIFT+ALT+D) - not "R" because that's Nvidia's overlay default
        public static void ShowReducerWindow()
        {
            ReducerWindow window = GetWindow<ReducerWindow>();
            window.titleContent = new GUIContent("Reducer");
        }

        /// Add style to the UI window; subscribe to click actions.
        /// High-level event chain handler.
        /// (!) Persistent vals will NOT load immediately here; await them at setOnActionEvents
        public async void CreateGUI()
        {
            // Init styles, bind fields to ui, sub to events
            initVisualTreeStyles();
            setUiElements();
            sanityCheckUiElements();

            // Fields set from here
            resetUi(); // @ ReducerWindowActions.cs
            setOnActionEvents(); // @ ReducerWindowCallbacks.cs

            try
            {
                await initDynamicEventsFromReducerWindow(); // @ ReducerWindowActions
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
            StyleSheet reducerStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>(PathToUss);
            
            // Sanity check, before applying styles (since these are all loaded via implicit paths)
            // Ensure all elements and styles were found
            Assert.IsNotNull(visualTree, "Failed to load ReducerWindow: " +
                $"Expected {nameof(visualTree)} (UXML) to be at: {PathToUxml}");
            
            Assert.IsNotNull(commonStyles, "Failed to load ReducerWindow: " +
                $"Expected {nameof(commonStyles)} (USS) to be at: '{SpacetimeMeta.PathToCommonUss}'");
            
            Assert.IsNotNull(reducerStyles, "Failed to load ReducerWindow: " +
                $"Expected {nameof(reducerStyles)} (USS) to be at: '{PathToUss}'");
            
            // Clone the visual tree (UXML)
            visualTree.CloneTree(rootVisualElement);
            
            // apply style (USS)
            rootVisualElement.styleSheets.Add(commonStyles);
            rootVisualElement.styleSheets.Add(reducerStyles);
        }

        /// All VisualElement field names should match their #newIdentity in camelCase
        private void setUiElements()
        {
            topBannerBtn = rootVisualElement.Q<Button>(nameof(topBannerBtn));
            
            serverTxt = rootVisualElement.Q<TextField>(nameof(serverTxt));
            identityTxt = rootVisualElement.Q<TextField>(nameof(identityTxt));
            moduleTxt = rootVisualElement.Q<TextField>(nameof(moduleTxt));
            
            reducersTreeView = rootVisualElement.Q<TreeView>(nameof(reducersTreeView));
            reducersLoadingLabel = rootVisualElement.Q<Label>(nameof(reducersLoadingLabel));
            
            actionsFoldout = rootVisualElement.Q<Foldout>(nameof(actionsFoldout));
            actionTxt = rootVisualElement.Q<TextField>(nameof(actionTxt));
            actionsSyntaxHintLabel = rootVisualElement.Q<Label>(nameof(actionsSyntaxHintLabel));
            actionsRunBtn = rootVisualElement.Q<Button>(nameof(actionsRunBtn));
        }

        /// Changing implicit names can easily cause unexpected nulls
        /// All VisualElement field names should match their #newIdentity in camelCase
        private void sanityCheckUiElements()
        {
            try
            {
                Assert.IsNotNull(topBannerBtn, $"Expected `#{nameof(topBannerBtn)}`");
                
                Assert.IsNotNull(serverTxt, $"Expected `#{nameof(serverTxt)}`");
                Assert.IsNotNull(identityTxt, $"Expected `#{nameof(identityTxt)}`");
                Assert.IsNotNull(moduleTxt, $"Expected `#{nameof(moduleTxt)}`");
                
                Assert.IsNotNull(reducersTreeView, $"Expected `#{nameof(reducersTreeView)}`");
                Assert.IsNotNull(reducersLoadingLabel, $"Expected `#{nameof(reducersLoadingLabel)}`");
                
                Assert.IsNotNull(actionsFoldout, $"Expected `#{nameof(actionsFoldout)}`");
                Assert.IsNotNull(actionTxt, $"Expected `#{nameof(actionTxt)}`");
                Assert.IsNotNull(actionsSyntaxHintLabel, $"Expected `#{nameof(actionsSyntaxHintLabel)}`");
                Assert.IsNotNull(actionsRunBtn, $"Expected `#{nameof(actionsRunBtn)}`");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error: {e}");
                throw;
            }
        }
        #endregion // Init
    }
}