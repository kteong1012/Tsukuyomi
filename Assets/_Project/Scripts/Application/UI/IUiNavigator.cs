using Tsukuyomi.Domain.UI;

namespace Tsukuyomi.Application.UI
{
    public interface IUiNavigator : System.IDisposable
    {
        void Show(ScreenId screenId);

        void Push(ScreenId screenId);

        void Replace(ScreenId screenId);

        bool Close(ScreenId screenId);

        bool Pop();

        void ShowOverlay(ScreenId screenId);

        bool HideOverlay(ScreenId screenId);

        bool IsVisible(ScreenId screenId);
    }
}
