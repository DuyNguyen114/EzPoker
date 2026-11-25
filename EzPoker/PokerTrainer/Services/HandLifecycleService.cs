using PokerTrainer.Enum;
using PokerTrainer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PokerTrainer.Services
{
    public class HandLifecycleService : IHandLifecycleService
    {
        private readonly IBettingService _bettingService;
        private readonly ITurnOrderService _turnOrderService;

        public HandLifecycleService(IBettingService bettingService, ITurnOrderService turnOrderService)
        {
            _bettingService = bettingService;
            _turnOrderService = turnOrderService;
        }

        public void StartNewHand(PokerTable table, int buttonSeat, decimal smallBlind, decimal bigBlind)
        {
            ArgumentNullException.ThrowIfNull(table);

            if (table.CurrentHand is not null)
            {
                throw new InvalidOperationException("Bàn đang có một hand chưa kết thúc");
            }

            List<Player> playablePlayers = table.Players.Where(x => x.Stack > 0).OrderBy(x => x.SeatNumber).ToList();

            if (playablePlayers.Count < 2)
            {
                throw new InvalidOperationException(
                    "Cần ít nhất hai người còn chip để bắt đầu hand.");
            }

            Player? buttonPlayer = playablePlayers.FirstOrDefault(x => x.SeatNumber == buttonSeat);

            if (buttonPlayer is null)
            {
                throw new InvalidOperationException(
                    $"Không có người còn chip tại ghế Button " +
                    $"{buttonSeat}.");
            }

            ResetPlayersForNewHand(table);

            int handNumber = table.HandHistory.Count + 1;

            var hand = new PokerHand(handNumber, buttonSeat, smallBlind, bigBlind);

            table.StartHand(hand);

            int buttonIndex = FindPlayerIndexBySeat(table, buttonSeat);

            int smallBlindIndex;
            int bigBlindIndex;
            int firstPreFlopIndex;

            if (playablePlayers.Count == 2)
            {
                /*
                 * Heads-up:
                 * Button cũng là Small Blind.
                 * Button hành động đầu tiên Preflop.
                 */
                smallBlindIndex = buttonIndex;

                bigBlindIndex = FindNextPlayablePlayerIndex(table, smallBlindIndex);

                firstPreFlopIndex = buttonIndex;
            }
            else
            {
                /*
                 * Từ 3 người trở lên:
                 * SB ở bên trái Button.
                 * BB ở bên trái SB.
                 * Người bên trái BB hành động đầu tiên.
                 */
                smallBlindIndex = FindNextPlayablePlayerIndex(table, buttonIndex);

                bigBlindIndex = FindNextPlayablePlayerIndex(table, smallBlindIndex);

                firstPreFlopIndex = FindNextPlayablePlayerIndex(table, bigBlindIndex);
            }

            Player smallBlindPlayer = table.Players[smallBlindIndex];
            Player bigBlindPlayer = table.Players[bigBlindIndex];

            _bettingService.PostSmallBlind(table, smallBlindPlayer);

            _bettingService.PostBigBlind(table, bigBlindPlayer);

            if (playablePlayers.Count(p => !p.IsAllIn) >= 2 && hand.CurrentBet < bigBlind)
                hand.UpdateCurrentBet(bigBlind);

            if (!_turnOrderService.IsBettingRoundComplete(table))
            {
                Player first = table.Players[firstPreFlopIndex];
                int nextIndex = first.IsAllIn
                    ? FindNextPlayerWhoCanAct(table, firstPreFlopIndex)
                    : firstPreFlopIndex;
                if (nextIndex >= 0)
                    table.SetCurrentPlayerIndex(nextIndex);
            }
        }

        public void MoveToNextStreet(PokerTable table)
        {
            ArgumentNullException.ThrowIfNull(table);

            PokerHand hand =
                table.CurrentHand
                ?? throw new InvalidOperationException(
                    "Chưa có hand đang diễn ra.");

            int remainingPlayers =
                table.Players.Count(
                    player => !player.HasFolded);

            if (remainingPlayers <= 1)
            {
                throw new InvalidOperationException(
                    "Chỉ còn một người chơi. " +
                    "Hand phải được kết thúc thay vì chuyển street.");
            }

            if (!_turnOrderService.IsBettingRoundComplete(table))
            {
                throw new InvalidOperationException(
                    "Vòng cược hiện tại chưa kết thúc.");
            }

            PokerStreet nextStreet = GetNextStreet(hand.CurrentStreet);

            ResetStreetCommitments(table);

            hand.MoveToStreet(nextStreet);

            if (nextStreet == PokerStreet.Showdown)
            {
                table.ClearCurrentPlayer();
                return;
            }

            if (_turnOrderService.IsBettingRoundComplete(table))
            {
                table.ClearCurrentPlayer();
                return;
            }

            int buttonIndex = FindPlayerIndexBySeat(table, hand.ButtonSeat);

            int firstPostFlopIndex = FindNextPlayerWhoCanAct(table, buttonIndex);

            if (firstPostFlopIndex == -1)
            {
                /*
                * Không ai còn khả năng hành động:
                * ví dụ tất cả người còn bài đã all-in.
                */
                table.ClearCurrentPlayer();
                return;
            }

            table.SetCurrentPlayerIndex(firstPostFlopIndex);
        }

        private static void ResetPlayersForNewHand(PokerTable table)
        {
            foreach (Player player in table.Players)
            {
                player.HasFolded = player.Stack <= 0;
                player.IsAllIn = false;
                player.StreetCommitted = 0;
                player.TotalCommitted = 0;
                player.HasActedThisStreet = false;
            }
        }

        private static void ResetStreetCommitments(PokerTable table)
        {
            foreach (Player player in table.Players)
            {
                player.StreetCommitted = 0;
                player.HasActedThisStreet = false;
            }
        }

        private static PokerStreet GetNextStreet(PokerStreet currentStreet)
        {
            return currentStreet switch
            {
                PokerStreet.PreFlop =>
                    PokerStreet.Flop,

                PokerStreet.Flop =>
                    PokerStreet.Turn,

                PokerStreet.Turn =>
                    PokerStreet.River,

                PokerStreet.River =>
                    PokerStreet.Showdown,

                _ => throw new InvalidOperationException(
                    $"Không thể chuyển tiếp từ {currentStreet}.")
            };
        }

        private static int FindPlayerIndexBySeat(PokerTable table, int seatNumber)
        {
            for(int index = 0; index < table.Players.Count; index++)
            {
                if (table.Players[index].SeatNumber ==
                seatNumber)
                {
                    return index;
                }
            }

            throw new InvalidOperationException(
            $"Không tìm thấy ghế {seatNumber}.");
        }

        private static int FindNextPlayablePlayerIndex(PokerTable table, int currentIndex)
        {
            int playerCount = table.Players.Count;

            for (int offset = 1; offset <= playerCount; offset++)
            {
                int nextIndex =
                (currentIndex + offset) %
                playerCount;

                Player player = table.Players[nextIndex];

                if (player.Stack > 0) return nextIndex;
            }

            throw new InvalidOperationException(
            "Không tìm thấy người chơi tiếp theo.");
        }

        private static int FindNextPlayerWhoCanAct(PokerTable table, int currentIndex)
        {
            int playerCount =
            table.Players.Count;

            for (int offset = 1;
                 offset <= playerCount;
                 offset++)
            {
                int nextIndex =
                    (currentIndex + offset) %
                    playerCount;

                Player player =
                    table.Players[nextIndex];

                if (!player.HasFolded &&
                    !player.IsAllIn &&
                    player.Stack > 0)
                {
                    return nextIndex;
                }
            }

            return -1;
        }
    }
}
