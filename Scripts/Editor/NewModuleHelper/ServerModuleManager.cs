using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SpacetimeDB.Editor
{
    public static class ServerModuleManager
    {
        const string OPEN_EXPLORER_BTN_STR = "Open Folder";
        

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
            // #################################################################
            // - For C: `.NET 8` + wasm-experimental workload is required
            // - For Rust: Cargo is required (which may require VS build tools?)
            // #################################################################
            if (lang == SpacetimeMeta.ModuleLang.CSharp)
            {
                await ensureCsharpModulePrereqs();
            }
            // else if (lang == SpacetimeMeta.ModuleLang.Rust)
            // {
            //     await ensureRustModulePrereqs(); // We only need cargo, caught later
            // }
            
            // Create a directory picker, defaulting to the project's root
            string projectRoot = Application.dataPath.Replace("/Assets", "");
            
            // Dir picker
            string initProjDirPath = EditorUtility.OpenFolderPanel("New SpacetimeDB Server Module Dir", 
                projectRoot, defaultName: "");
            
            if (string.IsNullOrWhiteSpace(initProjDirPath))
            {
                return;
            }
            
            CreateNewServerModuleResult createNewServerModuleResult = await SpacetimeDbCliActions
                .CreateNewServerModuleAsync(lang, initProjDirPath);
            
            if (createNewServerModuleResult.HasCliErr)
            {
                string errMsg = "Error creating new SpacetimeDB Server Module at " +
                    $"{initProjDirPath}:\n\n{createNewServerModuleResult.CliError}";
                Debug.LogError(errMsg);
                
                // Show modal editor popup error message with a [Close] button
                return;
            }

            string bodyStr = "Success";
            
            // Log -> Create a cancellable success window with 2 buttons: [Open Project] [Open in Explorer]
            Debug.Log($"Successfully created new SpacetimeDB Server Module at {initProjDirPath}");
            
            // Create buttons
            const string openExplorerBtnStr = "Open in Explorer";
            Dictionary<string, Action> btnNameActionDict = new()
            {
                { openExplorerBtnStr, () => onOpenExplorerBtnClick(lang, initProjDirPath) },
            };

            if (!createNewServerModuleResult.HasCargo)
            {
                Debug.Log("Warning: Missing Rust's `cargo` project/package manager. " + 
                    "Install @ https://www.rust-lang.org/tools/install");
                
                const string installCargoBtnStr = "Install Cargo (Website)";
                btnNameActionDict.Add(installCargoBtnStr, () => Application.OpenURL(SpacetimeMeta.INSTALL_CARGO_URL));

                bodyStr += " (but missing `cargo`)";
            }
            
            showInitServerModuleSuccessWindow(bodyStr, btnNameActionDict);
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

        /// Open explorer to the project directory + focus the proj fileplorer
        private static void onOpenExplorerBtnClick(
            SpacetimeMeta.ModuleLang lang, 
            string initProjPathToProjDir)
        {
            string fileName = lang switch
            {
                SpacetimeMeta.ModuleLang.CSharp => SpacetimeMeta.DEFAULT_CS_MODULE_PROJ_FILE,
                SpacetimeMeta.ModuleLang.Rust => SpacetimeMeta.DEFAULT_RUST_MODULE_PROJ_FILE,
                _ => throw new ArgumentOutOfRangeException()
            };
            
            string pathToProjFile = Path.Join(initProjPathToProjDir, fileName);
            EditorUtility.RevealInFinder(pathToProjFile);
        }
        
        
        #region Show Windows
        private static void showInitServerModuleSuccessWindow(
            string bodyStr = "Success", 
            Dictionary<string, Action> btnNameActionDict = null)
        {
            SpacetimePopupWindow.ShowWindowOpts opts = new()
            {
                title = "Server Module Created",
                Body = bodyStr, // "Success [(but missing `cargo`)]"
                PrefixBodyIcon = SpacetimePopupWindow.PrefixBodyIcon.SuccessCircle,
                Width = 250,
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
        
        /// Show modal editor popup error message with a command to copy to terminal
        // [MenuItem("Window/SpacetimeDB/Test/showInstallWasiWorkloadWindow %#&T")] // CTRL+ALT+SHIFT+T // (!) Commment out when !testing
        private static void showInstallWasiWorkloadWindow()
        {
            const string cmdToCopy = "dotnet workload install wasm-experimental";
            Dictionary<string, Action> btnNameActionDict = new()
            {
                { "Copy", () => GUIUtility.systemCopyBuffer = cmdToCopy },
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
        #endregion // Show Windows
        
        
        #region Tests
        // [MenuItem("Window/SpacetimeDB/Test/testShowInitModuleSuccessWindow %#&T")] // CTRL+ALT+SHIFT+T // (!) Commment out when !testing
        private static void testShowInitModuleSuccessWindow()
        {
            const string initProjPath = "%USERPROFILE/temp/stdbMod"; // << Set test path here

            // Create buttons
            Dictionary<string, Action> btnNameActionDict = new()
            {
                { OPEN_EXPLORER_BTN_STR, () => onOpenExplorerBtnClick(SpacetimeMeta.ModuleLang.Rust, initProjPath) },
            };
            
            showInitServerModuleSuccessWindow("Success", btnNameActionDict);
        }
        
        [MenuItem("Window/SpacetimeDB/Test/testShowInitModuleSuccessButNoCargoWindow %#&T")] // CTRL+ALT+SHIFT+T // (!) Commment out when !testing
        private static void testShowInitModuleSuccessButNoCargoWindow()
        {
            const string initProjPath = "%USERPROFILE/temp/stdbMod"; // << Set test path here

            // Create buttons
            const string installCargoBtnStr = "Install Cargo (Website)";
            Dictionary<string, Action> btnNameActionDict = new()
            {
                { installCargoBtnStr, () => Application.OpenURL(SpacetimeMeta.INSTALL_CARGO_URL) },
                { OPEN_EXPLORER_BTN_STR, () => onOpenExplorerBtnClick(SpacetimeMeta.ModuleLang.Rust, initProjPath) },
            };
            
            showInitServerModuleSuccessWindow("Success (but missing `cargo`)", btnNameActionDict);
        }
        #endregion // Tests
    }
}