using PokerTrainer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokerTrainer.Services
{
    public interface IBettingUndo
    {
        void Execute(PokerTable table, Action action);
        bool CanUndo(PokerTable table);
        bool TryUndo(PokerTable table);
        void Clear();
    }
}
