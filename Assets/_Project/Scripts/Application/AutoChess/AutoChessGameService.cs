using System;
using System.Collections.Generic;
using Tsukuyomi.Application.Config;
using Tsukuyomi.Generated.Config;

namespace Tsukuyomi.Application.AutoChess
{
    public sealed class AutoChessGameService : IAutoChessGameService
    {
        private readonly IConfigRepository<AutoChessContentConfig> _repository;
        private readonly IConfigHotReloadService _hotReloadService;
        private readonly Random _random = new(20260325);

        private readonly List<string> _battleLog = new();
        private readonly Dictionary<string, UnitsItemConfig> _unitById = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SkillsItemConfig> _skillById = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, WavesItemConfig> _waveByRound = new();
        private readonly List<string> _shopPool = new();

        private AutoChessContentConfig _content = new();
        private OwnedUnit[] _board = Array.Empty<OwnedUnit>();
        private OwnedUnit[] _bench = Array.Empty<OwnedUnit>();
        private AutoChessShopOfferState[] _shopOffers = Array.Empty<AutoChessShopOfferState>();
        private int _round = 1;
        private int _gold;
        private int _hp;
        private string _battleSummary = "Ready";
        private AutoChessBattleOutcome _lastBattleOutcome = AutoChessBattleOutcome.None;
        private AutoChessBattleReplayState _battleReplay = new();
        private int _nextReplayId = 1;

        public AutoChessGameService(
            IConfigRepository<AutoChessContentConfig> repository,
            IConfigHotReloadService hotReloadService)
        {
            _repository = repository;
            _hotReloadService = hotReloadService;
            _hotReloadService.Reloaded += OnConfigReloaded;

            ReloadContent();
            StartNewRun();
        }

        public event Action Changed;

        public AutoChessSnapshot Snapshot { get; private set; } = new();

        public void StartNewRun()
        {
            EnsureContentLoaded();

            _round = 1;
            _gold = _content.metadata.startingGold;
            _hp = _content.metadata.startingHp;
            _battleSummary = "Draft your team and start battle.";
            _lastBattleOutcome = AutoChessBattleOutcome.None;
            _battleReplay = CreateEmptyReplay();
            _battleLog.Clear();

            _board = new OwnedUnit[Math.Max(1, _content.metadata.boardSize)];
            _bench = new OwnedUnit[Math.Max(1, _content.metadata.benchSize)];
            _shopOffers = new AutoChessShopOfferState[Math.Max(1, _content.metadata.shopSlots)];

            RollShop();
            PublishSnapshot();
        }

        public bool RefreshShop()
        {
            EnsureContentLoaded();

            if (_gold < _content.metadata.refreshCost)
            {
                _battleSummary = "Not enough gold to refresh.";
                PublishSnapshot();
                return false;
            }

            _gold -= _content.metadata.refreshCost;
            RollShop();
            _battleSummary = "Shop refreshed.";
            PublishSnapshot();
            return true;
        }

        public bool BuyShopOffer(int shopIndex)
        {
            EnsureContentLoaded();

            if (!IsIndexValid(shopIndex, _shopOffers.Length))
            {
                return false;
            }

            var offer = _shopOffers[shopIndex];
            if (offer == null || offer.isEmpty || !_unitById.TryGetValue(offer.unitId, out var unitDefinition))
            {
                return false;
            }

            if (_gold < unitDefinition.cost)
            {
                _battleSummary = "Not enough gold.";
                PublishSnapshot();
                return false;
            }

            var benchSlot = FindFirstEmpty(_bench);
            if (benchSlot < 0)
            {
                _battleSummary = "Bench is full.";
                PublishSnapshot();
                return false;
            }

            _gold -= unitDefinition.cost;
            _bench[benchSlot] = new OwnedUnit { unitId = unitDefinition.id };
            _shopOffers[shopIndex] = CreateEmptyOffer(shopIndex);
            RecomputeAffordability();
            _battleSummary = $"Bought {unitDefinition.name}.";
            PublishSnapshot();
            return true;
        }

