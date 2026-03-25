using System;

namespace Tsukuyomi.Generated.Config
{
    [Serializable]
    public sealed class MetadataConfig
    {
        public int startingGold = 10;
        public int startingHp = 30;
        public int benchSize = 8;
        public int boardSize = 8;
        public int shopSlots = 5;
        public int refreshCost = 2;
        public int winGold = 5;
        public int lossHpBase = 2;
        public int maxBattleTurns = 200;
    }

    [Serializable]
    public sealed class ShopRulesConfig
    {
        public string[] offerUnitIds = System.Array.Empty<string>();
    }

    [Serializable]
    public sealed class SkillsItemConfig
    {
        public string id = "";
        public string name = "";
        public string description = "";
        public string triggerType = "";
        public string effectType = "";
        public int power = 0;
        public int manaCost = 0;
        public int durationTurns = 0;
    }

    [Serializable]
    public sealed class UnitsItemConfig
    {
        public string id = "";
        public string name = "";
        public int cost = 0;
        public int maxHp = 0;
        public int attack = 0;
        public int manaPerAttack = 0;
        public string skillId = "";
    }

    [Serializable]
    public sealed class WavesItemConfig
    {
        public int round = 0;
        public string[] enemyUnitIds = System.Array.Empty<string>();
    }

    [Serializable]
    public sealed class AutoChessContentConfig
    {
        public MetadataConfig metadata = new MetadataConfig();
        public ShopRulesConfig shopRules = new ShopRulesConfig();
        public SkillsItemConfig[] skills = System.Array.Empty<SkillsItemConfig>();
        public UnitsItemConfig[] units = System.Array.Empty<UnitsItemConfig>();
        public WavesItemConfig[] waves = System.Array.Empty<WavesItemConfig>();
    }
}
