using System.Collections.Generic;
using Tsukuyomi.Application.AutoChess;
using Tsukuyomi.Application.Localization;
using Tsukuyomi.Application.UI;
using Tsukuyomi.Domain.UI;
using UnityEngine.UIElements;

namespace Tsukuyomi.Presentation.UI
{
    public sealed class AutoChessViewBinder : IUiViewBinder
    {
        private readonly IAutoChessGameService _gameService;
        private readonly ILocalizationService _localizationService;

        private IUiNavigator _navigator;
        private Label _shopTitleLabel;
        private Label _boardTitleLabel;
        private Label _benchTitleLabel;
        private Label _actionsTitleLabel;
        private Label _battleLogTitleLabel;
        private Label _roundLabel;
        private Label _goldLabel;
        private Label _hpLabel;
        private Label _battleSummaryLabel;
        private Label _selectionLabel;
        private Label _battleLogLabel;
        private VisualElement _shopContainer;
        private VisualElement _boardContainer;
        private VisualElement _benchContainer;
        private Button _refreshShopButton;
        private Button _startBattleButton;
        private Button _newRunButton;
        private Button _sellSelectedButton;
        private Button _backButton;

        private readonly List<Button> _shopButtons = new();
        private readonly List<Button> _boardButtons = new();
        private readonly List<Button> _benchButtons = new();

        private int _selectedBenchIndex = -1;

        public AutoChessViewBinder(
            IAutoChessGameService gameService,
            ILocalizationService localizationService)
        {
            _gameService = gameService;
            _localizationService = localizationService;
        }

        public ScreenId ScreenId => ScreenId.AutoChess;

        public void Bind(IUiElementQuery query, IUiNavigator navigator)
        {
            _navigator = navigator;
            _shopTitleLabel = query.Q<Label>("shop-title-label");
            _boardTitleLabel = query.Q<Label>("board-title-label");
            _benchTitleLabel = query.Q<Label>("bench-title-label");
            _actionsTitleLabel = query.Q<Label>("actions-title-label");
            _battleLogTitleLabel = query.Q<Label>("battle-log-title-label");
            _roundLabel = query.Q<Label>("round-label");
            _goldLabel = query.Q<Label>("gold-label");
            _hpLabel = query.Q<Label>("hp-label");
            _battleSummaryLabel = query.Q<Label>("battle-summary-label");
            _selectionLabel = query.Q<Label>("selection-label");
            _battleLogLabel = query.Q<Label>("battle-log-label");
            _shopContainer = query.Q<VisualElement>("shop-container");
            _boardContainer = query.Q<VisualElement>("board-container");
            _benchContainer = query.Q<VisualElement>("bench-container");
            _refreshShopButton = query.Q<Button>("refresh-shop-btn");
            _startBattleButton = query.Q<Button>("start-battle-btn");
            _newRunButton = query.Q<Button>("new-run-btn");
            _sellSelectedButton = query.Q<Button>("sell-selected-btn");
            _backButton = query.Q<Button>("back-btn");

            _gameService.Changed += OnGameChanged;
            _localizationService.Changed += OnLocalizationChanged;

            if (_refreshShopButton != null)
            {
                _refreshShopButton.clicked += OnRefreshShop;
            }

            if (_startBattleButton != null)
            {
                _startBattleButton.clicked += OnStartBattle;
            }

            if (_newRunButton != null)
            {
                _newRunButton.clicked += OnNewRun;
            }

            if (_sellSelectedButton != null)
            {
                _sellSelectedButton.clicked += OnSellSelected;
            }

            if (_backButton != null)
            {
                _backButton.clicked += OnBack;
            }

            BuildDynamicButtons();
            Refresh();
        }