        public bool MoveBenchToBoard(int benchIndex, int boardIndex)
        {
            if (!IsIndexValid(benchIndex, _bench.Length) || !IsIndexValid(boardIndex, _board.Length))
            {
                return false;
            }

            if (_bench[benchIndex] == null)
            {
                return false;
            }

            if (_board[boardIndex] != null)
            {
                return false;
            }

            _board[boardIndex] = _bench[benchIndex];
            _bench[benchIndex] = null;
            _battleSummary = "Unit moved to board.";
            PublishSnapshot();
            return true;
        }

        public bool MoveBoardToBench(int boardIndex)
        {
            if (!IsIndexValid(boardIndex, _board.Length) || _board[boardIndex] == null)
            {
                return false;
            }

            var benchSlot = FindFirstEmpty(_bench);
            if (benchSlot < 0)
            {
                _battleSummary = "Bench is full.";
                PublishSnapshot();
                return false;
            }

            _bench[benchSlot] = _board[boardIndex];
            _board[boardIndex] = null;
            _battleSummary = "Unit moved to bench.";
            PublishSnapshot();
            return true;
        }

        public bool SellBenchUnit(int benchIndex)
        {
            if (!IsIndexValid(benchIndex, _bench.Length) || _bench[benchIndex] == null)
            {
                return false;
            }

            var unitId = _bench[benchIndex].unitId;
            if (_unitById.TryGetValue(unitId, out var unit))
            {
                _gold += Math.Max(1, unit.cost - 1);
            }
            else
            {
                _gold += 1;
            }

            _bench[benchIndex] = null;
            RecomputeAffordability();
            _battleSummary = $"Sold {unit?.name ?? unitId}.";
            PublishSnapshot();
            return true;
        }

        public AutoChessBattleOutcome StartBattle()
        {
            EnsureContentLoaded();
            _battleLog.Clear();

            var nextCombatId = 1;
            var playerTeam = BuildTeamFromOwnedUnits(_board, isPlayer: true, ref nextCombatId);
            var enemyTeam = BuildEnemyTeamForRound(_round, ref nextCombatId);
            var replayEvents = new List<AutoChessReplayEventState>();
            var replayPlayerUnits = BuildReplayUnits(playerTeam);
            var replayEnemyUnits = BuildReplayUnits(enemyTeam);

            if (playerTeam.Count == 0)
            {
                _battleLog.Add("No units on board. Automatic defeat.");
                ResolveLose(enemyTeam.Count);
                _battleReplay = new AutoChessBattleReplayState
                {
                    replayId = _nextReplayId++,
                    outcome = _lastBattleOutcome,
                    playerUnits = replayPlayerUnits,
                    enemyUnits = replayEnemyUnits,
                    events = System.Array.Empty<AutoChessReplayEventState>()
                };
                PublishSnapshot();
                return _lastBattleOutcome;
            }

            var maxTurns = Math.Max(20, _content.metadata.maxBattleTurns);
            var playerCursor = -1;
            var enemyCursor = -1;

            for (var turn = 1; turn <= maxTurns; turn++)
            {
                ExecuteTurn(turn, playerTeam, enemyTeam, ref playerCursor, replayEvents);
                if (!HasAlive(enemyTeam))
                {
                    break;
                }

                ExecuteTurn(turn, enemyTeam, playerTeam, ref enemyCursor, replayEvents);
                if (!HasAlive(playerTeam))
                {
                    break;
                }
            }

            if (HasAlive(playerTeam) && !HasAlive(enemyTeam))
            {
                ResolveWin();
            }
            else
            {
                ResolveLose(CountAlive(enemyTeam));
            }

            _battleReplay = new AutoChessBattleReplayState
            {
                replayId = _nextReplayId++,
                outcome = _lastBattleOutcome,
                playerUnits = replayPlayerUnits,
                enemyUnits = replayEnemyUnits,
                events = replayEvents.ToArray()
            };

            _round++;
            RollShop();
            PublishSnapshot();
            return _lastBattleOutcome;
        }

