using PokerTrainer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokerTrainer.Services
{
    public class PokerGameService : IPokerGameService
    {
        private readonly IBettingService _bettingService;
        private readonly ITurnOrderService _turnOrderService;
        private readonly IHandLifecycleService _handLifecycleService;
        private readonly IPotService _potService;

        public PokerGameService(
            IBettingService bettingService,
            ITurnOrderService turnOrderService,
            IHandLifecycleService handLifecycleService,
            IPotService potService)
        {
            _bettingService =
                bettingService
                ?? throw new ArgumentNullException(
                    nameof(bettingService));

            _turnOrderService =
                turnOrderService
                ?? throw new ArgumentNullException(
                    nameof(turnOrderService));

            _handLifecycleService =
                handLifecycleService
                ?? throw new ArgumentNullException(
                    nameof(handLifecycleService));
            _potService = potService
                ?? throw new ArgumentNullException(
                    nameof(potService));
        }

        public void Fold(PokerTable table)
        {
            Player player = GetCurrentPlayer(table);

            _bettingService.Fold(table, player);

            _turnOrderService.AdvanceAfterAction(table, false);

            int remainingPlayers = table.Players.Count(p => !p.HasFolded);

            if(remainingPlayers == 1)
            {
                _potService.AwardToLastRemainingPlayer(table);
            }
        }

        public void Check(PokerTable table)
        {
            Player player = GetCurrentPlayer(table);

            _bettingService.Check(table, player);

            _turnOrderService.AdvanceAfterAction(table, false);
        }

        public void Call(PokerTable table)
        {
            Player player = GetCurrentPlayer(table);

            _bettingService.Call(table, player);

            _turnOrderService.AdvanceAfterAction(table, false);
        }

        public void Bet(PokerTable table, decimal amount)
        {
            Player player = GetCurrentPlayer(table);

            _bettingService.Bet(table, player, amount);

            /*
             * Một opening bet khiến những người đã check
             * trước đó phải hành động lại.
             */
            _turnOrderService.AdvanceAfterAction(
                table,
                reopensBetting: true);
        }

        public void RaiseTo(PokerTable table, decimal totalStreetBet)
        {
            Player player = GetCurrentPlayer(table);

            /*
             * RaiseTo của BettingService chỉ chấp nhận
             * full raise hợp lệ, vì vậy action được mở lại.
             */
                _bettingService.RaiseTo(
                    table,
                    player,
                    totalStreetBet);

                _turnOrderService.AdvanceAfterAction(
                    table,
                    reopensBetting: true);
        }

        private static Player GetCurrentPlayer(PokerTable table)
        {
            ArgumentNullException.ThrowIfNull(table);

            if (table.CurrentHand is null)
            {
                throw new InvalidOperationException(
                    "Chưa có hand đang diễn ra.");
            }

            return table.CurrentPlayer
                ?? throw new InvalidOperationException(
                    "Không có người đang đến lượt.");
        }

        public void AllIn(PokerTable table)
        {
            Player player =
                GetCurrentPlayer(table);

            PokerHand hand =
                GetCurrentHand(table);

            /*
             * Phải lưu trạng thái trước khi AllIn,
             * vì sau hành động CurrentBet có thể thay đổi.
             */
            decimal oldCurrentBet =
                hand.CurrentBet;

            decimal minimumRaiseIncrement =
                hand.MinimumRaiseIncrement;

            decimal allInTotal =
                player.StreetCommitted +
                player.Stack;

            bool isOpeningBet =
                oldCurrentBet == 0 &&
                allInTotal > 0;

            bool isFullRaise =
                allInTotal > oldCurrentBet &&
                allInTotal - oldCurrentBet >=
                minimumRaiseIncrement;

            bool reopensBetting =
                isOpeningBet ||
                isFullRaise;

            _bettingService.AllIn(
                table,
                player);

            _turnOrderService.AdvanceAfterAction(
                table,
                reopensBetting);
        }

        public void MoveToNextStreet(
        PokerTable table)
        {
            _handLifecycleService
                .MoveToNextStreet(table);
        }

        private static PokerHand GetCurrentHand(PokerTable table)
        {
            ArgumentNullException.ThrowIfNull(table);

            return table.CurrentHand
            ?? throw new InvalidOperationException(
                "Chưa có hand đang diễn ra.");
        }
        public decimal AwardPot(PokerTable table, Guid winnerId)
        {
            return _potService.AwardEntirePot(
                table,
                winnerId);
        }

    }
}
