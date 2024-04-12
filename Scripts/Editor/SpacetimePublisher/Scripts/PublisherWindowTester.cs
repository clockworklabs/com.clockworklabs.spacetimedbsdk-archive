using System;
using System.Threading.Tasks;
using UnityEngine;

namespace SpacetimeDB.Editor
{
    public partial class PublisherWindow
    {
        private const bool PUBLISH_WINDOW_TESTS = false;
        
        private void startTest(string funcName)
        {
            Debug.Log($"<color=orange>START {funcName}</color>");
            resetUi();
        }
        
        private async Task testProgressBar()
        {
            startTest(nameof(testProgressBar));
            showUi(installCliGroupBox);
            showUi(installCliProgressBar);
            
            await startProgressBarAsync(
                installCliProgressBar,
                barTitle: "TestProgressBar ...",
                initVal: 5,
                valIncreasePerSec: 20,
                autoHideOnComplete: false);
            
            // Stop everything else
            throw new NotImplementedException($"PublisherWIndowTester done: " +
                $"Set !{nameof(PUBLISH_WINDOW_TESTS)} to init normally");
        }

        private async Task startTests()
        {
            if (!PUBLISH_WINDOW_TESTS)
            {
                return;
            }
            
            await testProgressBar();
        }
    }
}
