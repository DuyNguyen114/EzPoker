using PokerTrainer.Enum;
using PokerTrainer.Models;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;

namespace PokerTrainer.Services
{
    public class BettingService : IBettingService
    {
        public decimal GetCallAmount(PokerTable table, Player player)
        {
            PokerHand hand = GetCurrentHand(table);

            EnsurePlayerBelongsToTable(table, player);

            decimal amountTocall = hand.CurrentBet - player.StreetCommitted;

            if(amountTocall < 0)
            {
                return 0;
            }

            // Người chơi không thể call nhiều hơn số chip họ đang có
            return Math.Min(amountTocall, player.Stack);
        }

        public void PostSmallBlind(PokerTable table, Player player)
        {
            PokerHand hand = GetCurrentHand(table);

            PostBlind(table, player, hand.SmallBlind, PokerActionType.PostSmallBlind);
        }

        public void PostBigBlind(PokerTable table, Player player)
        {
            PokerHand hand = GetCurrentHand(table);
            PostBlind(table, player, hand.BigBlind, PokerActionType.PostBigBlind);
        }

        public void Fold(PokerTable table, Player player)
        {
            PokerHand hand = GetCurrentHand(table);

            EnsurePlayerCanAct(table, player);

            player.HasFolded = true;

            var action = new PokerAction(
                player.Id,
                player.Name,
                hand.CurrentStreet,
                PokerActionType.Fold,
                0,
                player.StreetCommitted);

            hand.AddAction(action);
        }

        public void Check(PokerTable table, Player player)
        {
            PokerHand hand = GetCurrentHand(table);

            EnsurePlayerCanAct(table, player);

            decimal callAmount = GetCallAmount(table, player);

            if(callAmount > 0)
            {
                throw new InvalidOperationException(
                    "Người chơi không thể check, cần call " + callAmount + " chip.");
            }

            var action = new PokerAction(
                player.Id,
                player.Name,
                hand.CurrentStreet,
                PokerActionType.Check,
                0,
                player.StreetCommitted);

            hand.AddAction(action);
        }

        public void Call(PokerTable table, Player player)
        {
            PokerHand hand = GetCurrentHand(table);

            EnsurePlayerCanAct(table, player);

            decimal callAmount = GetCallAmount(table, player);
            if (callAmount <= 0)
            {
                throw new InvalidOperationException(
                    "Người chơi không cần call.");
            }

            CommitChips(player, callAmount);

            PokerActionType actionType = player.IsAllIn ? PokerActionType.AllIn : PokerActionType.Call;


            var action = new PokerAction(
                player.Id,
                player.Name,
                hand.CurrentStreet,
                PokerActionType.Call,
                callAmount,
                player.StreetCommitted);

            hand.AddAction(action);
        }

        private void PostBlind(PokerTable table, Player player, decimal blindAmount, PokerActionType actionType)
        {
            PokerHand hand = GetCurrentHand(table);

            EnsurePlayerCanAct(table, player);

            if(hand.CurrentStreet != PokerStreet.PreFlop)
            {
                throw new InvalidOperationException(
                    "Chỉ có thể post blind trong PreFlop.");
            } 
            
            if(actionType != PokerActionType.PostSmallBlind && actionType != PokerActionType.PostBigBlind)
            {
                throw new ArgumentException(
                    "Hành động phải là PostSmallBlind hoặc PostBigBlind.",
                    nameof(actionType));
            }

            if(player.Stack < blindAmount)
            {
                throw new InvalidOperationException(
                $"{player.Name} không đủ chip để đăng blind. " +
                "All-in blind sẽ được hỗ trợ ở phiên bản sau.");
            }

            CommitChips(player, blindAmount);

            if(player.StreetCommitted > hand.CurrentBet)
            {
                hand.UpdateCurrentBet(player.StreetCommitted);
            }    

            var action = new PokerAction(
                player.Id,
                player.Name,
                hand.CurrentStreet,
                actionType,
                blindAmount,
                player.StreetCommitted);

            hand.AddAction(action);
        }

        private static void CommitChips(Player player, decimal amount)
        {
            if(amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Số chip phải lớn hơn 0.");
            }

            if(amount > player.Stack)
            {
                throw new InvalidOperationException(
                    "Người chơi không đủ chip để commit.");
            }

            player.Stack -= amount;
            player.StreetCommitted += amount;
            player.TotalCommitted += amount;

            if(player.Stack == 0)
            {
                player.IsAllIn = true;
            }
        } 

