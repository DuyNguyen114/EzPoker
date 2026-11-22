using PokerTrainer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokerTrainer.Services
{
    public interface ITurnOrderService
    {
        bool IsBettingRoundComplete(PokerTable table);
        void AdvanceAfterAction(PokerTable table, bool reopensBetting);
    }
}
