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
        const string TEST_INIT_PROJ_DIR_PATH = "%USERPROFILE/temp/stdbMod";

        [MenuItem("Window/SpacetimeDB/New Server Module/C#")]
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
            await SpacetimeWindow.EnsureHasSpacetimeDbCli();
            
            // ###################################################################
            // - For C: `.NET 8` + `wasm-experimental` workload is required
            // - For Rust: `cargo` is required (which may require VS build tools?)
            // ###################################################################
            if (lang == SpacetimeMeta.ModuleLang.CSharp)
            {
                await ensureCsharpModulePrereqs();
            }
            else if (lang == SpacetimeMeta.ModuleLang.Rust)
            {
                // `cargo` isn't required to init, but we'll tell the user later if missing
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
            
            // Log -> Create a cancellable success window with 2 buttons: [Open Project] [Open in Explorer]
            Debug.Log($"Successfully created new SpacetimeDB Server Module at {initProjDirPath}");
            
            // Create buttons
            const string openExplorerBtnStr = "Open in Explorer";
            Dictionary<string, Action> btnNameActionDict = new()
            {
                { openExplorerBtnStr, () => onOpenExplorerBtnClick(lang, initProjDirPath) },
            };

            string bodyStr = "Success";
            if (!createNewServerModuleResult.HasCargo)
            {
                Debug.Log("Warning: Missing Rust's `cargo` project/package manager. " + 
                    "Install @ https://www.rust-lang.org/tools/install");
                
                const string installCargoBtnStr = "Install `cargo` (Website)";
                btnNameActionDict.Add(installCargoBtnStr, () => Application.OpenURL(SpacetimeMeta.INSTALL_CARGO_URL));

                bodyStr += " (but missing `cargo`)";
            }
            
            showInitServerModuleSuccessWindow(lang, bodyStr, btnNameActionDict);
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
                showInstallDotnet8PlusWasiWindow();
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
            pathToProjFile = SpacetimeWindow.NormalizePath(pathToProjFile);
            EditorUtility.RevealInFinder(pathToProjFile);
        }
        
        
        #region Show Windows
        private static void showInitServerModuleSuccessWindow(
            SpacetimeMeta.ModuleLang lang,
            string bodyStr = "Success", 
            Dictionary<string, Action> btnNameActionDict = null)
        {
            SpacetimePopupWindow.ShowWindowOpts opts = new()
            {
                title = $"{lang} Server Module Created",
                Body = bodyStr, // "Success [(but missing `cargo`)]"
                PrefixBodyIcon = SpacetimePopupWindow.PrefixBodyIcon.SuccessCircle,
                Width = 250,
                Height = 100,
                isModal = true,
                ButtonNameActionDict = btnNameActionDict,
                AlignMiddleCenter = true,
            };
            
            SpacetimePopupWindow.ShowWindow(opts);
        }
        
        /// Show modal editor popup error message with an [Install .NET 8+ (Website)] button
        /// If missing .NET 8, we're also going to be missing `wasm-experimental` workload, so we +show this below
        // [MenuItem("Window/SpacetimeDB/Test/showInstallDotnet8PlusWindow %#&T")] // CTRL+ALT+SHIFT+T // (!) Commment out when !testing
        private static void showInstallDotnet8PlusWasiWindow()
        {
            const string installDotnetBtnStr = "Install .NET 8+ (website)";
            Dictionary<string, Action> btnNameActionDict = new()
            {
                { installDotnetBtnStr, () => Application.OpenURL(SpacetimeMeta.DOTNET_8PLUS_URL) },
                { "Copy code below (for admin terminal)", () => GUIUtility.systemCopyBuffer = SpacetimeMeta.DOTNET_INSTALL_WASM_CMD },
            };
            
            SpacetimePopupWindow.ShowWindowOpts opts = new()
            {
                title = "Failed to Initialize C# Module",
                Body = "Missing Prerequisites:\n(1) Install .NET 8+\n(2) Copy to admin terminal:",
                ReadonlyBlockAfterBody = SpacetimeMeta.DOTNET_INSTALL_WASM_CMD,
                ReadonlyBlockBeforeBtns = false, // after
                PrefixBodyIcon = SpacetimePopupWindow.PrefixBodyIcon.ErrorCircle,
                Width = 300,
                Height = 150,
                isModal = true,
                ButtonNameActionDict = btnNameActionDict,
            };
            
            SpacetimePopupWindow.ShowWindow(opts);
        }
        
        /// Show modal editor popup error message with a command to copy to terminal
        // [MenuItem("Window/SpacetimeDB/Test/showInstallWasiWorkloadWindow %#&T")] // CTRL+ALT+SHIFT+T // (!) Commment out when !testing
        private static void showInstallWasiWorkloadWindow()
        {
            Dictionary<string, Action> btnNameActionDict = new()
            {
                { "Copy", () => GUIUtility.systemCopyBuffer = SpacetimeMeta.DOTNET_INSTALL_WASM_CMD },
            };
            
            SpacetimePopupWindow.ShowWindowOpts opts = new()
            {
                title = "dotnet workload missing",
                Body = "Missing Prerequisite - copy to admin terminal:",
                ReadonlyBlockAfterBody = "dotnet workload install wasm-experimental",
                PrefixBodyIcon = SpacetimePopupWindow.PrefixBodyIcon.ErrorCircle,
                Width = 350,
                Height = 100,
                isModal = true,
                ButtonNameActionDict = btnNameActionDict,
            };
            
            SpacetimePopupWindow.ShowWindow(opts);
        }
        #endregion // Show Windows
        
        
        #region Tests (using `TEST_` constants @ file top)
        // [MenuItem("Window/SpacetimeDB/Test/testShowInitCsharpModuleSuccessWindow %#&T")] // CTRL+ALT+SHIFT+T // (!) Commment out when !testing
        private static void testShowInitCsharpModuleSuccessWindow()
        {
            // Create buttons
            Dictionary<string, Action> btnNameActionDict = new()
            {
                { OPEN_EXPLORER_BTN_STR, () => onOpenExplorerBtnClick(SpacetimeMeta.ModuleLang.Rust, TEST_INIT_PROJ_DIR_PATH) },
            };
            
            showInitServerModuleSuccessWindow(
                SpacetimeMeta.ModuleLang.CSharp, 
                bodyStr: "Success", 
                btnNameActionDict);
        }
        
        // [MenuItem("Window/SpacetimeDB/Test/testShowInitModuleSuccessButNoCargoWindow %#&T")] // CTRL+ALT+SHIFT+T // (!) Commment out when !testing
        private static void testShowInitModuleSuccessButNoCargoWindow()
        {
            // Create buttons
            const string installCargoBtnStr = "Install `cargo` (website)";
            Dictionary<string, Action> btnNameActionDict = new()
            {
                { installCargoBtnStr, () => Application.OpenURL(SpacetimeMeta.INSTALL_CARGO_URL) },
                { OPEN_EXPLORER_BTN_STR, () => onOpenExplorerBtnClick(SpacetimeMeta.ModuleLang.Rust, TEST_INIT_PROJ_DIR_PATH) },
            };
            
            showInitServerModuleSuccessWindow(
                SpacetimeMeta.ModuleLang.Rust, 
                bodyStr: "Success (but missing `cargo`)", 
                btnNameActionDict);
        }
        
        // [MenuItem("Window/SpacetimeDB/Test/testShowInitModuleCsharpFail %#&T")] // CTRL+ALT+SHIFT+T // (!) Commment out when !testing
        private static void testShowInitModuleCsharpFail() =>
            showInstallDotnet8PlusWasiWindow();
        
        // [MenuItem("Window/SpacetimeDB/Test/testPopupWindow %#&T")] // CTRL+ALT+SHIFT+T // (!) Commment out when !testing
        private static async void testInstallSpacetimeDbCliShowModalProgressBarAsync() =>
            await SpacetimeWindow.InstallSpacetimeDbCliShowModalProgressBarAsync();
        #endregion // Tests
    }
}