        public void Refresh()
        {
            var snapshot = _gameService.Snapshot;
            if (snapshot == null)
            {
                return;
            }

            EnsureDynamicButtonCounts(snapshot);
            if (_selectedBenchIndex >= snapshot.benchSlots.Length)
            {
                _selectedBenchIndex = -1;
            }

            if (_roundLabel != null)
            {
                var prefix = _localizationService.GetText("ui.autochess.roundPrefix");
                _roundLabel.text = $"{prefix}: {snapshot.round}";
            }

            if (_goldLabel != null)
            {
                var prefix = _localizationService.GetText("ui.autochess.goldPrefix");
                _goldLabel.text = $"{prefix}: {snapshot.gold}";
            }

            if (_hpLabel != null)
            {
                var prefix = _localizationService.GetText("ui.autochess.hpPrefix");
                _hpLabel.text = $"{prefix}: {snapshot.hp}";
            }

            if (_battleSummaryLabel != null)
            {
                _battleSummaryLabel.text = snapshot.battleSummary;
            }

            if (_selectionLabel != null)
            {
                _selectionLabel.text = _selectedBenchIndex >= 0
                    ? _localizationService.Format("ui.autochess.selection.selected", _selectedBenchIndex + 1)
                    : _localizationService.GetText("ui.autochess.selection.none");
            }

            RefreshStaticText();

            RefreshShopButtons(snapshot);
            RefreshBoardButtons(snapshot);
            RefreshBenchButtons(snapshot);
            RefreshBattleLog(snapshot);

            if (_sellSelectedButton != null)
            {
                _sellSelectedButton.SetEnabled(
                    _selectedBenchIndex >= 0 &&
                    _selectedBenchIndex < snapshot.benchSlots.Length &&
                    !snapshot.benchSlots[_selectedBenchIndex].isEmpty);
            }

            if (_startBattleButton != null)
            {
                var hasBoardUnits = false;
                for (var i = 0; i < snapshot.boardSlots.Length; i++)
                {
                    if (!snapshot.boardSlots[i].isEmpty)
                    {
                        hasBoardUnits = true;
                        break;
                    }
                }

                _startBattleButton.SetEnabled(snapshot.hp > 0 && hasBoardUnits);
            }
        }

        public void Unbind()
        {
            _gameService.Changed -= OnGameChanged;
            _localizationService.Changed -= OnLocalizationChanged;

            if (_refreshShopButton != null)
            {
                _refreshShopButton.clicked -= OnRefreshShop;
            }

            if (_startBattleButton != null)
            {
                _startBattleButton.clicked -= OnStartBattle;
            }

            if (_newRunButton != null)
            {
                _newRunButton.clicked -= OnNewRun;
            }

            if (_sellSelectedButton != null)
            {
                _sellSelectedButton.clicked -= OnSellSelected;
            }

            if (_backButton != null)
            {
                _backButton.clicked -= OnBack;
            }
        }

        public void Dispose()
        {
            Unbind();
        }

        private void BuildDynamicButtons()
        {
            var snapshot = _gameService.Snapshot;
            if (snapshot == null)
            {
                return;
            }

            _shopButtons.Clear();
            _boardButtons.Clear();
            _benchButtons.Clear();

            _shopContainer?.Clear();
            _boardContainer?.Clear();
            _benchContainer?.Clear();

            for (var i = 0; i < snapshot.shopOffers.Length; i++)
            {
                var slotIndex = i;
                var button = new Button(() => OnBuy(slotIndex))
                {
                    name = $"shop-slot-{slotIndex}",
                    text = $"{_localizationService.GetText("ui.autochess.offerPrefix")} {slotIndex + 1}"
                };
                button.AddToClassList("autochess-shop-button");
                _shopButtons.Add(button);
                _shopContainer?.Add(button);
            }

            for (var i = 0; i < snapshot.boardSlots.Length; i++)
            {
                var slotIndex = i;
                var button = new Button(() => OnBoardSlot(slotIndex))
                {
                    name = $"board-slot-{slotIndex}",
                    text = $"{_localizationService.GetText("ui.autochess.boardSlotPrefix")} {slotIndex + 1}"
                };
                button.AddToClassList("autochess-board-button");
                _boardButtons.Add(button);
                _boardContainer?.Add(button);
            }

            for (var i = 0; i < snapshot.benchSlots.Length; i++)
            {
                var slotIndex = i;
                var button = new Button(() => OnBenchSlot(slotIndex))
                {
                    name = $"bench-slot-{slotIndex}",
                    text = $"{_localizationService.GetText("ui.autochess.benchSlotPrefix")} {slotIndex + 1}"
                };
                button.AddToClassList("autochess-bench-button");
                _benchButtons.Add(button);
                _benchContainer?.Add(button);
            }
        }

