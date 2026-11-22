using PokerTrainer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokerTrainer.Services
{
    public interface IBettingService
    {
        decimal GetCallAmount(
        PokerTable table,
        Player player);

        void PostSmallBlind(
            PokerTable table,
            Player player);

        void PostBigBlind(
            PokerTable table,
            Player player);

        void Fold(
            PokerTable table,
            Player player);

        void Check(
            PokerTable table,
            Player player);

        void Call(
            PokerTable table,
            Player player);

        void Bet(
            PokerTable table,
            Player player,
            decimal amount);

        void RaiseTo(
            PokerTable table,
            Player player,
            decimal totalStreetBet);

        void AllIn(
            PokerTable table,
            Player player);
    }
}
