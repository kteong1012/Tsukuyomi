namespace Tsukuyomi.Application.AutoChess
{
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
    }
}
