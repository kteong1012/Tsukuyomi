using System;
using System.Collections;
using System.Collections.Generic;
using Tsukuyomi.Application.AutoChess;
using Tsukuyomi.Application.Config;
using Tsukuyomi.Application.UI;
using Tsukuyomi.Domain.UI;
using Tsukuyomi.Generated.Config;
using UnityEngine;

namespace Tsukuyomi.Bootstrap.World
{
    public sealed class AutoChessBattleWorldController : MonoBehaviour
    {
        private readonly Dictionary<int, UnitVisual> _unitsByCombatId = new();
        private readonly Dictionary<string, string> _spritePathByUnitId = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Sprite> _spriteCacheByPath = new(StringComparer.OrdinalIgnoreCase);

        private IAutoChessGameService _gameService;
        private IUiNavigator _uiNavigator;
        private IConfigRepository<BattleViewConfig> _viewRepository;
        private IConfigHotReloadService _hotReloadService;

        private BattleViewConfig _viewConfig;
        private Camera _battleCamera;
        private AutoChessCameraDirector _cameraDirector;
        private Transform _unitRoot;
        private Sprite _generatedFallbackSprite;
        private Coroutine _replayCoroutine;
        private int _lastReplayId = -1;
        private bool _lastVisibleState;
        private bool _isInitialized;

        public void Initialize(
            IAutoChessGameService gameService,
            IUiNavigator uiNavigator,
            IConfigRepository<BattleViewConfig> viewRepository,
            IConfigHotReloadService hotReloadService)
        {
            if (_isInitialized)
            {
                return;
            }

            _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
            _uiNavigator = uiNavigator ?? throw new ArgumentNullException(nameof(uiNavigator));
            _viewRepository = viewRepository ?? throw new ArgumentNullException(nameof(viewRepository));
            _hotReloadService = hotReloadService ?? throw new ArgumentNullException(nameof(hotReloadService));

            ReloadViewConfig();
            EnsureRuntimeObjects();

            _gameService.Changed += OnGameChanged;
            _hotReloadService.Reloaded += OnConfigReloaded;
            _isInitialized = true;

            _lastVisibleState = _uiNavigator.IsVisible(ScreenId.AutoChess);
            _cameraDirector.SetVisible(_lastVisibleState);
            _unitRoot.gameObject.SetActive(_lastVisibleState);
            RenderIdleBoard(_gameService.Snapshot);
        }

