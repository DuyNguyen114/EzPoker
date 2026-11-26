using PokerTrainer.Enum;
using PokerTrainer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokerTrainer.Services
{
    public sealed class BettingUndo : IBettingUndo
    {
        private Snapshot? _previous;
        public void Clear() => _previous = null;
        public bool CanUndo(PokerTable table)
        {
            var snapshot = _previous;

            return snapshot is not null
                && ReferenceEquals(table.CurrentHand, snapshot.Hand)
                && snapshot.Hand.CurrentStreet == snapshot.Street;
        } 
    
        public void Execute(PokerTable table, Action action)
        {
            var hand = table.CurrentHand ?? throw new InvalidOperationException("Chưa có hand");

            var snapshot = new Snapshot(hand, hand.CurrentStreet, hand.Actions.Count, hand.CurrentBet, hand.MinimumRaiseIncrement, table.CurrentPlayerIndex,
                table.Players.Select(p => new PlayerState(p.Stack, p.StreetCommitted, p.TotalCommitted, p.HasFolded, p.IsAllIn, p.HasActedThisStreet)).ToArray());

            action();

            //Fold cuoi cung co the tu trao pot va ket thuc hand
            _previous = ReferenceEquals(table.CurrentHand, hand) && hand.CurrentStreet == snapshot.Street && hand.Actions.Count > snapshot.ActionCount ? snapshot : null;
        }

        public bool TryUndo(PokerTable table)
        {
            //if (!CanUndo(table) || _previous is { } not snapshot) return false;

            var snapshot = _previous;

            if (snapshot is null || !CanUndo(table))
            {
                return false;
            }

            for(int i = 0; i < table.Players.Count; i++)
            {
                var player = table.Players[i];
                var saved = snapshot.Players[i];

                player.Stack = saved.Stack;
                player.StreetCommitted = saved.StreetCommitted;
                player.TotalCommitted = saved.TotalCommitted;
                player.HasFolded = saved.HasFolded;
                player.IsAllIn = saved.IsAllIn;
                player.HasActedThisStreet = saved.HasActedThisStreet;
            }

            snapshot.Hand.RestoreBetting(snapshot.ActionCount, snapshot.CurrentBet, snapshot.MinimumRaiseIncrement);

            table.SetCurrentPlayerIndex(snapshot.CurrentPlayerIndex);
            Clear();
            return true;
        }

        private sealed record Snapshot(PokerHand Hand, PokerStreet Street, int ActionCount, decimal CurrentBet, decimal MinimumRaiseIncrement, int CurrentPlayerIndex, PlayerState[] Players);

        private sealed record PlayerState(decimal Stack, decimal StreetCommitted, decimal TotalCommitted, bool HasFolded, bool IsAllIn, bool HasActedThisStreet);
    }
}
