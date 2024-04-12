using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SpacetimeDB.Editor
{
    public static class ServerModuleManager
    {
        [MenuItem("Window/SpacetimeDB/New Server Module/C# %#&p")] // CTRL+SHIFT+ALT+P
        public static async void CreateNewCsModule()
        {
            await createNewModule(SpacetimeMeta.ModuleLang.CSharp);
        }
        
        [MenuItem("Window/SpacetimeDB/New Server Module/Rust")]
        public static async void CreateNewRustModule()
        {
            await createNewModule(SpacetimeMeta.ModuleLang.Rust);
        }

        private static async Task createNewModule(SpacetimeMeta.ModuleLang lang)
        {
            // Create a directory picker, defaulting to the project's root
            string projectRoot = Application.dataPath.Replace("/Assets", "");
            
            // Dir picker
            string initProjDirPath = EditorUtility.OpenFolderPanel("New SpacetimeDB Server Module Dir", 
                projectRoot, defaultName: "");
            
            if (string.IsNullOrWhiteSpace(initProjDirPath))
            {
                return;
            }
            
            SpacetimeCliResult cliResult = await SpacetimeDbCliActions.CreateNewServerModule(lang, initProjDirPath);
            
            if (cliResult.HasCliErr)
            {
                string errMsg = $"Error creating new SpacetimeDB Server Module at {initProjDirPath}:\n\n{cliResult.CliError}";
                Debug.LogError(errMsg);
                
                // Show modal editor popup error message with a [Close] button
                return;
            }
            
            // Create a cancellable success window with 2 buttons: [Open Project] [Open in Explorer]
            string successMsg = $"Successfully created new SpacetimeDB Server Module at {initProjDirPath}";
            Debug.Log(successMsg);
            
            // Create buttons
            const string openExplorerBtnStr = "Open in Explorer";
            const string openProjBtnStr = "Open Project";
            Dictionary<string, Action> btnNameActionDict = new()
            {
                { openExplorerBtnStr, () => onOpenExplorerBtnClick(lang, initProjDirPath) },
                // { openProjBtnStr, () => onOpenProjBtnClick(lang, initProjDirPath) }, // TODO
            };
            
            // ###############################################################
            // TODO:
            // - For C: `.NET 8` + wasm-experimental workload is required
            // - For Rust: Cargo is required 
            // ###############################################################
            
            SpacetimePopupWindow.ShowWindowOpts opts = new()
            {
                title = "Server Module Created",
                body = "Success:",
                prefixBodyIcon = SpacetimePopupWindow.PrefixBodyIcon.SuccessCircle,
                Width = 250,
                Height = 100,
                isModal = true,
                ButtonNameActionDict = btnNameActionDict,
            };
            
            SpacetimePopupWindow.ShowWindow(opts);
        }

        /// Open explorer to the project directory
        private static void onOpenExplorerBtnClick(SpacetimeMeta.ModuleLang lang, string initProjDirPath) =>
            SpacetimeWindow.OpenDirectoryWindow(initProjDirPath);

        private static void onOpenProjBtnClick(SpacetimeMeta.ModuleLang lang, string initProjDirPath)
        {
            // c#: open SpacetimeMeta.DEFAULT_CS_MODULE_PROJ_FILE
            // rust: open SpacetimeMeta.DEFAULT_RUST_MODULE_PROJ_FILE
            string projFilePath = lang switch
            {
                SpacetimeMeta.ModuleLang.CSharp => SpacetimeMeta.DEFAULT_CS_MODULE_PROJ_FILE,
                SpacetimeMeta.ModuleLang.Rust => SpacetimeMeta.DEFAULT_RUST_MODULE_PROJ_FILE,
                _ => throw new ArgumentOutOfRangeException()
            };
            
            // Open that file
            string projFileFullPath = $"{initProjDirPath}/{projFilePath}";
            throw new NotImplementedException("TODO: Open project");
        }
    }
}