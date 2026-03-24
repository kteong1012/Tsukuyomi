using NUnit.Framework;
using Tsukuyomi.Application.UI;
using Tsukuyomi.Domain.UI;

namespace Tsukuyomi.Tests.EditMode
{
    public sealed class UiNavigationStateTests
    {
        [Test]
        public void PushPopReplaceAndOverlay_WorkAsExpected()
        {
            var state = new UiNavigationState();

            state.Push(ScreenId.MainMenu);
            state.Push(ScreenId.Settings);
            Assert.That(state.Stack.Count, Is.EqualTo(2));
            Assert.That(state.IsVisible(ScreenId.Settings), Is.True);

            state.Replace(ScreenId.MainMenu);
            Assert.That(state.Stack.Count, Is.EqualTo(2));
            Assert.That(state.Stack[1], Is.EqualTo(ScreenId.MainMenu));

            state.ShowOverlay(ScreenId.UguiFallbackDemo);
            Assert.That(state.IsVisible(ScreenId.UguiFallbackDemo), Is.True);
            Assert.That(state.HideOverlay(ScreenId.UguiFallbackDemo), Is.True);
            Assert.That(state.IsVisible(ScreenId.UguiFallbackDemo), Is.False);

            Assert.That(state.Pop(out var removed), Is.True);
            Assert.That(removed, Is.EqualTo(ScreenId.MainMenu));
            Assert.That(state.Stack.Count, Is.EqualTo(1));
        }
    }
}
