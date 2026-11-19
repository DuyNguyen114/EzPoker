using PokerTrainer.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokerTrainer.Models
{
    public class PokerAction
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid PlayerId { get; init; }
        public string PlayerName { get; init; } = string.Empty;
        public PokerStreet Street { get; init; }
        public PokerActionType ActionType { get; init; }

        // Số chip thực sự bị trừ trong lần hành động này
        public decimal AmountAdded { get; init; }
        // Tổng số chip mà người chơi đã đặt trong street hiện tại
        public decimal TotalStreetBet { get; init; }
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        public PokerAction(Guid playerId, string playerName, PokerStreet street, PokerActionType actionType, decimal amountAdded, decimal totalStreetBet)
        {
            if (playerId == Guid.Empty)
            {
                throw new ArgumentException(
                    "PlayerId không hợp lệ.",
                    nameof(playerId));
            }

            if (string.IsNullOrWhiteSpace(playerName))
            {
                throw new ArgumentException(
                    "Tên người chơi không được để trống.",
                    nameof(playerName));
            }

            if (amountAdded < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amountAdded),
                    "Số chip thêm vào không được âm.");
            }

            if (totalStreetBet < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalStreetBet),
                    "Tổng cược trong vòng không được âm.");
            }

            if (amountAdded > totalStreetBet)
            {
                throw new ArgumentException(
                    "Số chip thêm vào không thể lớn hơn tổng cược trong vòng.");
            }

            PlayerId = playerId;
            PlayerName = playerName;
            Street = street;
            ActionType = actionType;
            AmountAdded = amountAdded;
            TotalStreetBet = totalStreetBet;
        }
    }
}
