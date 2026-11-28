using System;
using System.Collections.Generic;
using System.Text;

namespace PokerTrainer.Models
{
    public enum CardSuit
    {
        Clubs,    // ♣ Tép
        Diamonds, // ♦ Rô
        Hearts,   // ♥ Cơ
        Spades    // ♠ Bích
    }

    // 2–10 là số trên lá; J=11, Q=12, K=13, A=14.
    public readonly record struct PokerCard(int Rank, CardSuit Suit);
}
