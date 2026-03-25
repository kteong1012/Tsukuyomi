namespace Tsukuyomi.Application.AutoChess
{
    public enum AutoChessReplayActionType
    {
        None = 0,
        Attack = 1,
        SkillDamage = 2,
        SkillHeal = 3,
        SkillBuff = 4
    }

    public sealed class AutoChessUnitState
    {
        public string unitId = string.Empty;
        public string displayName = string.Empty;
        public int cost;
        public int maxHp;
        public int attack;
        public string skillName = string.Empty;
    }

    public sealed class AutoChessBoardSlotState
    {
        public int index;
        public bool isEmpty;
        public AutoChessUnitState unit;
    }

    public sealed class AutoChessBenchSlotState
    {
        public int index;
        public bool isEmpty;
        public AutoChessUnitState unit;
    }

    public sealed class AutoChessShopOfferState
    {
        public int index;
        public bool isEmpty;
        public string unitId = string.Empty;
        public string displayName = string.Empty;
        public int cost;
        public bool canAfford;
    }

    public sealed class AutoChessReplayUnitState
    {
        public int combatId;
        public bool isPlayerSide;
        public string unitId = string.Empty;
        public string displayName = string.Empty;
        public int maxHp;
        public int startingHp;
    }

    public sealed class AutoChessReplayEventState
    {
        public int turn;
        public AutoChessReplayActionType actionType;
        public int actorCombatId;
        public int targetCombatId;
        public int value;
        public int targetHpAfter;
        public string skillName = string.Empty;
        public bool targetDefeated;
    }

    public sealed class AutoChessBattleReplayState
    {
        public int replayId;
        public AutoChessBattleOutcome outcome;
        public AutoChessReplayUnitState[] playerUnits = System.Array.Empty<AutoChessReplayUnitState>();
        public AutoChessReplayUnitState[] enemyUnits = System.Array.Empty<AutoChessReplayUnitState>();
        public AutoChessReplayEventState[] events = System.Array.Empty<AutoChessReplayEventState>();
    }

    public sealed class AutoChessSnapshot
    {
        public int round;
        public int gold;
        public int hp;
        public int refreshCost;
        public string battleSummary = string.Empty;
        public AutoChessBattleOutcome lastBattleOutcome;
        public AutoChessShopOfferState[] shopOffers = System.Array.Empty<AutoChessShopOfferState>();
        public AutoChessBoardSlotState[] boardSlots = System.Array.Empty<AutoChessBoardSlotState>();
        public AutoChessBenchSlotState[] benchSlots = System.Array.Empty<AutoChessBenchSlotState>();
        public string[] battleLog = System.Array.Empty<string>();
        public AutoChessBattleReplayState battleReplay = new();
    }
}
