using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SpacetimeDB.Editor
{
    /// Dynamic editor window popup helper
    public class SpacetimePopupWindow : EditorWindow
    {
        private GuiOpts _guiOpts;

        public enum PrefixBodyIcon
        {
            None,
            SuccessCircle,
            ErrorCircle,
        }
        
        public class ShowWindowOpts : GuiOpts
        {
            public string title = "SpacetimeDB Window";
            public int Width = 250;
            public int Height = 150;
            public bool isModal = true;
        }

        /// Set @ OnGUI
        public class GuiOpts
        {
            /// Shows a small icon before Body
            public PrefixBodyIcon PrefixBodyIcon = PrefixBodyIcon.None;
            
            public string Body = "Some body text";
            
            /// Great for showing code snippets; best to put a copy btn after it
            /// (Unity won't allow readonly+copyable); Unlocks ReadonlyBlockBeforeBtns:bool.
            public string ReadonlyBlockAfterBody = null;

            /// Default: Before btns, else goes after
            public bool ReadonlyBlockBeforeBtns = true;
            
            /// Shows at bottom
            public Dictionary<string, Action> ButtonNameActionDict = new();

            /// Default: Align middle left
            public bool AlignMiddleCenter = false;
        }

        public static void ShowWindow(ShowWindowOpts opts)
        {
            // Create a new window instance
            SpacetimePopupWindow window = CreateInstance<SpacetimePopupWindow>();
            window.titleContent = new GUIContent(opts.title);
            window._guiOpts = opts; // To be triggered @ OnGUI
            
            // Centered pos
            window.position = new Rect(
                x: Screen.width / 2 - opts.Width / 2,
                y: Screen.height / 2 - opts.Height / 2,
                width: opts.Width,
                height: opts.Height);
            
            // Make the window modal
            try
            {
                if (opts.isModal)
                {
                    window.ShowModalUtility();
                }
                else
                {
                    window.ShowUtility();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                throw;
            }
        }
        
        private void OnGUI()
        {
            // Align center style
            GUIStyle alignStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = _guiOpts.AlignMiddleCenter 
                    ? TextAnchor.MiddleCenter 
                    : TextAnchor.MiddleLeft
            };

            // Create GUIContent for both text and optional icon prefixed
            Texture prefixStatusIcon = getIconTexture();
            
            // Unity handles \n poorly, so we split line breaks to their own Labels
            string[] lines = _guiOpts.Body.Split('\n');
            foreach (string line in lines)
            {
                GUIContent lineContent = new GUIContent(line, prefixStatusIcon);
                EditorGUILayout.LabelField(lineContent, alignStyle);
                
                // Reset the icon after the first line so it only appears once
                prefixStatusIcon = null;
            }

            // Create readonly text block?
            if (_guiOpts.ReadonlyBlockAfterBody != null && _guiOpts.ReadonlyBlockBeforeBtns)
            {
                insertTextBlock();
            }

            // Create buttons; on click, call Action
            foreach (KeyValuePair<string, Action> kvp in _guiOpts.ButtonNameActionDict)
            {
                if (GUILayout.Button(kvp.Key))
                {
                    kvp.Value.Invoke(); // Callback
                }
            }
            
            // Create readonly text block?
            if (_guiOpts.ReadonlyBlockAfterBody != null && !_guiOpts.ReadonlyBlockBeforeBtns)
            {
                insertTextBlock();
            }
        }

        private void insertTextBlock()
        {
            // Calculate the height based on text length
            GUIStyle textStyle = GUI.skin.textArea;
            float width = position.width - 20;
            Vector2 textSize = textStyle.CalcSize(new GUIContent(_guiOpts.ReadonlyBlockAfterBody));

            // Estimate lines based on width and calculate height
            int lines = Mathf.Max(1, (int)(textSize.x / width) + 1);
            float height = lines * textStyle.lineHeight + 10; // Add some padding

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextArea(
                _guiOpts.ReadonlyBlockAfterBody, 
                GUILayout.Height(height));
            EditorGUI.EndDisabledGroup();
        }

        private Texture getIconTexture()
        {
            if (_guiOpts.PrefixBodyIcon == PrefixBodyIcon.SuccessCircle)
            {
                return EditorGUIUtility.IconContent("d_winbtn_mac_max").image;
            }
            
            if (_guiOpts.PrefixBodyIcon == PrefixBodyIcon.ErrorCircle)
            {
                return EditorGUIUtility.IconContent("d_winbtn_mac_close").image;
            }
            
            return null;
        }
        
        // [MenuItem("Window/SpacetimeDB/Test/testPopupWindow %#&T")] // CTRL+ALT+SHIFT+T // (!) Commment out when !testing
        private static void testPopupWindow()
        {
            // Create buttons
            const string openExplorerBtnStr = "Test btn 1";
            const string openProjBtnStr = "Test btn 3";
            Dictionary<string, Action> btnNameActionDict = new()
            {
                { openExplorerBtnStr, () => Debug.Log($"Clicked {openExplorerBtnStr}") },
                { openProjBtnStr, () => Debug.Log($"Clicked {openProjBtnStr}") },
            };

            ShowWindowOpts opts = new()
            {
                title = "Some title",
                Body = "Some success body",
                PrefixBodyIcon = PrefixBodyIcon.SuccessCircle,
                Width = 250,
                Height = 100,
                isModal = true,
                ButtonNameActionDict = btnNameActionDict,
                ReadonlyBlockAfterBody = "This is readonly, copyable text",
            };
            
            ShowWindow(opts);
        }
    }
}