        private static PokerHand GetCurrentHand(PokerTable table)
        {
            if (table.CurrentHand is null)
            {
                throw new InvalidOperationException(
                    "Không có hand nào đang diễn ra.");
            }
            return table.CurrentHand ?? throw new InvalidOperationException("Không có hand nào đang diễn ra.");
        }

        private static void EnsurePlayerBelongsToTable(PokerTable table, Player player)
        {
            ArgumentNullException.ThrowIfNull(player, nameof(player));

            if (!table.Players.Any(
                tablePlayer =>
                    tablePlayer.Id == player.Id))
            {
                throw new InvalidOperationException(
                    "Người chơi không thuộc bàn chơi này.");
            }
        }

        private static void EnsurePlayerCanAct(PokerTable table, Player player)
        {
            EnsurePlayerBelongsToTable(table, player);

            if (player.HasFolded)
            {
                throw new InvalidOperationException(
                    "Người chơi đã fold.");
            }
            if (player.IsAllIn)
            {
                throw new InvalidOperationException(
                    "Người chơi đã all-in.");
            }
        }


        public void Bet(PokerTable table, Player player, decimal amount)
        {
            PokerHand hand = GetCurrentHand(table);

            EnsurePlayerCanAct(table, player);

            if(hand.CurrentBet > 0)
            {
                throw new InvalidOperationException(
                    "Không thể bet khi đã có cược. Vui lòng sử dụng raise.");
            }  
            
            if(amount < hand.BigBlind)
            {
                throw new InvalidOperationException(
                    $"Mức cược tối thiểu bằng Big Blind ({hand.BigBlind}).");
            }

            if(amount >= player.Stack)
            {
                throw new InvalidOperationException(
                    "Vui lòng sử dụng all-in.");
            }

            CommitChips(player, amount);

            hand.RegisterBet(player.StreetCommitted);

            var action = new PokerAction(
                player.Id,
                player.Name,
                hand.CurrentStreet,
                PokerActionType.Bet,
                amount,
                player.StreetCommitted);

            hand.AddAction(action);
        }

        public void RaiseTo(PokerTable table, Player player, decimal totalStreetBet)
        {
            PokerHand hand = GetCurrentHand(table);
            
            EnsurePlayerCanAct(table, player);

            if (hand.CurrentBet <= 0)
            {
                throw new InvalidOperationException("Chưa có ai bet. Hãy sử dụng Bet.");
            }

            if(totalStreetBet < hand.MinimumRaiseTo)
            {
                throw new InvalidOperationException(
                    $"Mức raise tối thiểu là {hand.MinimumRaiseTo}.");
            }

            decimal amountToAdd = totalStreetBet - player.StreetCommitted;

            if(amountToAdd <= 0)
            {
                throw new InvalidOperationException(
                    "Người chơi đã commit đủ số chip để đạt mức raise này.");
            }

            if(amountToAdd >= player.Stack)
            {
                throw new InvalidOperationException(
                    "Người chơi không đủ chip để raise, nếu dùng toàn bộ chip vui lòng sử dụng all-in.");
            }

            CommitChips(player, amountToAdd);

            hand.RegisterRaise(totalStreetBet);

            var action = new PokerAction(player.Id,
                player.Name,
                hand.CurrentStreet,
                PokerActionType.Raise,
                amountToAdd,
                player.StreetCommitted);

            hand.AddAction(action);
        }

        public void AllIn(PokerTable table, Player player)
        {
            PokerHand hand = GetCurrentHand(table);

            EnsurePlayerCanAct(table, player);

            decimal amountAdded = player.Stack;

            if(amountAdded <= 0)
            {
                throw new InvalidOperationException(
                    "Người chơi không còn chip để all-in.");
            }

            decimal newTotalStreetBet = player.StreetCommitted + amountAdded;

            decimal oldCurrentBet = hand.CurrentBet;

            CommitChips(player, amountAdded);

            if (oldCurrentBet == 0)
            {
                hand.RegisterBet(newTotalStreetBet);
            }
            else
            {
                hand.RegisterRaise(newTotalStreetBet);
            }
            var action = new PokerAction(
                player.Id,
                player.Name,
                hand.CurrentStreet,
                PokerActionType.AllIn,
                amountAdded,
                player.StreetCommitted);

            hand.AddAction(action);
        }
    }
}
