#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tsukuyomi.Editor
{
    [InitializeOnLoad]
    internal static class RuntimePanelSettingsAssetSetup
    {
        private const string ThemeAssetPath = "Assets/_Project/Resources/UI/UnityDefaultRuntimeTheme.tss";
        private const string PanelSettingsAssetPath = "Assets/_Project/Resources/UI/RuntimePanelSettings.asset";

        static RuntimePanelSettingsAssetSetup()
        {
            EnsureRuntimePanelSettingsAsset();
        }

        [MenuItem("Tsukuyomi/UI/Regenerate Runtime PanelSettings")]
        private static void RegenerateRuntimePanelSettings()
        {
            EnsureRuntimePanelSettingsAsset(forceRecreate: true);
        }

        private static void EnsureRuntimePanelSettingsAsset(bool forceRecreate = false)
        {
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemeAssetPath);
            if (theme == null)
            {
                Debug.LogWarning($"Cannot create RuntimePanelSettings, missing theme: {ThemeAssetPath}");
                return;
            }

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsAssetPath);
            if (panelSettings == null || forceRecreate)
            {
                if (panelSettings != null && forceRecreate)
                {
                    AssetDatabase.DeleteAsset(PanelSettingsAssetPath);
                }

                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsAssetPath);
            }

            var updated = false;
            if (panelSettings.themeStyleSheet != theme)
            {
                panelSettings.themeStyleSheet = theme;
                updated = true;
            }

            if (updated)
            {
                EditorUtility.SetDirty(panelSettings);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif
