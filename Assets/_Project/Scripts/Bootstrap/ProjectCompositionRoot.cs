using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using Tsukuyomi.Application.AutoChess;
using Tsukuyomi.Application.Config;
using Tsukuyomi.Application.Localization;
using Tsukuyomi.Application.Settings;
using Tsukuyomi.Application.UI;
using Tsukuyomi.Bootstrap.World;
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
        private const string RuntimePanelSettingsResourcePath = "UI/RuntimePanelSettings";

        private static ProjectCompositionRoot _instance;

        private IConfigHotReloadService _hotReloadService;
        private IUiNavigator _uiNavigator;
        private AutoChessBattleWorldController _battleWorldController;
        private PanelSettings _runtimePanelSettings;
        private GameObject _runtimeUiRoot;
        private bool _isComposed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInitializedForTests();
        }

        public static void EnsureInitializedForTests()
        {
            if (_instance != null)
            {
                return;
            }

            var existing = FindFirstObjectByType<ProjectCompositionRoot>();
            if (existing != null)
            {
                _instance = existing;
                return;
            }

            var rootObject = new GameObject("[ProjectCompositionRoot]");
            DontDestroyOnLoad(rootObject);
            _instance = rootObject.AddComponent<ProjectCompositionRoot>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator Start()
        {
            // Wait one frame to ensure UIDocument internals are ready before binding runtime UI.
            yield return null;
            ComposeSafely();
        }

        private void OnDestroy()
        {
            _uiNavigator?.Dispose();
            _battleWorldController?.Cleanup();
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
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void ComposeSafely()
        {
            if (_isComposed)
            {
                return;
            }

            try
            {
                Compose();
                _isComposed = true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private void Compose()
        {
            EnsureInputEventSystem();
            var uiRoot = EnsureRuntimeUiRoot();
            if (uiRoot == null)
            {
                Debug.LogError("UIDocument root visual element is not ready. UI bootstrap aborted.", this);
                return;
            }

            var validator = new JsonSchemaConfigValidator();
            var settingsRepository = new JsonConfigRepository<GameSettingsConfig>(
                "game_settings",
                validator,
                ConfigPaths.GetConfigSourcePath("game_settings"),
                ConfigPaths.GetSchemaSourcePath("game_settings"));
            _hotReloadService = new JsonConfigHotReloadService(ConfigPaths.GetConfigSourcePath("game_settings"));
            var autoChessRepository = new JsonConfigRepository<AutoChessContentConfig>(
                "autochess_content",
                validator,
                ConfigPaths.GetConfigSourcePath("autochess_content"),
                ConfigPaths.GetSchemaSourcePath("autochess_content"));
            var localizationRepository = new JsonConfigRepository<LocalizationTextsConfig>(
                "localization_texts",
                validator,
                ConfigPaths.GetConfigSourcePath("localization_texts"),
                ConfigPaths.GetSchemaSourcePath("localization_texts"));
            var battleViewRepository = new JsonConfigRepository<BattleViewConfig>(
                "battle_view",
                validator,
                ConfigPaths.GetConfigSourcePath("battle_view"),
                ConfigPaths.GetSchemaSourcePath("battle_view"));
            var settingsStore = new PlayerPrefsSettingsStore("tsukuyomi.settings.");
            var settingsService = new GameSettingsService(settingsRepository, _hotReloadService, settingsStore);
            var autoChessService = new AutoChessGameService(autoChessRepository, _hotReloadService);
            var localizationService = new LocalizationService(localizationRepository, _hotReloadService, settingsService);

            var screenDefinitions = BuildScreenDefinitions();
            var binderFactories = new Dictionary<ScreenId, Func<IUiViewBinder>>
            {
                [ScreenId.MainMenu] = () => new MainMenuViewBinder(settingsService, localizationService),
                [ScreenId.Settings] = () => new SettingsViewBinder(settingsService, localizationService),
                [ScreenId.AutoChess] = () => new AutoChessViewBinder(autoChessService, localizationService)
            };
            var uguiFallbackHost = new UguiFallbackHost();

            _uiNavigator = new UiToolkitNavigator(
                uiRoot,
                screenDefinitions,
                binderFactories,
                uguiFallbackHost);

            ProjectRuntime.Initialize(_uiNavigator, settingsService, autoChessService, localizationService);
            var battleWorldController = EnsureBattleWorldController();
            battleWorldController.Initialize(autoChessService, _uiNavigator, battleViewRepository, _hotReloadService);

            var defaultScreen = ParseDefaultScreen(settingsService.Data?.ui?.defaultScreen);
            _uiNavigator.Show(defaultScreen);
        }

        private AutoChessBattleWorldController EnsureBattleWorldController()
        {
            if (_battleWorldController != null)
            {
                return _battleWorldController;
            }

            var controllerObject = new GameObject("AutoChessBattleWorldController");
            controllerObject.transform.SetParent(transform, false);
            _battleWorldController = controllerObject.AddComponent<AutoChessBattleWorldController>();
            return _battleWorldController;
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

            try
            {
                var inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
                // Different InputSystem package versions expose this API with different visibility.
                var assignMethod = typeof(InputSystemUIInputModule).GetMethod(
                    "AssignDefaultActions",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                assignMethod?.Invoke(inputModule, Array.Empty<object>());
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"InputSystemUIInputModule init failed. Falling back to StandaloneInputModule. {exception.Message}");
                eventSystemObject.AddComponent<StandaloneInputModule>();
            }
        }

        private VisualElement EnsureRuntimeUiRoot()
        {
            if (_runtimeUiRoot == null)
            {
                _runtimeUiRoot = new GameObject("RuntimeUiRoot");
                _runtimeUiRoot.transform.SetParent(transform, false);

                var uiDocument = _runtimeUiRoot.AddComponent<UIDocument>();
                _runtimePanelSettings = CreateRuntimePanelSettings();
                uiDocument.panelSettings = _runtimePanelSettings;
            }

            var document = _runtimeUiRoot.GetComponent<UIDocument>();
            var rootVisualElement = document.rootVisualElement;
            if (rootVisualElement == null)
            {
                return null;
            }

            rootVisualElement.style.flexGrow = 1f;
            rootVisualElement.style.width = Length.Percent(100);
            rootVisualElement.style.height = Length.Percent(100);
            rootVisualElement.AddToClassList("runtime-root");
            return rootVisualElement;
        }

        private static PanelSettings CreateRuntimePanelSettings()
        {
            var panelSettingsTemplate = Resources.Load<PanelSettings>(RuntimePanelSettingsResourcePath);
            if (panelSettingsTemplate != null)
            {
                return Instantiate(panelSettingsTemplate);
            }

            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.themeStyleSheet = LoadRuntimeTheme();
            return panelSettings;
        }

        private static ThemeStyleSheet LoadRuntimeTheme()
        {
            var theme = Resources.Load<ThemeStyleSheet>("UI/UnityDefaultRuntimeTheme");
            if (theme == null)
            {
                Debug.LogError(
                    "Missing runtime UI Toolkit theme. Expected Resources/UI/UnityDefaultRuntimeTheme.tss");
            }

            return theme;
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
                    ScreenId.AutoChess,
                    "UI/AutoChessScreen",
                    new[] { "UI/AutoChessScreen" },
                    ScreenLayer.Base,
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