        private void ResolveWin()
        {
            _gold += Math.Max(1, _content.metadata.winGold);
            _lastBattleOutcome = AutoChessBattleOutcome.Win;
            _battleSummary = "Victory. Prepare for the next round.";
            _battleLog.Add("Battle result: WIN");
            RecomputeAffordability();
        }

        private void ResolveLose(int aliveEnemies)
        {
            var loss = Math.Max(1, _content.metadata.lossHpBase + Math.Max(0, aliveEnemies));
            _hp = Math.Max(0, _hp - loss);
            _lastBattleOutcome = AutoChessBattleOutcome.Lose;
            _battleSummary = _hp > 0
                ? $"Defeat. Lost {loss} HP."
                : $"Defeat. Lost {loss} HP. Run ended, start a new run.";
            _battleLog.Add("Battle result: LOSE");
        }

        private void ExecuteTurn(
            int turn,
            List<CombatUnit> actingTeam,
            List<CombatUnit> defendingTeam,
            ref int cursor,
            List<AutoChessReplayEventState> replayEvents)
        {
            var actor = NextAlive(actingTeam, ref cursor);
            if (actor == null)
            {
                return;
            }

            var target = FirstAlive(defendingTeam);
            if (target == null)
            {
                return;
            }

            if (actor.skill != null && actor.mana >= actor.skill.manaCost)
            {
                CastSkill(actor, target, turn, replayEvents);
                actor.mana = 0;
            }
            else
            {
                var damage = Math.Max(1, actor.attack + actor.tempAttackBonus);
                target.hp = Math.Max(0, target.hp - damage);
                actor.mana += Math.Max(1, actor.manaPerAttack);
                target.mana += Math.Max(1, target.manaPerAttack / 2);
                _battleLog.Add(
                    $"T{turn}: {actor.displayName} attacks {target.displayName} for {damage} (target HP: {target.hp}).");
                replayEvents.Add(new AutoChessReplayEventState
                {
                    turn = turn,
                    actionType = AutoChessReplayActionType.Attack,
                    actorCombatId = actor.combatId,
                    targetCombatId = target.combatId,
                    value = damage,
                    targetHpAfter = target.hp,
                    targetDefeated = target.hp <= 0
                });
            }

            if (actor.buffTurnsRemaining > 0)
            {
                actor.buffTurnsRemaining--;
                if (actor.buffTurnsRemaining == 0)
                {
                    actor.tempAttackBonus = 0;
                    _battleLog.Add($"T{turn}: {actor.displayName}'s buff faded.");
                }
            }
        }

        private void CastSkill(
            CombatUnit actor,
            CombatUnit target,
            int turn,
            List<AutoChessReplayEventState> replayEvents)
        {
            switch (actor.skill.effectType)
            {
                case "DamageSingle":
                {
                    var damage = Math.Max(1, actor.skill.power + actor.attack / 2);
                    target.hp = Math.Max(0, target.hp - damage);
                    _battleLog.Add(
                        $"T{turn}: {actor.displayName} casts {actor.skill.name}, dealing {damage} to {target.displayName} (HP: {target.hp}).");
                    replayEvents.Add(new AutoChessReplayEventState
                    {
                        turn = turn,
                        actionType = AutoChessReplayActionType.SkillDamage,
                        actorCombatId = actor.combatId,
                        targetCombatId = target.combatId,
                        value = damage,
                        targetHpAfter = target.hp,
                        targetDefeated = target.hp <= 0,
                        skillName = actor.skill.name ?? string.Empty
                    });
                    break;
                }
                case "HealSelf":
                {
                    var heal = Math.Max(1, actor.skill.power);
                    actor.hp = Math.Min(actor.maxHp, actor.hp + heal);
                    _battleLog.Add(
                        $"T{turn}: {actor.displayName} casts {actor.skill.name}, healing {heal} (HP: {actor.hp}).");
                    replayEvents.Add(new AutoChessReplayEventState
                    {
                        turn = turn,
                        actionType = AutoChessReplayActionType.SkillHeal,
                        actorCombatId = actor.combatId,
                        targetCombatId = actor.combatId,
                        value = heal,
                        targetHpAfter = actor.hp,
                        targetDefeated = false,
                        skillName = actor.skill.name ?? string.Empty
                    });
                    break;
                }
                case "BuffAttackSelf":
                {
                    actor.tempAttackBonus = Math.Max(actor.tempAttackBonus, Math.Max(1, actor.skill.power));
                    actor.buffTurnsRemaining = Math.Max(1, actor.skill.durationTurns);
                    _battleLog.Add(
                        $"T{turn}: {actor.displayName} casts {actor.skill.name}, gaining +{actor.tempAttackBonus} attack for {actor.buffTurnsRemaining} turns.");
                    replayEvents.Add(new AutoChessReplayEventState
                    {
                        turn = turn,
                        actionType = AutoChessReplayActionType.SkillBuff,
                        actorCombatId = actor.combatId,
                        targetCombatId = actor.combatId,
                        value = actor.tempAttackBonus,
                        targetHpAfter = actor.hp,
                        targetDefeated = false,
                        skillName = actor.skill.name ?? string.Empty
                    });
                    break;
                }
                default:
                {
                    _battleLog.Add($"T{turn}: {actor.displayName} tried to cast an unknown skill effect.");
                    break;
                }
            }
        }

