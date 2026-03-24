using System.Collections.Generic;
using Tsukuyomi.Domain.UI;

namespace Tsukuyomi.Application.UI
{
    public sealed class UiNavigationState
    {
        private readonly List<ScreenId> _stack = new();
        private readonly HashSet<ScreenId> _overlaySet = new();
        private readonly List<ScreenId> _overlayOrder = new();

        public IReadOnlyList<ScreenId> Stack => _stack;

        public IReadOnlyList<ScreenId> Overlays => _overlayOrder;

        public void Push(ScreenId screenId)
        {
            if (_stack.Count > 0 && _stack[^1] == screenId)
            {
                return;
            }

            _stack.Add(screenId);
        }

        public bool Pop(out ScreenId removedScreen)
        {
            if (_stack.Count == 0)
            {
                removedScreen = default;
                return false;
            }

            removedScreen = _stack[^1];
            _stack.RemoveAt(_stack.Count - 1);
            return true;
        }

        public bool Remove(ScreenId screenId)
        {
            if (_overlaySet.Contains(screenId))
            {
                _overlaySet.Remove(screenId);
                _overlayOrder.Remove(screenId);
                return true;
            }

            return _stack.Remove(screenId);
        }

        public void Replace(ScreenId screenId)
        {
            if (_stack.Count > 0)
            {
                _stack.RemoveAt(_stack.Count - 1);
            }

            _stack.Add(screenId);
        }

        public void ShowOverlay(ScreenId screenId)
        {
            if (_overlaySet.Add(screenId))
            {
                _overlayOrder.Add(screenId);
            }
        }

        public bool HideOverlay(ScreenId screenId)
        {
            if (!_overlaySet.Remove(screenId))
            {
                return false;
            }

            _overlayOrder.Remove(screenId);
            return true;
        }

        public bool IsVisible(ScreenId screenId)
        {
            return _stack.Contains(screenId) || _overlaySet.Contains(screenId);
        }
    }
}
