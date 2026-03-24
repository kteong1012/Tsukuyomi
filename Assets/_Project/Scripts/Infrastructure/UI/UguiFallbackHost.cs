using System.Collections.Generic;
using Tsukuyomi.Application.UI;
using Tsukuyomi.Domain.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Tsukuyomi.Infrastructure.UI
{
    public sealed class UguiFallbackHost : IUguiFallbackHost
    {
        private readonly HashSet<ScreenId> _whitelist = new()
        {
            ScreenId.UguiFallbackDemo
        };

        private readonly Dictionary<ScreenId, GameObject> _instances = new();

        public bool Show(ScreenId screenId)
        {
            if (!_whitelist.Contains(screenId))
            {
                Debug.LogWarning($"Screen '{screenId}' is not allowed to use uGUI fallback.");
                return false;
            }

            if (_instances.TryGetValue(screenId, out var existing))
            {
                existing.SetActive(true);
                return true;
            }

            var instance = BuildFallbackCanvas(screenId);
            _instances[screenId] = instance;
            return true;
        }

        public bool Hide(ScreenId screenId)
        {
            if (!_instances.TryGetValue(screenId, out var instance))
            {
                return false;
            }

            instance.SetActive(false);
            return true;
        }

        public bool IsVisible(ScreenId screenId)
        {
            return _instances.TryGetValue(screenId, out var instance) && instance.activeSelf;
        }

        private GameObject BuildFallbackCanvas(ScreenId screenId)
        {
            var canvasGo = new GameObject($"[uGUI Fallback] {screenId}");
            Object.DontDestroyOnLoad(canvasGo);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2500;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            var panelGo = CreateUiObject("Panel", canvasGo.transform);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.25f, 0.25f);
            panelRect.anchorMax = new Vector2(0.75f, 0.75f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.12f, 0.15f, 0.95f);

            var titleGo = CreateUiObject("Title", panelGo.transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.65f);
            titleRect.anchorMax = new Vector2(0.9f, 0.9f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var title = titleGo.AddComponent<Text>();
            title.alignment = TextAnchor.MiddleCenter;
            title.font = GetBuiltinFont();
            title.fontSize = 28;
            title.color = Color.white;
            title.text = "uGUI Fallback Demo (Whitelist)";

            var descriptionGo = CreateUiObject("Description", panelGo.transform);
            var descriptionRect = descriptionGo.GetComponent<RectTransform>();
            descriptionRect.anchorMin = new Vector2(0.1f, 0.35f);
            descriptionRect.anchorMax = new Vector2(0.9f, 0.6f);
            descriptionRect.offsetMin = Vector2.zero;
            descriptionRect.offsetMax = Vector2.zero;

            var description = descriptionGo.AddComponent<Text>();
            description.alignment = TextAnchor.UpperCenter;
            description.font = GetBuiltinFont();
            description.fontSize = 18;
            description.color = new Color(0.88f, 0.9f, 0.95f, 1f);
            description.text = "Runtime UI defaults to UI Toolkit.\nThis panel exists only as a controlled escape hatch.";

            return canvasGo;
        }

        private static Font GetBuiltinFont()
        {
            var legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return legacy != null ? legacy : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