        private static CombatUnit NextAlive(List<CombatUnit> team, ref int cursor)
        {
            if (team.Count == 0)
            {
                return null;
            }

            for (var i = 0; i < team.Count; i++)
            {
                cursor = (cursor + 1) % team.Count;
                if (team[cursor].hp > 0)
                {
                    return team[cursor];
                }
            }

            return null;
        }

        private static CombatUnit FirstAlive(List<CombatUnit> team)
        {
            for (var i = 0; i < team.Count; i++)
            {
                if (team[i].hp > 0)
                {
                    return team[i];
                }
            }

            return null;
        }

        private static bool HasAlive(List<CombatUnit> team)
        {
            return CountAlive(team) > 0;
        }

        private static int CountAlive(List<CombatUnit> team)
        {
            var count = 0;
            for (var i = 0; i < team.Count; i++)
            {
                if (team[i].hp > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private List<CombatUnit> BuildTeamFromOwnedUnits(OwnedUnit[] source, bool isPlayer, ref int nextCombatId)
        {
            var result = new List<CombatUnit>();
            for (var i = 0; i < source.Length; i++)
            {
                var owned = source[i];
                if (owned == null)
                {
                    continue;
                }

                if (_unitById.TryGetValue(owned.unitId, out var unitDefinition))
                {
                    result.Add(CreateCombatUnit(unitDefinition, isPlayer, nextCombatId++));
                }
            }

            return result;
        }

        private List<CombatUnit> BuildEnemyTeamForRound(int round, ref int nextCombatId)
        {
            var result = new List<CombatUnit>();

            if (_waveByRound.TryGetValue(round, out var wave) && wave.enemyUnitIds != null && wave.enemyUnitIds.Length > 0)
            {
                for (var i = 0; i < wave.enemyUnitIds.Length; i++)
                {
                    if (_unitById.TryGetValue(wave.enemyUnitIds[i], out var definition))
                    {
                        result.Add(CreateCombatUnit(definition, isPlayer: false, nextCombatId++));
                    }
                }
            }

            if (result.Count > 0)
            {
                return result;
            }

            var generatedCount = Math.Max(2, Math.Min(_content.metadata.boardSize, 2 + round / 2));
            for (var i = 0; i < generatedCount; i++)
            {
                var unitId = DrawRandomUnitId();
                if (_unitById.TryGetValue(unitId, out var definition))
                {
                    result.Add(CreateCombatUnit(definition, isPlayer: false, nextCombatId++));
                }
            }

            return result;
        }

        private CombatUnit CreateCombatUnit(UnitsItemConfig unitDefinition, bool isPlayer, int combatId)
        {
            SkillsItemConfig skill = null;
            if (!string.IsNullOrWhiteSpace(unitDefinition.skillId))
            {
                _skillById.TryGetValue(unitDefinition.skillId, out skill);
            }

            return new CombatUnit
            {
                side = isPlayer ? "Player" : "Enemy",
                isPlayerSide = isPlayer,
                combatId = combatId,
                unitId = unitDefinition.id,
                displayName = unitDefinition.name,
                hp = Math.Max(1, unitDefinition.maxHp),
                maxHp = Math.Max(1, unitDefinition.maxHp),
                attack = Math.Max(1, unitDefinition.attack),
                manaPerAttack = Math.Max(1, unitDefinition.manaPerAttack),
                mana = 0,
                skill = skill
            };
        }

        private static AutoChessReplayUnitState[] BuildReplayUnits(List<CombatUnit> team)
        {
            var result = new AutoChessReplayUnitState[team.Count];
            for (var i = 0; i < team.Count; i++)
            {
                var unit = team[i];
                result[i] = new AutoChessReplayUnitState
                {
                    combatId = unit.combatId,
                    isPlayerSide = unit.isPlayerSide,
                    unitId = unit.unitId,
                    displayName = unit.displayName,
                    maxHp = unit.maxHp,
                    startingHp = unit.hp
                };
            }

            return result;
        }

        private void RollShop()
        {
            for (var i = 0; i < _shopOffers.Length; i++)
            {
                var unitId = DrawRandomUnitId();
                if (!_unitById.TryGetValue(unitId, out var unit))
                {
                    _shopOffers[i] = CreateEmptyOffer(i);
                    continue;
                }

                _shopOffers[i] = new AutoChessShopOfferState
                {
                    index = i,
                    isEmpty = false,
                    unitId = unit.id,
                    displayName = unit.name,
                    cost = unit.cost,
                    canAfford = _gold >= unit.cost
                };
            }
        }

        private void RecomputeAffordability()
        {
            for (var i = 0; i < _shopOffers.Length; i++)
            {
                var offer = _shopOffers[i];
                if (offer == null || offer.isEmpty)
                {
                    continue;
                }

                offer.canAfford = _gold >= offer.cost;
            }
        }

        private string DrawRandomUnitId()
        {
            if (_shopPool.Count == 0)
            {
                return string.Empty;
            }

            return _shopPool[_random.Next(0, _shopPool.Count)];
        }

        private static int FindFirstEmpty(OwnedUnit[] slots)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsIndexValid(int index, int length)
        {
            return index >= 0 && index < length;
        }

        private static AutoChessShopOfferState CreateEmptyOffer(int index)
        {
            return new AutoChessShopOfferState
            {
                index = index,
                isEmpty = true,
                unitId = string.Empty,
                displayName = "Empty",
                cost = 0,
                canAfford = false
            };
        }

        private AutoChessBattleReplayState CreateEmptyReplay()
        {
            return new AutoChessBattleReplayState
            {
                replayId = _nextReplayId++,
                outcome = AutoChessBattleOutcome.None,
                playerUnits = System.Array.Empty<AutoChessReplayUnitState>(),
                enemyUnits = System.Array.Empty<AutoChessReplayUnitState>(),
                events = System.Array.Empty<AutoChessReplayEventState>()
            };
        }

        private void PublishSnapshot()
        {
            Snapshot = BuildSnapshot();
            Changed?.Invoke();
        }

        private AutoChessSnapshot BuildSnapshot()
        {
            return new AutoChessSnapshot
            {
                round = _round,
                gold = _gold,
                hp = _hp,
                refreshCost = _content.metadata.refreshCost,
                battleSummary = _battleSummary,
                lastBattleOutcome = _lastBattleOutcome,
                shopOffers = BuildShopOffers(),
                boardSlots = BuildBoardSlots(),
                benchSlots = BuildBenchSlots(),
                battleLog = _battleLog.ToArray(),
                battleReplay = CloneBattleReplay(_battleReplay)
            };
        }

        private static AutoChessBattleReplayState CloneBattleReplay(AutoChessBattleReplayState replay)
        {
            replay ??= new AutoChessBattleReplayState();

            var playerUnits = new AutoChessReplayUnitState[replay.playerUnits?.Length ?? 0];
            for (var i = 0; i < playerUnits.Length; i++)
            {
                var source = replay.playerUnits[i];
                playerUnits[i] = new AutoChessReplayUnitState
                {
                    combatId = source.combatId,
                    isPlayerSide = source.isPlayerSide,
                    unitId = source.unitId,
                    displayName = source.displayName,
                    maxHp = source.maxHp,
                    startingHp = source.startingHp
                };
            }

            var enemyUnits = new AutoChessReplayUnitState[replay.enemyUnits?.Length ?? 0];
            for (var i = 0; i < enemyUnits.Length; i++)
            {
                var source = replay.enemyUnits[i];
                enemyUnits[i] = new AutoChessReplayUnitState
                {
                    combatId = source.combatId,
                    isPlayerSide = source.isPlayerSide,
                    unitId = source.unitId,
                    displayName = source.displayName,
                    maxHp = source.maxHp,
                    startingHp = source.startingHp
                };
            }

            var events = new AutoChessReplayEventState[replay.events?.Length ?? 0];
            for (var i = 0; i < events.Length; i++)
            {
                var source = replay.events[i];
                events[i] = new AutoChessReplayEventState
                {
                    turn = source.turn,
                    actionType = source.actionType,
                    actorCombatId = source.actorCombatId,
                    targetCombatId = source.targetCombatId,
                    value = source.value,
                    targetHpAfter = source.targetHpAfter,
                    skillName = source.skillName,
                    targetDefeated = source.targetDefeated
                };
            }

            return new AutoChessBattleReplayState
            {
                replayId = replay.replayId,
                outcome = replay.outcome,
                playerUnits = playerUnits,
                enemyUnits = enemyUnits,
                events = events
            };
        }

        private AutoChessShopOfferState[] BuildShopOffers()
        {
            var offers = new AutoChessShopOfferState[_shopOffers.Length];
            for (var i = 0; i < _shopOffers.Length; i++)
            {
                var source = _shopOffers[i] ?? CreateEmptyOffer(i);
                offers[i] = new AutoChessShopOfferState
                {
                    index = source.index,
                    isEmpty = source.isEmpty,
                    unitId = source.unitId,
                    displayName = source.displayName,
                    cost = source.cost,
                    canAfford = source.canAfford
                };
            }

            return offers;
        }

        private AutoChessBoardSlotState[] BuildBoardSlots()
        {
            var result = new AutoChessBoardSlotState[_board.Length];
            for (var i = 0; i < _board.Length; i++)
            {
                result[i] = new AutoChessBoardSlotState
                {
                    index = i,
                    isEmpty = _board[i] == null,
                    unit = BuildUnitState(_board[i]?.unitId)
                };
            }

            return result;
        }

        private AutoChessBenchSlotState[] BuildBenchSlots()
        {
            var result = new AutoChessBenchSlotState[_bench.Length];
            for (var i = 0; i < _bench.Length; i++)
            {
                result[i] = new AutoChessBenchSlotState
                {
                    index = i,
                    isEmpty = _bench[i] == null,
                    unit = BuildUnitState(_bench[i]?.unitId)
                };
            }

            return result;
        }

        private AutoChessUnitState BuildUnitState(string unitId)
        {
            if (string.IsNullOrWhiteSpace(unitId) || !_unitById.TryGetValue(unitId, out var unit))
            {
                return null;
            }

            var skillName = string.Empty;
            if (!string.IsNullOrWhiteSpace(unit.skillId) && _skillById.TryGetValue(unit.skillId, out var skill))
            {
                skillName = skill.name;
            }

            return new AutoChessUnitState
            {
                unitId = unit.id,
                displayName = unit.name,
                cost = unit.cost,
                maxHp = unit.maxHp,
                attack = unit.attack,
                skillName = skillName
            };
        }

        private void EnsureContentLoaded()
        {
            if (_content == null || _content.units == null || _content.units.Length == 0)
            {
                ReloadContent();
            }
        }

        private void OnConfigReloaded(string configName)
        {
            if (!string.Equals(configName, _repository.ConfigName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ReloadContent();
            SanitizeOwnedUnits();
            ResizeOwnedSlotsToMetadata();
            RollShop();
            _battleSummary = "AutoChess content hot reloaded.";
            PublishSnapshot();
        }

        private void ReloadContent()
        {
            _content = _repository.Reload();
            _unitById.Clear();
            _skillById.Clear();
            _waveByRound.Clear();
            _shopPool.Clear();

            if (_content.skills != null)
            {
                for (var i = 0; i < _content.skills.Length; i++)
                {
                    var skill = _content.skills[i];
                    if (!string.IsNullOrWhiteSpace(skill.id))
                    {
                        _skillById[skill.id] = skill;
                    }
                }
            }

            if (_content.units != null)
            {
                for (var i = 0; i < _content.units.Length; i++)
                {
                    var unit = _content.units[i];
                    if (!string.IsNullOrWhiteSpace(unit.id))
                    {
                        _unitById[unit.id] = unit;
                    }
                }
            }

            if (_content.waves != null)
            {
                for (var i = 0; i < _content.waves.Length; i++)
                {
                    var wave = _content.waves[i];
                    _waveByRound[wave.round] = wave;
                }
            }

            if (_content.shopRules?.offerUnitIds != null)
            {
                for (var i = 0; i < _content.shopRules.offerUnitIds.Length; i++)
                {
                    var id = _content.shopRules.offerUnitIds[i];
                    if (_unitById.ContainsKey(id))
                    {
                        _shopPool.Add(id);
                    }
                }
            }

            if (_shopPool.Count == 0)
            {
                foreach (var unitId in _unitById.Keys)
                {
                    _shopPool.Add(unitId);
                }
            }
        }

        private void SanitizeOwnedUnits()
        {
            for (var i = 0; i < _board.Length; i++)
            {
                if (_board[i] != null && !_unitById.ContainsKey(_board[i].unitId))
                {
                    _board[i] = null;
                }
            }

            for (var i = 0; i < _bench.Length; i++)
            {
                if (_bench[i] != null && !_unitById.ContainsKey(_bench[i].unitId))
                {
                    _bench[i] = null;
                }
            }
        }

        private void ResizeOwnedSlotsToMetadata()
        {
            var targetBoardSize = Math.Max(1, _content.metadata.boardSize);
            var targetBenchSize = Math.Max(1, _content.metadata.benchSize);
            var targetShopSlots = Math.Max(1, _content.metadata.shopSlots);

            if (_board.Length == targetBoardSize &&
                _bench.Length == targetBenchSize &&
                _shopOffers.Length == targetShopSlots)
            {
                return;
            }

            var carriedUnits = new List<OwnedUnit>();
            for (var i = 0; i < _board.Length; i++)
            {
                if (_board[i] != null)
                {
                    carriedUnits.Add(_board[i]);
                }
            }

            for (var i = 0; i < _bench.Length; i++)
            {
                if (_bench[i] != null)
                {
                    carriedUnits.Add(_bench[i]);
                }
            }

            _board = new OwnedUnit[targetBoardSize];
            _bench = new OwnedUnit[targetBenchSize];
            _shopOffers = new AutoChessShopOfferState[targetShopSlots];

            var carryCursor = 0;
            for (var i = 0; i < _board.Length && carryCursor < carriedUnits.Count; i++, carryCursor++)
            {
                _board[i] = carriedUnits[carryCursor];
            }

            for (var i = 0; i < _bench.Length && carryCursor < carriedUnits.Count; i++, carryCursor++)
            {
                _bench[i] = carriedUnits[carryCursor];
            }
        }

        private sealed class OwnedUnit
        {
            public string unitId = string.Empty;
        }

        private sealed class CombatUnit
        {
            public string side = string.Empty;
            public bool isPlayerSide;
            public int combatId;
            public string unitId = string.Empty;
            public string displayName = string.Empty;
            public int hp;
            public int maxHp;
            public int attack;
            public int manaPerAttack;
            public int mana;
            public int tempAttackBonus;
            public int buffTurnsRemaining;
            public SkillsItemConfig skill;
        }
    }
}
