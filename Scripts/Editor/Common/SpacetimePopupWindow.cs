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
            public string body = "Some body text";
            public PrefixBodyIcon prefixBodyIcon = PrefixBodyIcon.None;
            public Dictionary<string, Action> ButtonNameActionDict = new();
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
            if (opts.isModal)
            {
                window.ShowModalUtility();
            }
            else
            {
                window.ShowUtility();
            }
        }
        
        private void OnGUI()
        {
            // Align center style
            GUIStyle centeredStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
            };

            // Create GUIContent for both text and optional icon
            Texture prefixStatusIcon = getIconTexture();
            GUIContent content = new GUIContent(_guiOpts.body, prefixStatusIcon);

            // Display the label with the icon
            EditorGUILayout.LabelField(content, centeredStyle);
            
            // Create buttons; on click, call Action
            foreach (KeyValuePair<string, Action> kvp in _guiOpts.ButtonNameActionDict)
            {
                if (GUILayout.Button(kvp.Key))
                {
                    kvp.Value.Invoke(); // Callback
                }
            }
        }

        private Texture getIconTexture()
        {
            if (_guiOpts.prefixBodyIcon == PrefixBodyIcon.SuccessCircle)
            {
                return EditorGUIUtility.IconContent("d_winbtn_mac_max").image;
            }
            
            if (_guiOpts.prefixBodyIcon == PrefixBodyIcon.ErrorCircle)
            {
                return EditorGUIUtility.IconContent("d_winbtn_mac_close").image;
            }
            
            return null;
        }
    }
}