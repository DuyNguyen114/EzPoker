using PokerTrainer.Enum;
using PokerTrainer.Models;
using Xunit;

namespace PokerTrainer.Core.Tests;

public class PokerHandCardsTests
{
    [Fact]
    public void BoardThroughTurn_DoesNotRevealRiver()
    {
        var hand = new PokerHand(1, 1, 100m, 200m);

        var king = new PokerCard(13, CardSuit.Hearts);
        var eight = new PokerCard(8, CardSuit.Spades);
        var three = new PokerCard(3, CardSuit.Diamonds);
        var turn = new PokerCard(4, CardSuit.Diamonds);
        var river = new PokerCard(6, CardSuit.Diamonds);
        var ace = new PokerCard(14, CardSuit.Spades);

        hand.SetHeroCards(2, ace, new PokerCard(11, CardSuit.Spades));

        hand.MoveToStreet(PokerStreet.Flop);
        hand.SetBoardCards(PokerStreet.Flop, king, eight, three);

        hand.MoveToStreet(PokerStreet.Turn);
        Assert.Throws<ArgumentException>(
            () => hand.SetBoardCards(PokerStreet.Turn, ace));
        Assert.Empty(hand.GetRevealedCards(PokerStreet.Turn));
        hand.SetBoardCards(PokerStreet.Turn, turn);

        hand.MoveToStreet(PokerStreet.River);
        hand.SetBoardCards(PokerStreet.River, river);

        Assert.Equal<int?>(2, hand.HeroSeatNumber);
        Assert.Equal(new[] { king, eight, three, turn },
            hand.GetBoardThrough(PokerStreet.Turn).ToArray());
        Assert.Equal(5, hand.GetBoardThrough(PokerStreet.River).Count);
    }
}