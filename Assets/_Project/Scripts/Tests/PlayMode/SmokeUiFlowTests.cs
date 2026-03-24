using System.Collections;
using NUnit.Framework;
using Tsukuyomi.Bootstrap;
using Tsukuyomi.Domain.UI;
using UnityEngine.TestTools;

namespace Tsukuyomi.Tests.PlayMode
{
    public sealed class SmokeUiFlowTests
    {
        [UnityTest]
        public IEnumerator MainMenuAndSettingsOverlay_AreReachable()
        {
            ProjectCompositionRoot.EnsureInitializedForTests();
            yield return null;

            Assert.That(ProjectRuntime.Navigator, Is.Not.Null);
            Assert.That(ProjectRuntime.Navigator.IsVisible(ScreenId.MainMenu), Is.True);

            ProjectRuntime.Navigator.ShowOverlay(ScreenId.Settings);
            yield return null;
            Assert.That(ProjectRuntime.Navigator.IsVisible(ScreenId.Settings), Is.True);

            ProjectRuntime.Navigator.HideOverlay(ScreenId.Settings);
            yield return null;
            Assert.That(ProjectRuntime.Navigator.IsVisible(ScreenId.Settings), Is.False);
        }

        [UnityTest]
        public IEnumerator UguiFallbackScreen_CanOpenAndCloseWithoutBreakingUiToolkitFlow()
        {
            ProjectCompositionRoot.EnsureInitializedForTests();
            yield return null;

            ProjectRuntime.Navigator.ShowOverlay(ScreenId.UguiFallbackDemo);
            yield return null;
            Assert.That(ProjectRuntime.Navigator.IsVisible(ScreenId.UguiFallbackDemo), Is.True);

            ProjectRuntime.Navigator.HideOverlay(ScreenId.UguiFallbackDemo);
            yield return null;
            Assert.That(ProjectRuntime.Navigator.IsVisible(ScreenId.UguiFallbackDemo), Is.False);

            Assert.That(ProjectRuntime.Navigator.IsVisible(ScreenId.MainMenu), Is.True);
        }
    }
}