        private void EnsureDynamicButtonCounts(AutoChessSnapshot snapshot)
        {
            if (_shopButtons.Count != snapshot.shopOffers.Length ||
                _boardButtons.Count != snapshot.boardSlots.Length ||
                _benchButtons.Count != snapshot.benchSlots.Length)
            {
                BuildDynamicButtons();
            }
        }

        private void RefreshShopButtons(AutoChessSnapshot snapshot)
        {
            var count = snapshot.shopOffers.Length < _shopButtons.Count ? snapshot.shopOffers.Length : _shopButtons.Count;
            for (var i = 0; i < count; i++)
            {
                var offer = snapshot.shopOffers[i];
                var button = _shopButtons[i];
                if (offer.isEmpty)
                {
                    button.text = $"{_localizationService.GetText("ui.autochess.offerPrefix")} {i + 1}\n{_localizationService.GetText("ui.autochess.offerEmpty")}";
                    button.SetEnabled(false);
                    continue;
                }

                button.text = $"{offer.displayName}\n{_localizationService.GetText("ui.autochess.costPrefix")}: {offer.cost}";
                button.SetEnabled(offer.canAfford);
            }
        }

        private void RefreshBoardButtons(AutoChessSnapshot snapshot)
        {
            var count = snapshot.boardSlots.Length < _boardButtons.Count ? snapshot.boardSlots.Length : _boardButtons.Count;
            for (var i = 0; i < count; i++)
            {
                var slot = snapshot.boardSlots[i];
                var button = _boardButtons[i];
                if (slot.isEmpty || slot.unit == null)
                {
                    button.text = $"{_localizationService.GetText("ui.autochess.boardSlotPrefix")} {i + 1}\n({_localizationService.GetText("ui.common.empty")})";
                }
                else
                {
                    button.text =
                        $"{slot.unit.displayName}\n{_localizationService.GetText("ui.autochess.atkShortPrefix")} {slot.unit.attack} " +
                        $"{_localizationService.GetText("ui.autochess.hpShortPrefix")} {slot.unit.maxHp}";
                }
            }
        }

        private void RefreshBenchButtons(AutoChessSnapshot snapshot)
        {
            var count = snapshot.benchSlots.Length < _benchButtons.Count ? snapshot.benchSlots.Length : _benchButtons.Count;
            for (var i = 0; i < count; i++)
            {
                var slot = snapshot.benchSlots[i];
                var button = _benchButtons[i];
                if (slot.isEmpty || slot.unit == null)
                {
                    button.text = $"{_localizationService.GetText("ui.autochess.benchSlotPrefix")} {i + 1}\n({_localizationService.GetText("ui.common.empty")})";
                }
                else
                {
                    var skillText = string.IsNullOrWhiteSpace(slot.unit.skillName)
                        ? _localizationService.GetText("ui.autochess.noSkill")
                        : slot.unit.skillName;
                    button.text = $"{slot.unit.displayName}\n{_localizationService.GetText("ui.autochess.costPrefix")} {slot.unit.cost} | {skillText}";
                }

                if (_selectedBenchIndex == i)
                {
                    button.AddToClassList("selected");
                }
                else
                {
                    button.RemoveFromClassList("selected");
                }
            }
        }

