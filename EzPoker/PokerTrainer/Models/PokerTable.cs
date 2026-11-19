using System;
using System.Collections.Generic;
using System.Text;
using PokerTrainer.Enum;

namespace PokerTrainer.Models
{
    public class PokerTable
    {
        private const int MininumPlayers = 2;
        private const int MaximumPlayers = 9;

        private readonly List<Player> _players = new List<Player>();
        private readonly List<PokerHand> _handHistory = new List<PokerHand>();

        public Guid Id { get; init; } = Guid.NewGuid();
        public string Name { get; }
        public IReadOnlyList<Player> Players => _players.AsReadOnly();
        public IReadOnlyList<PokerHand> HandHistory => _handHistory.AsReadOnly();
        public PokerHand? CurrentHand { get; private set; }
        public int CurrentPlayerIndex { get; private set; } = -1;
        public Player? CurrentPlayer => (CurrentPlayerIndex >= 0 && CurrentPlayerIndex < _players.Count) ? _players[CurrentPlayerIndex] : null;
        public decimal CurrentPot => CurrentHand?.TotalPot ?? 0;

        public decimal TotalChipsOnTable => _players.Sum(player => player.Stack + player.TotalCommitted);

        public PokerTable(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "Tên bàn không được để trống.",
                    nameof(name));
            }
            Name = name.Trim();
        }

        public void AddPlayer(Player player)
        {
            if (_players.Count >= MaximumPlayers)
            {
                throw new InvalidOperationException(
                    $"Không thể thêm người chơi mới. Bàn đã đạt số lượng tối đa ({MaximumPlayers}).");
            }
            if (_players.Any(p => p.SeatNumber == player.SeatNumber))
            {
                throw new InvalidOperationException(
                    $"Ghế {player.SeatNumber} đã có người chơi.");
            }

            _players.Add(player);

            _players.Sort(
            (first, second) =>
                first.SeatNumber.CompareTo(
                    second.SeatNumber));
        }

        public void RemovePlayer(int seatNumber)
        {
            EnsureHandIsNotRunning();

            var playerToRemove = _players.FirstOrDefault(p => p.SeatNumber == seatNumber);
            if (playerToRemove == null)
            {
                throw new InvalidOperationException(
                    $"Không tìm thấy người chơi ở ghế {seatNumber}.");
            }
            _players.Remove(playerToRemove);
        }

        public void StartHand(PokerHand hand)
        {
            ArgumentNullException.ThrowIfNull(hand, nameof(hand));

            if (_players.Count < MininumPlayers)
            {
                throw new InvalidOperationException(
                    $"Không đủ người chơi để bắt đầu ván bài. Cần ít nhất {MininumPlayers} người chơi.");
            }

            if (CurrentHand is not null)
            {
                throw new InvalidOperationException(
                    "Đang có một hand chưa kết thúc.");
            }

            bool buttonSeatExists = _players.Any(p => p.SeatNumber == hand.ButtonSeat);

            if(!buttonSeatExists)
            {
                throw new InvalidOperationException(
                    $"Không tìm thấy người chơi ở ghế button {hand.ButtonSeat}.");
            }

            CurrentHand = hand;
            CurrentPlayerIndex = -1;
        }

        public void SetCurrentPlayer(int index)
        {
            if (CurrentHand is null)
            {
                throw new InvalidOperationException(
                    "Không có hand nào đang diễn ra.");
            }
            
            if(index < 0 || index >= _players.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    $"Vị trí {index} không hợp lệ.");
            }

            Player player = _players[index];

            if (player.HasFolded)
            {
                throw new InvalidOperationException("Người chơi đã fold");
            }

            if(player.IsAllIn)
            {
                throw new InvalidOperationException("Người chơi đã all-in");
            }

            CurrentPlayerIndex = index;
        }

        public void CompleteCurrentHand()
        {
            if (CurrentHand is null)
            {
                throw new InvalidOperationException(
                    "Không có hand nào đang diễn ra.");
            }

            if (CurrentHand.CurrentStreet != PokerStreet.Completed)
            {
                throw new InvalidOperationException(
                    "Không thể kết thúc hand chưa ở trạng thái Completed.");
            }

            _handHistory.Add(CurrentHand);

            CurrentHand = null;
            CurrentPlayerIndex = -1;
        }

        private void EnsureHandIsNotRunning()
        {
            if (CurrentHand is not null)
            {
                throw new InvalidOperationException(
                    "Không thể thực hiện thao tác này khi đang có hand diễn ra.");
            }
        }
    }
}
