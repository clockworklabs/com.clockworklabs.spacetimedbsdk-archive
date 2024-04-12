using System;
using System.Threading.Tasks;

namespace SpacetimeDB.Editor
{
    public partial class PublisherWindow
    {
        private const bool PUBLISH_WINDOW_TESTS = true;
        
        private async Task startTests()
        {
            if (!PUBLISH_WINDOW_TESTS)
            {
                return;
            }

            resetUi();
            hideUi(serverSelectedDropdown);
            serverFoldout.text = "PublisherWindowTester.PUBLISH_WINDOW_TESTS";
            
            testInstallWasmOpt();
            _ = testProgressBar();
            
            // Stop everything else
            throw new NotImplementedException($"PublisherWIndowTester done: " +
                $"Set !{nameof(PUBLISH_WINDOW_TESTS)} to init normally");
        }
        
        private async Task testProgressBar()
        {
            showUi(installCliGroupBox);
            showUi(installCliProgressBar);
            
            await startProgressBarAsync(
                installCliProgressBar,
                barTitle: "TestProgressBar ...",
                initVal: 5,
                valIncreasePerSec: 20,
                autoHideOnComplete: false);
            

        }

        private void testInstallWasmOpt()
        {
            showUi(publishResultFoldout);
            publishResultFoldout.value = true;
            
            hideUi(publishResultDateTimeTxt);
            hideUi(publishResultHostTxt);
            hideUi(publishResultDbAddressTxt);
            hideUi(publishResultIsOptimizedBuildToggle);
            hideUi(publishResultGenerateClientFilesBtn);
            hideUi(publishResultGetServerLogsBtn);
            
            showUi(installCliGroupBox);
            showUi(installWasmOptBtn);
            installWasmOptBtn.SetEnabled(true);
        }
    }
}
