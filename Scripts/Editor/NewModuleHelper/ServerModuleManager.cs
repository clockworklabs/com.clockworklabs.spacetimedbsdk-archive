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
            if (lang == SpacetimeMeta.ModuleLang.CSharp)
            {
                await ensureCsharpModulePrereqs();
            }
            else if (lang == SpacetimeMeta.ModuleLang.Rust)
            {
                // await ensureRustModulePrereqs(); // TODO
            }
            
            // Create a directory picker, defaulting to the project's root
            string projectRoot = Application.dataPath.Replace("/Assets", "");
            
            // Dir picker
            string initProjDirPath = EditorUtility.OpenFolderPanel("New SpacetimeDB Server Module Dir", 
                projectRoot, defaultName: "");
            
            if (string.IsNullOrWhiteSpace(initProjDirPath))
            {
                return;
            }
            
            SpacetimeCliResult cliResult = await SpacetimeDbCliActions.CreateNewServerModuleAsync(lang, initProjDirPath);
            
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
                Body = "Success:",
                PrefixBodyIcon = SpacetimePopupWindow.PrefixBodyIcon.SuccessCircle,
                Width = 250,
                Height = 100,
                isModal = true,
                ButtonNameActionDict = btnNameActionDict,
            };
            
            SpacetimePopupWindow.ShowWindow(opts);
        }

        /// Ensures `wasi-experimental` workload is installed via `dotnet`
        private static async Task ensureCsharpModulePrereqs()
        {
            CheckHasWasiWorkloadResult hasWasiWorkloadResult = await SpacetimeDbCliActions.CheckHasWasiExperimentalWorkloadAsync();
            bool hasWasiWorkload = hasWasiWorkloadResult.HasWasiWorkload;
            if (hasWasiWorkload)
            {
                return;
            }
            
            // !has wasi workload -- 1st check for .NET 8+
            Debug.Log("<b>Installing dotnet `wasi-experimental` workload ...</b>");
            CheckHasDotnet8PlusResult hasDotnet8PlusResult = await SpacetimeDbCliActions.CheckHasDotnet8PlusAsync();
            bool hasDotnet8Plus = hasDotnet8PlusResult.HasDotnet8Plus;
            if (!hasDotnet8Plus)
            {
                showInstallDotnet8PlusWindow();
                throw new Exception("dotnet 8+ is required for `wasi-experimental` workload (SpacetimeDB Server Module)");
            }
            
            // Install `wasi-experimental` workload: Requires admin, so we show a copy+paste cmd
            showInstallWasiWorkloadWindow();
        }

        /// Show modal editor popup error message with a command to copy to terminal
        // [MenuItem("Window/SpacetimeDB/Test/showInstallWasiWorkloadWindow %#&T")] // CTRL+ALT+SHIFT+T // (!) Commment out when !testing
        private static void showInstallWasiWorkloadWindow()
        {
            const string copyBtnStr = "Copy";
            const string cmd = "dotnet workload install wasm-experimental";
            Dictionary<string, Action> btnNameActionDict = new()
            {
                { copyBtnStr, () => SpacetimeWindow.CopyToClipboard(cmd) },
            };
            
            SpacetimePopupWindow.ShowWindowOpts opts = new()
            {
                title = "dotnet workload missing",
                Body = "Missing Prerequisite - copy to admin terminal:",
                readonlyBlockAfterBody = "dotnet workload install wasm-experimental",
                PrefixBodyIcon = SpacetimePopupWindow.PrefixBodyIcon.ErrorCircle,
                Width = 350,
                Height = 100,
                isModal = true,
                ButtonNameActionDict = btnNameActionDict,
            };
            
            SpacetimePopupWindow.ShowWindow(opts);
        }

        /// Show modal editor popup error message with an [Install .NET 8+ (Website)] button
        // [MenuItem("Window/SpacetimeDB/Test/showInstallDotnet8PlusWindow %#&T")] // CTRL+ALT+SHIFT+T // (!) Commment out when !testing
        private static void showInstallDotnet8PlusWindow()
        {
            const string installDotnetBtnStr = "Install .NET 8+ (Website)";
            Dictionary<string, Action> btnNameActionDict = new()
            {
                { installDotnetBtnStr, () => Application.OpenURL("https://dotnet.microsoft.com/download") },
            };
            
            SpacetimePopupWindow.ShowWindowOpts opts = new()
            {
                title = "dotnet 8+ Required",
                Body = "Missing Prerequisite",
                PrefixBodyIcon = SpacetimePopupWindow.PrefixBodyIcon.ErrorCircle,
                Width = 250,
                Height = 70,
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