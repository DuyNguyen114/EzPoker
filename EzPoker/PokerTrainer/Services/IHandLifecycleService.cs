using PokerTrainer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokerTrainer.Services
{
    public interface IHandLifecycleService
    {
        void StartNewHand(PokerTable table, int buttonSeat, decimal smallBlind, decimal bigBlind);
        void MoveToNextStreet(PokerTable table);
    }
}
