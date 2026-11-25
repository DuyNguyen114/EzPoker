using PokerTrainer.Enum;
using PokerTrainer.Models;
using PokerTrainer.Services;
using Xunit;

namespace PokerTrainer.Core.Tests;

public class ShowdownFlowTests
{
    [Fact]
    public void Showdown_AwardsPot_AndNextHandPreservesStacks()
    {
        var betting = new BettingService();
        var turnOrder = new TurnOrderService();
        var lifecycle = new HandLifecycleService(betting, turnOrder);
        var game = new PokerGameService(
            betting, turnOrder, lifecycle, new PotService());

        var table = new PokerTable("Test table");
        var an = new Player("An", 1, 1000m);
        var binh = new Player("Bình", 2, 1000m);
        var cuong = new Player("Cường", 3, 1000m);

        table.AddPlayer(an);
        table.AddPlayer(binh);
        table.AddPlayer(cuong);

        lifecycle.StartNewHand(table, buttonSeat: 1, smallBlind: 10m, bigBlind: 20m);

        // Preflop: BTN call 20, SB call thêm 10, BB check.
        game.Call(table);
        game.Call(table);
        game.Check(table);

        // Flop, Turn, River: cả ba check.
        for (int street = 0; street < 3; street++)
        {
            game.MoveToNextStreet(table);
            game.Check(table);
            game.Check(table);
            game.Check(table);
        }

        game.MoveToNextStreet(table);

        Assert.Equal(PokerStreet.Showdown, table.CurrentHand!.CurrentStreet);
        Assert.Equal(60m, table.CurrentPot);
        Assert.Equal(980m, an.Stack);

        decimal awarded = game.AwardPot(table, an.Id);

        Assert.Equal(60m, awarded);
        Assert.Equal(1040m, an.Stack);
        Assert.Null(table.CurrentHand);
        Assert.Equal(0m, table.CurrentPot);

        var completed = Assert.Single(table.HandHistory);
        Assert.Equal(an.Id, completed.WinnerId);
        Assert.Equal(60m, completed.AwardedPot);

        int nextButton = NextButtonSeatSelector.Select(table, completed.ButtonSeat);
        lifecycle.StartNewHand(table, nextButton, completed.SmallBlind, completed.BigBlind);

        Assert.Equal(2, table.CurrentHand!.HandNumber);
        Assert.Equal(2, table.CurrentHand.ButtonSeat);
        Assert.Equal(30m, table.CurrentPot);
        Assert.Equal(1020m, an.Stack);   // BB: 1.040 - 20
        Assert.Equal(980m, binh.Stack);  // BTN: không đóng blind
        Assert.Equal(970m, cuong.Stack); // SB: 980 - 10
    }

    [Theory]
    [InlineData(1, 3)] // Bỏ qua ghế 2 đã hết chip
    [InlineData(3, 1)] // Hết vòng thì quay về ghế 1
    public void Select_SkipsEmptyStacks_AndWrapsAround(
        int previousButton, int expectedButton)
    {
        var table = new PokerTable("Test table");
        table.AddPlayer(new Player("An", 1, 1000m));
        table.AddPlayer(new Player("Bình", 2, 0m));
        table.AddPlayer(new Player("Cường", 3, 1000m));

        Assert.Equal(expectedButton,
            NextButtonSeatSelector.Select(table, previousButton));
    }
}