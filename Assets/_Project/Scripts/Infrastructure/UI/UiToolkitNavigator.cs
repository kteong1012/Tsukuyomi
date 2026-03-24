using System;
using System.Collections.Generic;
using Tsukuyomi.Application.UI;
using Tsukuyomi.Domain.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tsukuyomi.Infrastructure.UI
{
    public sealed class UiToolkitNavigator : IUiNavigator, IDisposable
    {
        private readonly VisualElement _root;
        private readonly IUguiFallbackHost _uguiFallbackHost;
        private readonly Dictionary<ScreenId, ScreenDefinition> _definitions;
        private readonly Dictionary<ScreenId, Func<IUiViewBinder>> _binderFactories;
        private readonly Dictionary<ScreenId, RuntimeScreen> _runtimeScreens;
        private readonly UiNavigationState _state;

        public UiToolkitNavigator(
            VisualElement root,
            IEnumerable<ScreenDefinition> definitions,
            IDictionary<ScreenId, Func<IUiViewBinder>> binderFactories,
            IUguiFallbackHost uguiFallbackHost)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _uguiFallbackHost = uguiFallbackHost ?? throw new ArgumentNullException(nameof(uguiFallbackHost));
            _definitions = new Dictionary<ScreenId, ScreenDefinition>();
            _binderFactories = new Dictionary<ScreenId, Func<IUiViewBinder>>(binderFactories);
            _runtimeScreens = new Dictionary<ScreenId, RuntimeScreen>();
            _state = new UiNavigationState();

            foreach (var definition in definitions)
            {
                _definitions[definition.ScreenId] = definition;
            }

            _root.style.flexGrow = 1f;
        }

        public void Show(ScreenId screenId)
        {
            Replace(screenId);
        }

        public void Push(ScreenId screenId)
        {
            var definition = GetDefinition(screenId);
            var currentTop = GetTopScreen();
            if (currentTop.HasValue && currentTop.Value != screenId)
            {
                HideRuntimeVisual(currentTop.Value);
            }

            _state.Push(screenId);
            ShowRuntimeVisual(screenId, definition, asOverlay: false);
        }

        public void Replace(ScreenId screenId)
        {
            if (_state.Pop(out var removedScreen))
            {
                HideRuntimeVisual(removedScreen);
            }

            _state.Push(screenId);
            ShowRuntimeVisual(screenId, GetDefinition(screenId), asOverlay: false);
        }

        public bool Close(ScreenId screenId)
        {
            if (_state.HideOverlay(screenId))
            {
                HideRuntimeVisual(screenId);
                return true;
            }

            if (!_state.Remove(screenId))
            {
                return false;
            }

            HideRuntimeVisual(screenId);
            if (GetTopScreen() is { } topScreen)
            {
                ShowRuntimeVisual(topScreen, GetDefinition(topScreen), asOverlay: false);
            }

            return true;
        }

        public bool Pop()
        {
            if (!_state.Pop(out var removedScreen))
            {
                return false;
            }

            HideRuntimeVisual(removedScreen);
            if (GetTopScreen() is { } topScreen)
            {
                ShowRuntimeVisual(topScreen, GetDefinition(topScreen), asOverlay: false);
            }

            return true;
        }

        public void ShowOverlay(ScreenId screenId)
        {
            _state.ShowOverlay(screenId);
            ShowRuntimeVisual(screenId, GetDefinition(screenId), asOverlay: true);
        }

        public bool HideOverlay(ScreenId screenId)
        {
            if (!_state.HideOverlay(screenId))
            {
                return false;
            }

            HideRuntimeVisual(screenId);
            return true;
        }

        public bool IsVisible(ScreenId screenId)
        {
            return _state.IsVisible(screenId);
        }

        public void Dispose()
        {
            foreach (var runtime in _runtimeScreens.Values)
            {
                runtime.Binder?.Unbind();
                runtime.Binder?.Dispose();
                runtime.Root?.RemoveFromHierarchy();
            }

            _runtimeScreens.Clear();
        }

        private ScreenDefinition GetDefinition(ScreenId screenId)
        {
            if (_definitions.TryGetValue(screenId, out var definition))
            {
                return definition;
            }

            throw new InvalidOperationException($"No screen definition for '{screenId}'.");
        }

        private ScreenId? GetTopScreen()
        {
            if (_state.Stack.Count == 0)
            {
                return null;
            }

            return _state.Stack[_state.Stack.Count - 1];
        }

        private void ShowRuntimeVisual(ScreenId screenId, ScreenDefinition definition, bool asOverlay)
        {
            if (definition.UseUguiFallback)
            {
                _uguiFallbackHost.Show(screenId);
                return;
            }

            var runtime = EnsureRuntimeScreen(screenId, definition);
            runtime.Root.style.display = DisplayStyle.Flex;
            runtime.Root.pickingMode = PickingMode.Position;

            if (asOverlay || definition.Layer != ScreenLayer.Base)
            {
                runtime.Root.BringToFront();
            }
            else
            {
                runtime.Root.SendToBack();
            }

            runtime.Binder?.Refresh();
        }

        private void HideRuntimeVisual(ScreenId screenId)
        {
            if (!_definitions.TryGetValue(screenId, out var definition))
            {
                return;
            }

            if (definition.UseUguiFallback)
            {
                _uguiFallbackHost.Hide(screenId);
                return;
            }

            if (!_runtimeScreens.TryGetValue(screenId, out var runtime))
            {
                return;
            }

            runtime.Root.style.display = DisplayStyle.None;
            if (!definition.CacheInstance)
            {
                runtime.Binder?.Unbind();
                runtime.Binder?.Dispose();
                runtime.Root.RemoveFromHierarchy();
                _runtimeScreens.Remove(screenId);
            }
        }

        private RuntimeScreen EnsureRuntimeScreen(ScreenId screenId, ScreenDefinition definition)
        {
            if (_runtimeScreens.TryGetValue(screenId, out var runtime))
            {
                return runtime;
            }

            var visualTree = Resources.Load<VisualTreeAsset>(definition.UxmlPath);
            if (visualTree == null)
            {
                throw new InvalidOperationException(
                    $"Cannot load UXML '{definition.UxmlPath}' for screen '{screenId}'.");
            }

            var root = visualTree.Instantiate();
            root.name = screenId.ToString();
            root.style.display = DisplayStyle.None;
            root.AddToClassList("screen-root");

            if (definition.Layer == ScreenLayer.Base)
            {
                root.style.position = Position.Relative;
                root.style.flexGrow = 1f;
            }
            else
            {
                // Overlay/modal screens must be detached from normal layout flow.
                root.style.position = Position.Absolute;
                root.style.left = 0;
                root.style.right = 0;
                root.style.top = 0;
                root.style.bottom = 0;
            }

            foreach (var ussPath in definition.UssPaths)
            {
                if (string.IsNullOrWhiteSpace(ussPath))
                {
                    continue;
                }

                var styleSheet = Resources.Load<StyleSheet>(ussPath);
                if (styleSheet != null)
                {
                    root.styleSheets.Add(styleSheet);
                }
                else
                {
                    Debug.LogWarning($"Missing USS '{ussPath}' for screen '{screenId}'.");
                }
            }

            _root.Add(root);

            IUiViewBinder binder = null;
            if (_binderFactories.TryGetValue(screenId, out var binderFactory) && binderFactory != null)
            {
                binder = binderFactory();
                binder.Bind(new UiToolkitElementQuery(root), this);
            }

            runtime = new RuntimeScreen(definition, root, binder);
            _runtimeScreens[screenId] = runtime;
            return runtime;
        }

        private sealed class RuntimeScreen
        {
            public RuntimeScreen(ScreenDefinition definition, VisualElement root, IUiViewBinder binder)
            {
                Definition = definition;
                Root = root;
                Binder = binder;
            }

            public ScreenDefinition Definition { get; }

            public VisualElement Root { get; }

            public IUiViewBinder Binder { get; }
        }
    }
}
