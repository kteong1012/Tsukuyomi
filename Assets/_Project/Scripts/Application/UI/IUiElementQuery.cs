namespace Tsukuyomi.Application.UI
{
    public interface IUiElementQuery
    {
        TElement Q<TElement>(string name) where TElement : UnityEngine.UIElements.VisualElement;
    }
}
