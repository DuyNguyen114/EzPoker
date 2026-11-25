using PokerTrainer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokerTrainer.Services
{
    public class TurnOrderService : ITurnOrderService
    {
        public bool IsBettingRoundComplete(PokerTable table)
        {
            ArgumentNullException.ThrowIfNull(table, nameof(table));

            PokerHand hand = table.CurrentHand ?? throw new InvalidOperationException("Chưa có hand diễn ra.");

            List<Player> remainingPlayers =
            table.Players
                .Where(player => !player.HasFolded)
                .ToList();

            // Chỉ còn một người chưa fold
            if (remainingPlayers.Count <= 1)
            {
                return true;
            }

            var ableToAct = remainingPlayers.Where(CanPlayerAct).ToArray();
            if (ableToAct.Length <= 1)
                return ableToAct.Length == 0 ||
                    ableToAct[0].StreetCommitted >= hand.CurrentBet;

            foreach (Player player in remainingPlayers)
            {
                // Người all-in không cần hành động thêm
                if (player.IsAllIn ||
                    player.Stack <= 0)
                {
                    continue;
                }

                // Vẫn còn người chưa hành động
                if (!player.HasActedThisStreet)
                {
                    return false;
                }

                // Vẫn còn người chưa theo đủ mức cược
                if (player.StreetCommitted !=
                    hand.CurrentBet)
                {
                    return false;
                }
            }

            return true;
        }

        public void AdvanceAfterAction(PokerTable table, bool reopensBetting)
        {
            ArgumentNullException.ThrowIfNull(table);

            PokerHand hand =
                table.CurrentHand
                ?? throw new InvalidOperationException(
                    "Chưa có hand đang diễn ra.");

            Player currentPlayer =
                table.CurrentPlayer
                ?? throw new InvalidOperationException(
                    "Không có người đang đến lượt.");

            int currentIndex = table.CurrentPlayerIndex;

            /*
             * Khi có bet hoặc full raise,
             * những người còn lại phải phản hồi lại.
             */
            if (reopensBetting)
            {
                ResetOtherPlayersAfterRaise(
                    table,
                    currentPlayer.Id);
            }

            currentPlayer.HasActedThisStreet = true;

            if (IsBettingRoundComplete(table))
            {
                table.ClearCurrentPlayer();
                return;
            }

            int nextIndex = FindNextPlayerNeedingAction(table, currentIndex, hand);

            if(nextIndex == -1)
            {
                throw new InvalidOperationException(
                "Không tìm thấy người chơi tiếp theo " +
                "nhưng vòng cược chưa kết thúc.");
            }

            table.SetCurrentPlayerIndex(nextIndex);
        }

        private static void ResetOtherPlayersAfterRaise(PokerTable table, Guid raiseId)
        {
            foreach (Player player in table.Players)
            {
                if(player.Id == raiseId)
                {
                    continue;
                }

                if(player.HasFolded || player.IsAllIn || player.Stack <= 0)
                {
                    continue;
                }

                player.HasActedThisStreet = false;
            }
        }

        private static int FindNextPlayerNeedingAction(PokerTable table, int currentIndex, PokerHand hand)
        {
            int playerCount = table.Players.Count;

            for (int offset = 1; offset <= playerCount; offset++) 
            {
                int nextIndex = (currentIndex + offset) % playerCount;

                Player player = table.Players[nextIndex];

                if(!CanPlayerAct(player))
                {
                    continue;
                }

                bool hasNotActed = !player.HasActedThisStreet;

                bool hasNotMatchedBet = player.StreetCommitted < hand.CurrentBet;

                if(hasNotActed ||  hasNotMatchedBet)
                {
                    return nextIndex;
                }
            }

            return -1;
        }
        private static bool CanPlayerAct(
        Player player)
        {
            return
                !player.HasFolded &&
                !player.IsAllIn &&
                player.Stack > 0;
        }

    }
}
