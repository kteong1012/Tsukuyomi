using System;

namespace Tsukuyomi.Application.AutoChess
{
    public static class AutoChessBattlePlaybackSignals
    {
        public static event Action<int> ReplayStarted;

        public static event Action<int> ReplayCompleted;

        public static void PublishReplayStarted(int replayId)
        {
            if (replayId <= 0)
            {
                return;
            }

            ReplayStarted?.Invoke(replayId);
        }

        public static void PublishReplayCompleted(int replayId)
        {
            if (replayId <= 0)
            {
                return;
            }

            ReplayCompleted?.Invoke(replayId);
        }
    }
}
