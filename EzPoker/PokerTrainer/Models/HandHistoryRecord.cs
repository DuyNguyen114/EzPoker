using System;
using System.Collections.Generic;
using System.Text;

namespace PokerTrainer.Models
{
    public sealed record HandHistoryRecord(Guid HistoryId,
        DateTimeOffset SavedAt,
        int? HeroSeatNumber,
        PokerCard[] HeroHoleCards,
        PokerCard[] Flop,
        PokerCard[] Turn,
        PokerCard[] River,
        Dictionary<int, PokerCard[]> ShowdownCardsBySeat);
}