        private void Update()
        {
            if (!_isInitialized || _cameraDirector == null || _uiNavigator == null)
            {
                return;
            }

            var isVisible = _uiNavigator.IsVisible(ScreenId.AutoChess);
            if (isVisible != _lastVisibleState)
            {
                _lastVisibleState = isVisible;
                _cameraDirector.SetVisible(isVisible);
                if (_unitRoot != null)
                {
                    _unitRoot.gameObject.SetActive(isVisible);
                }
            }

            if (isVisible)
            {
                _cameraDirector.Tick(Time.unscaledDeltaTime);
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        public void Cleanup()
        {
            if (!_isInitialized)
            {
                return;
            }

            if (_replayCoroutine != null)
            {
                StopCoroutine(_replayCoroutine);
                _replayCoroutine = null;
            }

            _hotReloadService.Reloaded -= OnConfigReloaded;
            _gameService.Changed -= OnGameChanged;
            _unitsByCombatId.Clear();
            _spriteCacheByPath.Clear();
            _spritePathByUnitId.Clear();

            if (_generatedFallbackSprite != null)
            {
                var generatedTexture = _generatedFallbackSprite.texture;
                Destroy(_generatedFallbackSprite);
                if (generatedTexture != null)
                {
                    Destroy(generatedTexture);
                }

                _generatedFallbackSprite = null;
            }

            _isInitialized = false;
        }

        private void OnConfigReloaded(string configName)
        {
            if (!string.Equals(configName, _viewRepository.ConfigName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ReloadViewConfig();
            EnsureRuntimeObjects();
            StopReplayIfRunning();
            RenderIdleBoard(_gameService.Snapshot);
        }

        private void OnGameChanged()
        {
            var snapshot = _gameService.Snapshot;
            if (snapshot == null)
            {
                return;
            }

            var replay = snapshot.battleReplay;
            if (replay != null && replay.replayId > 0 && replay.replayId != _lastReplayId)
            {
                _lastReplayId = replay.replayId;
                StopReplayIfRunning();
                _replayCoroutine = StartCoroutine(PlayReplay(replay));
                return;
            }

            if (_replayCoroutine == null)
            {
                RenderIdleBoard(snapshot);
            }
        }

        private IEnumerator PlayReplay(AutoChessBattleReplayState replay)
        {
            if (replay == null)
            {
                yield break;
            }

            RenderReplayFormation(replay);
            _cameraDirector.SetBoardView();

            var events = replay.events ?? Array.Empty<AutoChessReplayEventState>();
            for (var i = 0; i < events.Length; i++)
            {
                var step = events[i];
                if (!_unitsByCombatId.TryGetValue(step.actorCombatId, out var actor))
                {
                    continue;
                }

                if (!_unitsByCombatId.TryGetValue(step.targetCombatId, out var target))
                {
                    target = actor;
                }

                var focusPoint = (actor.anchorPosition + target.anchorPosition) * 0.5f;
                _cameraDirector.Pulse(focusPoint);
                yield return AnimateAttackHop(actor, target.anchorPosition);
                yield return FlashTarget(target, GetFlashColor(step.actionType));

                target.currentHp = step.targetHpAfter;
                if (step.targetDefeated)
                {
                    yield return FadeOutUnit(target, 0.14f);
                }

                var wait = Mathf.Max(0.02f, _viewConfig.battleWorld.turnInterval);
                yield return new WaitForSecondsRealtime(wait);
            }

            var hold = Mathf.Max(0.05f, _viewConfig.battleWorld.postBattleHold);
            yield return new WaitForSecondsRealtime(hold);
            RenderIdleBoard(_gameService.Snapshot);
            _replayCoroutine = null;
        }

        private void RenderIdleBoard(AutoChessSnapshot snapshot)
        {
            if (snapshot == null || _unitRoot == null)
            {
                return;
            }

            ClearAllUnits();

            var boardSlots = snapshot.boardSlots ?? Array.Empty<AutoChessBoardSlotState>();
            if (boardSlots.Length == 0)
            {
                _cameraDirector.SetBoardView();
                return;
            }

            var mid = (boardSlots.Length - 1) * 0.5f;
            for (var i = 0; i < boardSlots.Length; i++)
            {
                var slot = boardSlots[i];
                if (slot == null || slot.isEmpty || slot.unit == null)
                {
                    continue;
                }

                var x = _viewConfig.battleWorld.boardCenterX + (i - mid) * _viewConfig.battleWorld.laneSpacingX;
                var y = _viewConfig.battleWorld.playerRowY;
                var position = new Vector3(x, y, 0f);
                CreateUnitVisual(
                    combatId: 1000 + i,
                    slot.unit.unitId,
                    slot.unit.displayName,
                    isPlayerSide: true,
                    slot.unit.maxHp,
                    slot.unit.maxHp,
                    position);
            }

            _cameraDirector.SetBoardView();
        }

        private void RenderReplayFormation(AutoChessBattleReplayState replay)
        {
            ClearAllUnits();

            var playerUnits = replay.playerUnits ?? Array.Empty<AutoChessReplayUnitState>();
            var enemyUnits = replay.enemyUnits ?? Array.Empty<AutoChessReplayUnitState>();
            SpawnReplayUnits(playerUnits, _viewConfig.battleWorld.playerRowY);
            SpawnReplayUnits(enemyUnits, _viewConfig.battleWorld.enemyRowY);
        }

        private void SpawnReplayUnits(AutoChessReplayUnitState[] units, float rowY)
        {
            if (units.Length == 0)
            {
                return;
            }

            var mid = (units.Length - 1) * 0.5f;
            for (var i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                var x = _viewConfig.battleWorld.boardCenterX + (i - mid) * _viewConfig.battleWorld.laneSpacingX;
                var position = new Vector3(x, rowY, 0f);
                CreateUnitVisual(
                    unit.combatId,
                    unit.unitId,
                    unit.displayName,
                    unit.isPlayerSide,
                    unit.maxHp,
                    unit.startingHp,
                    position);
            }
        }

        private UnitVisual CreateUnitVisual(
            int combatId,
            string unitId,
            string displayName,
            bool isPlayerSide,
            int maxHp,
            int currentHp,
            Vector3 anchorPosition)
        {
            var unitObject = new GameObject($"Unit_{combatId}_{unitId}");
            unitObject.transform.SetParent(_unitRoot, worldPositionStays: false);
            unitObject.transform.localPosition = anchorPosition;
            unitObject.transform.localScale = Vector3.one * Mathf.Max(0.2f, _viewConfig.battleWorld.unitScale);

            var renderer = unitObject.AddComponent<SpriteRenderer>();
            renderer.sprite = ResolveSprite(unitId);
            renderer.color = GetBaseColor(isPlayerSide, renderer.sprite == GetFallbackSprite());
            renderer.sortingOrder = isPlayerSide ? 10 : 20;

            var visual = new UnitVisual
            {
                combatId = combatId,
                unitId = unitId ?? string.Empty,
                displayName = displayName ?? unitId ?? string.Empty,
                isPlayerSide = isPlayerSide,
                maxHp = maxHp,
                currentHp = currentHp,
                gameObject = unitObject,
                renderer = renderer,
                anchorPosition = anchorPosition,
                baseColor = renderer.color
            };

            _unitsByCombatId[combatId] = visual;
            return visual;
        }

        private void ClearAllUnits()
        {
            foreach (var kv in _unitsByCombatId)
            {
                if (kv.Value?.gameObject != null)
                {
                    Destroy(kv.Value.gameObject);
                }
            }

            _unitsByCombatId.Clear();
        }

        private IEnumerator AnimateAttackHop(UnitVisual actor, Vector3 targetPosition)
        {
            if (actor == null || actor.gameObject == null)
            {
                yield break;
            }

            var origin = actor.anchorPosition;
            var direction = (targetPosition - origin);
            if (direction.sqrMagnitude <= 0.0001f)
            {
                yield break;
            }

            direction.Normalize();
            var hopDistance = Mathf.Max(0.05f, _viewConfig.battleWorld.attackHopDistance);
            var peak = origin + direction * hopDistance;
            var duration = Mathf.Max(0.03f, _viewConfig.battleWorld.attackHopDuration);
            var half = duration * 0.5f;

            yield return LerpUnitPosition(actor, origin, peak, half);
            yield return LerpUnitPosition(actor, peak, origin, half);
        }

        private IEnumerator LerpUnitPosition(UnitVisual unit, Vector3 from, Vector3 to, float duration)
        {
            if (unit == null || unit.gameObject == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                unit.gameObject.transform.localPosition = to;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                unit.gameObject.transform.localPosition = Vector3.Lerp(from, to, t);
                yield return null;
            }

            unit.gameObject.transform.localPosition = to;
        }

        private IEnumerator FlashTarget(UnitVisual unit, Color flashColor)
        {
            if (unit == null || unit.renderer == null)
            {
                yield break;
            }

            var duration = Mathf.Max(0.02f, _viewConfig.battleWorld.hitFlashDuration);
            unit.renderer.color = flashColor;
            yield return new WaitForSecondsRealtime(duration);
            unit.renderer.color = unit.baseColor;
        }

        private IEnumerator FadeOutUnit(UnitVisual unit, float duration)
        {
            if (unit == null || unit.renderer == null)
            {
                yield break;
            }

            var elapsed = 0f;
            var start = unit.renderer.color;
            var end = new Color(start.r * 0.6f, start.g * 0.6f, start.b * 0.6f, 0.25f);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                unit.renderer.color = Color.Lerp(start, end, t);
                yield return null;
            }

            unit.renderer.color = end;
            unit.baseColor = end;
        }

        private Color GetFlashColor(AutoChessReplayActionType actionType)
        {
            return actionType switch
            {
                AutoChessReplayActionType.SkillHeal => new Color(0.38f, 0.95f, 0.56f, 1f),
                AutoChessReplayActionType.SkillBuff => new Color(0.56f, 0.91f, 1f, 1f),
                _ => new Color(1f, 0.42f, 0.42f, 1f)
            };
        }

        private Color GetBaseColor(bool isPlayerSide, bool isFallback)
        {
            if (isFallback)
            {
                return ParseFallbackTint();
            }

            return isPlayerSide
                ? new Color(0.94f, 0.98f, 1f, 1f)
                : new Color(1f, 0.94f, 0.94f, 1f);
        }

        private Color ParseFallbackTint()
        {
            var tintHex = _viewConfig.fallbackVisual?.tintHex;
            if (!string.IsNullOrWhiteSpace(tintHex) && ColorUtility.TryParseHtmlString(tintHex, out var color))
            {
                return color;
            }

            return new Color(0.66f, 0.72f, 0.8f, 1f);
        }

        private Sprite ResolveSprite(string unitId)
        {
            var path = string.Empty;
            if (!string.IsNullOrWhiteSpace(unitId) &&
                _spritePathByUnitId.TryGetValue(unitId, out var mappedPath) &&
                !string.IsNullOrWhiteSpace(mappedPath))
            {
                path = mappedPath;
            }

            if (!string.IsNullOrWhiteSpace(path))
            {
                if (_spriteCacheByPath.TryGetValue(path, out var cached))
                {
                    return cached ?? GetFallbackSprite();
                }

                var loaded = Resources.Load<Sprite>(path);
                _spriteCacheByPath[path] = loaded;
                if (loaded != null)
                {
                    return loaded;
                }
            }

            var fallbackPath = _viewConfig.fallbackVisual?.spriteResourcePath ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(fallbackPath))
            {
                if (_spriteCacheByPath.TryGetValue(fallbackPath, out var cachedFallback))
                {
                    return cachedFallback ?? GetFallbackSprite();
                }

                var loadedFallback = Resources.Load<Sprite>(fallbackPath);
                _spriteCacheByPath[fallbackPath] = loadedFallback;
                if (loadedFallback != null)
                {
                    return loadedFallback;
                }
            }

            return GetFallbackSprite();
        }

        private Sprite GetFallbackSprite()
        {
            if (_generatedFallbackSprite != null)
            {
                return _generatedFallbackSprite;
            }

            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "tsukuyomi_generated_unit_fallback"
            };

            var pixels = new Color[64 * 64];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            _generatedFallbackSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                64f);
            _generatedFallbackSprite.name = "generated_fallback_unit";
            return _generatedFallbackSprite;
        }