        private void RefreshBattleLog(AutoChessSnapshot snapshot)
        {
            if (_battleLogLabel == null)
            {
                return;
            }

            if (snapshot.battleLog == null || snapshot.battleLog.Length == 0)
            {
                _battleLogLabel.text = _localizationService.GetText("ui.autochess.battleLog.empty");
                return;
            }

            _battleLogLabel.text = string.Join("\n", snapshot.battleLog);
        }

        private void OnBuy(int shopIndex)
        {
            _gameService.BuyShopOffer(shopIndex);
        }

        private void OnBoardSlot(int boardIndex)
        {
            if (_selectedBenchIndex >= 0)
            {
                if (_gameService.MoveBenchToBoard(_selectedBenchIndex, boardIndex))
                {
                    _selectedBenchIndex = -1;
                    Refresh();
                }

                return;
            }

            _gameService.MoveBoardToBench(boardIndex);
        }

        private void OnBenchSlot(int benchIndex)
        {
            var snapshot = _gameService.Snapshot;
            if (snapshot == null || benchIndex < 0 || benchIndex >= snapshot.benchSlots.Length)
            {
                return;
            }

            if (snapshot.benchSlots[benchIndex].isEmpty)
            {
                _selectedBenchIndex = -1;
            }
            else if (_selectedBenchIndex == benchIndex)
            {
                _selectedBenchIndex = -1;
            }
            else
            {
                _selectedBenchIndex = benchIndex;
            }

            Refresh();
        }

        private void OnRefreshShop()
        {
            _gameService.RefreshShop();
        }

        private void OnStartBattle()
        {
            _selectedBenchIndex = -1;
            _gameService.StartBattle();
        }

        private void OnSellSelected()
        {
            if (_selectedBenchIndex < 0)
            {
                return;
            }

            if (_gameService.SellBenchUnit(_selectedBenchIndex))
            {
                _selectedBenchIndex = -1;
                Refresh();
            }
        }

        private void OnNewRun()
        {
            _selectedBenchIndex = -1;
            _gameService.StartNewRun();
        }

        private void OnBack()
        {
            _selectedBenchIndex = -1;
            _navigator.Replace(ScreenId.MainMenu);
        }

        private void OnGameChanged()
        {
            Refresh();
        }

        private void OnLocalizationChanged()
        {
            BuildDynamicButtons();
            Refresh();
        }

        private void RefreshStaticText()
        {
            if (_shopTitleLabel != null)
            {
                _shopTitleLabel.text = _localizationService.GetText("ui.autochess.shop");
            }

            if (_boardTitleLabel != null)
            {
                _boardTitleLabel.text = _localizationService.GetText("ui.autochess.board");
            }

            if (_benchTitleLabel != null)
            {
                _benchTitleLabel.text = _localizationService.GetText("ui.autochess.bench");
            }

            if (_actionsTitleLabel != null)
            {
                _actionsTitleLabel.text = _localizationService.GetText("ui.autochess.actions");
            }

            if (_battleLogTitleLabel != null)
            {
                _battleLogTitleLabel.text = _localizationService.GetText("ui.autochess.battleLog");
            }

            if (_refreshShopButton != null)
            {
                _refreshShopButton.text = _localizationService.GetText("ui.autochess.refreshShop");
            }

            if (_startBattleButton != null)
            {
                _startBattleButton.text = _localizationService.GetText("ui.autochess.startBattle");
            }

            if (_sellSelectedButton != null)
            {
                _sellSelectedButton.text = _localizationService.GetText("ui.autochess.sellSelected");
            }

            if (_newRunButton != null)
            {
                _newRunButton.text = _localizationService.GetText("ui.autochess.newRun");
            }

            if (_backButton != null)
            {
                _backButton.text = _localizationService.GetText("ui.autochess.backMenu");
            }
        }
    }
}
