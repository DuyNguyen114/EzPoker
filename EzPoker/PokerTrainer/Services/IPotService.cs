using PokerTrainer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokerTrainer.Services
{
    public interface IPotService
    {
        decimal AwardEntirePot(PokerTable table, Guid winnerId);

        decimal AwardToLastRemainingPlayer(PokerTable table);
    }
}
