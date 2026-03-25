using System;

namespace Tsukuyomi.Application.AutoChess
{
    public interface IAutoChessGameService
    {
        event Action Changed;

        AutoChessSnapshot Snapshot { get; }

        void StartNewRun();

        bool RefreshShop();

        bool BuyShopOffer(int shopIndex);

        bool MoveBenchToBoard(int benchIndex, int boardIndex);

        bool MoveBoardToBench(int boardIndex);

        bool SellBenchUnit(int benchIndex);

        AutoChessBattleOutcome StartBattle();
    }
}
