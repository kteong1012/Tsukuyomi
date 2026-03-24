using Tsukuyomi.Domain.UI;

namespace Tsukuyomi.Application.UI
{
    public interface IUguiFallbackHost
    {
        bool Show(ScreenId screenId);

        bool Hide(ScreenId screenId);

        bool IsVisible(ScreenId screenId);
    }
}
