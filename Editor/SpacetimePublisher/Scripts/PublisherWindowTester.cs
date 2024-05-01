using System;
using System.Threading.Tasks;
using static SpacetimeDB.Editor.SpacetimeWindow;

namespace SpacetimeDB.Editor
{
    public partial class PublisherWindow
    {
        private const bool PUBLISH_WINDOW_TESTS = false;
        
        private async Task startTests()
        {
            if (!PUBLISH_WINDOW_TESTS)
            {
                return;
            }

            resetUi();
            HideUi(serverSelectedDropdown);
            serverFoldout.text = "PublisherWindowTester.PUBLISH_WINDOW_TESTS";
            
            // testInstallWasmOpt();
            _ = testProgressBar();
            
            // Stop everything else
            throw new NotImplementedException($"PublisherWIndowTester done: " +
                $"Set !{nameof(PUBLISH_WINDOW_TESTS)} to init normally");
        }
        
        private async Task testProgressBar()
        {
            ShowUi(installCliGroupBox);
            ShowUi(installCliProgressBar);
            
            _ = startProgressBarAsync(
                installCliProgressBar,
                barTitle: "TestProgressBar ...",
                initVal: 5,
                valIncreasePerSec: 20,
                autoHideOnComplete: false);
        }

        private void testInstallWasmOpt()
        {
            ShowUi(publishResultFoldout);
            publishResultFoldout.value = true;
            
            HideUi(publishResultDateTimeTxt);
            HideUi(publishResultHostTxt);
            HideUi(publishResultDbAddressTxt);
            HideUi(publishResultIsOptimizedBuildToggle);
            HideUi(publishResultGenerateClientFilesBtn);
            HideUi(publishResultGetServerLogsBtn);
            
            ShowUi(installCliGroupBox);
            ShowUi(installWasmOptBtn);
            installWasmOptBtn.SetEnabled(true);
        }
    }
}
