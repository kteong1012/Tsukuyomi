using System;
using Tsukuyomi.Domain.UI;

namespace Tsukuyomi.Application.UI
{
    public interface IUiViewBinder : IDisposable
    {
        ScreenId ScreenId { get; }

        void Bind(IUiElementQuery query, IUiNavigator navigator);

        void Refresh();

        void Unbind();
    }
}
