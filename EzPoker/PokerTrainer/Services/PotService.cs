using PokerTrainer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokerTrainer.Services
{
    public class PotService : IPotService
    {
        public decimal AwardEntirePot(PokerTable table, Guid winnerId)
        {
            ArgumentNullException.ThrowIfNull(table, nameof(table));

            PokerHand hand = table.CurrentHand ?? throw new InvalidOperationException("Không có ván bài hiện tại để trao pot.");

            Player winner = table.Players.FirstOrDefault(p => p.Id == winnerId) ?? throw new ArgumentException("Người chơi chiến thắng không tồn tại trong bàn.", nameof(winnerId));

            if (winner.HasFolded)
            {
                throw new InvalidOperationException("Người chơi chiến thắng đã fold, không thể trao pot.");
            }

            decimal pot = hand.TotalPot;

            if (pot <= 0)
            {
                throw new InvalidOperationException("Pot hiện tại bằng 0, không thể trao pot.");
            }

            winner.Stack += pot;

            hand.MarkCompleted(winner.Id, winner.Name, pot);

            ResetCommittedChips(table);

            table.ClearCurrentPlayer();

            table.CompleteCurrentHand();

            return pot;
        }

        public decimal AwardToLastRemainingPlayer(PokerTable table)
        {
            ArgumentNullException.ThrowIfNull(table);

            List<Player> remainingPlayers = table.Players.Where(p => !p.HasFolded).ToList();

            if(remainingPlayers.Count != 1)
            {
                throw new InvalidOperationException("Phải có đúng một người chưa fold.");
            }

            return AwardEntirePot(table, remainingPlayers[0].Id);
        }

        private static void ResetCommittedChips(PokerTable table)
        {
            foreach (var player in table.Players)
            {
                player.StreetCommitted = 0;
                player.TotalCommitted = 0;
                player.HasActedThisStreet = false;
            }
        }
    }
}