        private void EnsureRuntimeObjects()
        {
            if (_unitRoot == null)
            {
                var unitsObject = new GameObject("AutoChessBattleUnits");
                unitsObject.transform.SetParent(transform, false);
                _unitRoot = unitsObject.transform;
            }

            if (_battleCamera == null)
            {
                _battleCamera = Camera.main;
                if (_battleCamera == null)
                {
                    var cameraObject = new GameObject("AutoChessBattleCamera");
                    cameraObject.tag = "MainCamera";
                    cameraObject.transform.SetParent(transform, false);
                    _battleCamera = cameraObject.AddComponent<Camera>();
                }
            }

            _battleCamera.orthographic = true;
            _battleCamera.nearClipPlane = -50f;
            _battleCamera.farClipPlane = 50f;
            _battleCamera.clearFlags = CameraClearFlags.SolidColor;
            _battleCamera.backgroundColor = new Color(0.05f, 0.08f, 0.12f, 1f);
            _battleCamera.depth = -2f;

            _cameraDirector = new AutoChessCameraDirector(_battleCamera, _viewConfig.cameraRig);
            _cameraDirector.SetBoardView();
            _cameraDirector.ResetImmediate();
        }

        private void ReloadViewConfig()
        {
            _viewConfig = _viewRepository.Reload() ?? new BattleViewConfig();
            if (_viewConfig.battleWorld == null)
            {
                _viewConfig.battleWorld = new BattleWorldConfig();
            }

            if (_viewConfig.cameraRig == null)
            {
                _viewConfig.cameraRig = new CameraRigConfig();
            }

            if (_viewConfig.fallbackVisual == null)
            {
                _viewConfig.fallbackVisual = new FallbackVisualConfig();
            }

            _spritePathByUnitId.Clear();
            var visuals = _viewConfig.unitVisuals ?? Array.Empty<UnitVisualsItemConfig>();
            for (var i = 0; i < visuals.Length; i++)
            {
                var entry = visuals[i];
                if (string.IsNullOrWhiteSpace(entry.unitId))
                {
                    continue;
                }

                _spritePathByUnitId[entry.unitId] = entry.spriteResourcePath ?? string.Empty;
            }
        }

        private void StopReplayIfRunning()
        {
            if (_replayCoroutine != null)
            {
                StopCoroutine(_replayCoroutine);
                _replayCoroutine = null;
            }
        }

        private sealed class UnitVisual
        {
            public int combatId;
            public string unitId = string.Empty;
            public string displayName = string.Empty;
            public bool isPlayerSide;
            public int maxHp;
            public int currentHp;
            public GameObject gameObject;
            public SpriteRenderer renderer;
            public Vector3 anchorPosition;
            public Color baseColor;
        }
    }
}
