using System;
using Tsukuyomi.Application.UI;
using UnityEngine.UIElements;

namespace Tsukuyomi.Infrastructure.UI
{
    public sealed class UiToolkitElementQuery : IUiElementQuery
    {
        private readonly VisualElement _root;

        public UiToolkitElementQuery(VisualElement root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
        }

        public TElement Q<TElement>(string name) where TElement : VisualElement
        {
            return _root.Q<TElement>(name);
        }
    }
}
