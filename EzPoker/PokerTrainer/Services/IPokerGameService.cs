using PokerTrainer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokerTrainer.Services
{
    public interface IPokerGameService
    {
        void Fold(PokerTable table);

        void Check(PokerTable table);

        void Call(PokerTable table);

        void Bet(
            PokerTable table,
            decimal amount);

        void RaiseTo(
            PokerTable table,
            decimal totalStreetBet);

        void AllIn(PokerTable table);

        void MoveToNextStreet(
            PokerTable table);
    }
}
