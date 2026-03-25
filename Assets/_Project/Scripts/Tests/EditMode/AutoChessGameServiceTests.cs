using System;
using NUnit.Framework;
using Tsukuyomi.Application.AutoChess;
using Tsukuyomi.Application.Config;
using Tsukuyomi.Generated.Config;

namespace Tsukuyomi.Tests.EditMode
{
    public sealed class AutoChessGameServiceTests
    {
        [Test]
        public void StartNewRun_BuyPlaceAndBattle_ProgressesRound()
        {
            var config = BuildBasicConfig();
            var repository = new FakeConfigRepository(config);
            var hotReload = new FakeHotReloadService();
            var service = new AutoChessGameService(repository, hotReload);

            var initial = service.Snapshot;
            Assert.That(initial.round, Is.EqualTo(1));
            Assert.That(initial.gold, Is.EqualTo(10));
            Assert.That(initial.hp, Is.EqualTo(30));

            Assert.That(service.BuyShopOffer(0), Is.True);
            var afterBuy = service.Snapshot;
            var benchIndex = FindFirstOccupiedBench(afterBuy);
            Assert.That(benchIndex, Is.GreaterThanOrEqualTo(0));

            Assert.That(service.MoveBenchToBoard(benchIndex, 0), Is.True);
            var outcome = service.StartBattle();
            var afterBattle = service.Snapshot;

            Assert.That(outcome, Is.Not.EqualTo(AutoChessBattleOutcome.None));
            Assert.That(afterBattle.round, Is.EqualTo(2));
            Assert.That(afterBattle.battleLog.Length, Is.GreaterThan(0));
        }

        [Test]
        public void HotReload_UpdatesBattleSummary()
        {
            var config = BuildBasicConfig();
            var repository = new FakeConfigRepository(config);
            var hotReload = new FakeHotReloadService();
            var service = new AutoChessGameService(repository, hotReload);

            hotReload.Raise("autochess_content");
            Assert.That(service.Snapshot.battleSummary, Does.Contain("hot reloaded"));
        }

        [Test]
        public void HotReload_WhenMetadataSizesChange_ResizesSlots()
        {
            var initial = BuildBasicConfig();
            var repository = new FakeConfigRepository(initial);
            var hotReload = new FakeHotReloadService();
            var service = new AutoChessGameService(repository, hotReload);

            var updated = BuildBasicConfig();
            updated.metadata.boardSize = 6;
            updated.metadata.benchSize = 5;
            updated.metadata.shopSlots = 4;
            repository.Set(updated);

            hotReload.Raise("autochess_content");
            var snapshot = service.Snapshot;

            Assert.That(snapshot.boardSlots.Length, Is.EqualTo(6));
            Assert.That(snapshot.benchSlots.Length, Is.EqualTo(5));
            Assert.That(snapshot.shopOffers.Length, Is.EqualTo(4));
        }

        private static int FindFirstOccupiedBench(AutoChessSnapshot snapshot)
        {
            for (var i = 0; i < snapshot.benchSlots.Length; i++)
            {
                if (!snapshot.benchSlots[i].isEmpty)
                {
                    return i;
                }
            }

            return -1;
        }

        private static AutoChessContentConfig BuildBasicConfig()
        {
            return new AutoChessContentConfig
            {
                metadata = new MetadataConfig
                {
                    startingGold = 10,
                    startingHp = 30,
                    benchSize = 4,
                    boardSize = 4,
                    shopSlots = 3,
                    refreshCost = 2,
                    winGold = 5,
                    lossHpBase = 2,
                    maxBattleTurns = 60
                },
                shopRules = new ShopRulesConfig
                {
                    offerUnitIds = new[] { "soldier" }
                },
                skills = Array.Empty<SkillsItemConfig>(),
                units = new[]
                {
                    new UnitsItemConfig
                    {
                        id = "soldier",
                        name = "Soldier",
                        cost = 1,
                        maxHp = 60,
                        attack = 10,
                        manaPerAttack = 8,
                        skillId = string.Empty
                    }
                },
                waves = new[]
                {
                    new WavesItemConfig
                    {
                        round = 1,
                        enemyUnitIds = new[] { "soldier" }
                    }
                }
            };
        }

        private sealed class FakeConfigRepository : IConfigRepository<AutoChessContentConfig>
        {
            private AutoChessContentConfig _content;

            public FakeConfigRepository(AutoChessContentConfig content)
            {
                _content = content;
            }

            public string ConfigName => "autochess_content";

            public AutoChessContentConfig Get()
            {
                return _content;
            }

            public AutoChessContentConfig Reload()
            {
                return _content;
            }

            public void Set(AutoChessContentConfig content)
            {
                _content = content;
            }

            public string GetRawJson()
            {
                return "{}";
            }
        }

        private sealed class FakeHotReloadService : IConfigHotReloadService
        {
            public event Action<string> Reloaded;

            public void Start()
            {
            }

            public void Stop()
            {
            }

            public void Dispose()
            {
            }

            public void Raise(string configName)
            {
                Reloaded?.Invoke(configName);
            }
        }
    }
}
