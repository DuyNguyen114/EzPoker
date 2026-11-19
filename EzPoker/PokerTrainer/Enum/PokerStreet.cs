using System;
using System.Collections.Generic;
using System.Text;

namespace PokerTrainer.Enum
{
    public enum PokerStreet
    {
        PreFlop,
        Flop,
        Turn,
        River,
        Showdown,
        Completed
    }

    public enum PokerActionType
    {
        PostSmallBlind,
        PostBigBlind,
        Fold,
        Check,
        Call,
        Bet,
        Raise,
        AllIn
    }
}
