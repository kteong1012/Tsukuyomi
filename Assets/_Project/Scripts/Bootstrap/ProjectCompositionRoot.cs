using System;
using System.Collections.Generic;
using System.Reflection;
using Tsukuyomi.Application.Config;
using Tsukuyomi.Application.Settings;
using Tsukuyomi.Application.UI;
using Tsukuyomi.Domain.UI;
using Tsukuyomi.Generated.Config;
using Tsukuyomi.Infrastructure.Config;
using Tsukuyomi.Infrastructure.UI;
using Tsukuyomi.Presentation.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UIElements;

namespace Tsukuyomi.Bootstrap
{
    public sealed class ProjectCompositionRoot : MonoBehaviour
    {
        private static bool _isBootstrapped;

        private IConfigHotReloadService _hotReloadService;
        private IUiNavigator _uiNavigator;
        private PanelSettings _runtimePanelSettings;
        private GameObject _runtimeUiRoot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInitializedForTests();
        }

        public static void EnsureInitializedForTests()
        {
            if (FindFirstObjectByType<ProjectCompositionRoot>() != null)
            {
                return;
            }

            var rootObject = new GameObject("[ProjectCompositionRoot]");
            DontDestroyOnLoad(rootObject);
            rootObject.AddComponent<ProjectCompositionRoot>();
        }

        private void Awake()
        {
            if (_isBootstrapped)
            {
                Destroy(gameObject);
                return;
            }

            _isBootstrapped = true;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Compose();
        }

        private void OnDestroy()
        {
            _uiNavigator?.Dispose();
            _hotReloadService?.Dispose();

            if (_runtimePanelSettings != null)
            {
                Destroy(_runtimePanelSettings);
            }

            if (_runtimeUiRoot != null)
            {
                Destroy(_runtimeUiRoot);
            }

            ProjectRuntime.Reset();
            _isBootstrapped = false;
        }

        private void Compose()
        {
            EnsureInputEventSystem();
            var uiRoot = EnsureRuntimeUiRoot();

            var validator = new JsonSchemaConfigValidator();
            var configRepository = new JsonConfigRepository<GameSettingsConfig>(
                "game_settings",
                validator,
                ConfigPaths.GetConfigSourcePath("game_settings"),
                ConfigPaths.GetSchemaSourcePath("game_settings"));
            _hotReloadService = new JsonConfigHotReloadService(ConfigPaths.GetConfigSourcePath("game_settings"));
            var settingsStore = new PlayerPrefsSettingsStore("tsukuyomi.settings.");
            var settingsService = new GameSettingsService(configRepository, _hotReloadService, settingsStore);

            var screenDefinitions = BuildScreenDefinitions();
            var binderFactories = new Dictionary<ScreenId, Func<IUiViewBinder>>
            {
                [ScreenId.MainMenu] = () => new MainMenuViewBinder(settingsService),
                [ScreenId.Settings] = () => new SettingsViewBinder(settingsService)
            };
            var uguiFallbackHost = new UguiFallbackHost();

            _uiNavigator = new UiToolkitNavigator(
                uiRoot,
                screenDefinitions,
                binderFactories,
                uguiFallbackHost);

            ProjectRuntime.Initialize(_uiNavigator, settingsService);

            var defaultScreen = ParseDefaultScreen(settingsService.Data?.ui?.defaultScreen);
            _uiNavigator.Show(defaultScreen);
        }

        private static void EnsureInputEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            DontDestroyOnLoad(eventSystemObject);
            eventSystemObject.AddComponent<EventSystem>();
            var inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();

            // Different InputSystem package versions expose this API with different visibility.
            var assignMethod = typeof(InputSystemUIInputModule).GetMethod(
                "AssignDefaultActions",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            assignMethod?.Invoke(inputModule, Array.Empty<object>());
        }

        private VisualElement EnsureRuntimeUiRoot()
        {
            if (_runtimeUiRoot == null)
            {
                _runtimeUiRoot = new GameObject("RuntimeUiRoot");
                _runtimeUiRoot.transform.SetParent(transform, false);

                var uiDocument = _runtimeUiRoot.AddComponent<UIDocument>();
                _runtimePanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                uiDocument.panelSettings = _runtimePanelSettings;
            }

            var document = _runtimeUiRoot.GetComponent<UIDocument>();
            var rootVisualElement = document.rootVisualElement;
            rootVisualElement.style.flexGrow = 1f;
            rootVisualElement.AddToClassList("runtime-root");
            return rootVisualElement;
        }

        private static IEnumerable<ScreenDefinition> BuildScreenDefinitions()
        {
            return new[]
            {
                new ScreenDefinition(
                    ScreenId.MainMenu,
                    "UI/MainMenuScreen",
                    new[] { "UI/MainMenuScreen" },
                    ScreenLayer.Base,
                    cacheInstance: true),
                new ScreenDefinition(
                    ScreenId.Settings,
                    "UI/SettingsScreen",
                    new[] { "UI/SettingsScreen" },
                    ScreenLayer.Modal,
                    cacheInstance: true),
                new ScreenDefinition(
                    ScreenId.UguiFallbackDemo,
                    string.Empty,
                    Array.Empty<string>(),
                    ScreenLayer.Modal,
                    cacheInstance: false,
                    useUguiFallback: true)
            };
        }

        private static ScreenId ParseDefaultScreen(string defaultScreen)
        {
            if (string.IsNullOrWhiteSpace(defaultScreen))
            {
                return ScreenId.MainMenu;
            }

            return Enum.TryParse(defaultScreen, ignoreCase: true, out ScreenId parsed)
                ? parsed
                : ScreenId.MainMenu;
        }
    }
}
