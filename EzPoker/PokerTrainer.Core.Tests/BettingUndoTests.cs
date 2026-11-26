using PokerTrainer.Models;
using PokerTrainer.Services;
using Xunit;

namespace PokerTrainer.Core.Tests;

public class BettingUndoTests
{
    [Fact]
    public void UndoCall_RestoresStackPotTurnAndActionHistory()
    {
        var (table, game, undo) = CreateTable();
        var player = table.CurrentPlayer!;
        var hand = table.CurrentHand!;

        Assert.False(undo.CanUndo(table));

        undo.Execute(table, () => game.Call(table));

        Assert.Equal(980m, player.Stack);
        Assert.Equal(50m, table.CurrentPot);
        Assert.Equal(3, hand.Actions.Count);

        Assert.True(undo.TryUndo(table));

        Assert.Equal(1000m, player.Stack);
        Assert.Equal(30m, table.CurrentPot);
        Assert.Equal(0m, player.StreetCommitted);
        Assert.Equal(0m, player.TotalCommitted);
        Assert.False(player.HasActedThisStreet);
        Assert.Same(player, table.CurrentPlayer);
        Assert.Equal(2, hand.Actions.Count); // Chỉ còn SB và BB.
        Assert.False(undo.TryUndo(table));  // Chỉ hoàn tác một bước.
    }

    [Fact]
    public void UndoRaise_RestoresCurrentBetAndMinimumRaise()
    {
        var (table, game, undo) = CreateTable();
        var player = table.CurrentPlayer!;
        var hand = table.CurrentHand!;

        undo.Execute(table, () => game.RaiseTo(table, 60m));

        Assert.Equal(60m, hand.CurrentBet);
        Assert.Equal(100m, hand.MinimumRaiseTo);
        Assert.Equal(90m, table.CurrentPot);

        Assert.True(undo.TryUndo(table));

        Assert.Equal(20m, hand.CurrentBet);
        Assert.Equal(20m, hand.MinimumRaiseIncrement);
        Assert.Equal(40m, hand.MinimumRaiseTo);
        Assert.Equal(30m, table.CurrentPot);
        Assert.Equal(1000m, player.Stack);
        Assert.Same(player, table.CurrentPlayer);
        Assert.Equal(2, hand.Actions.Count);
    }

    [Fact]
    public void UndoAfterTwoCalls_RevertsOnlyTheSecondCall()
    {
        var (table, game, undo) = CreateTable();
        var firstPlayer = table.CurrentPlayer!;

        undo.Execute(table, () => game.Call(table));
        var secondPlayer = table.CurrentPlayer!;
        undo.Execute(table, () => game.Call(table));

        Assert.Equal(70m, table.CurrentPot);
        Assert.True(undo.TryUndo(table));

        Assert.Equal(980m, firstPlayer.Stack);
        Assert.True(firstPlayer.HasActedThisStreet);
        Assert.Equal(1000m, secondPlayer.Stack);
        Assert.False(secondPlayer.HasActedThisStreet);
        Assert.Equal(50m, table.CurrentPot);
        Assert.Same(secondPlayer, table.CurrentPlayer);
        Assert.False(undo.CanUndo(table));
    }

    [Fact]
    public void UndoAfterMovingToFlop_IsNotAllowed()
    {
        var (table, game, undo) = CreateTable();

        game.Call(table); // UTG góp 20.
        game.Call(table); // BTN góp 20.
        game.Call(table); // SB góp thêm 10.
        undo.Execute(table, () => game.Check(table)); // BB kết thúc Preflop.

        Assert.True(undo.CanUndo(table));

        game.MoveToNextStreet(table);

        Assert.False(undo.TryUndo(table));
        Assert.Equal(80m, table.CurrentPot);
    }

    private static (PokerTable Table, IPokerGameService Game, IBettingUndo Undo)
        CreateTable()
    {
        var table = new PokerTable("Undo tests");
        table.AddPlayer(new Player("BTN", 1, 1000m));
        table.AddPlayer(new Player("SB", 2, 1000m));
        table.AddPlayer(new Player("BB", 3, 1000m));
        table.AddPlayer(new Player("UTG", 4, 1000m));

        IBettingService betting = new BettingService();
        ITurnOrderService turnOrder = new TurnOrderService();
        IHandLifecycleService lifecycle =
            new HandLifecycleService(betting, turnOrder);

        IPokerGameService game =
            new PokerGameService(betting, turnOrder, lifecycle, new PotService());

        lifecycle.StartNewHand(
            table, buttonSeat: 1, smallBlind: 10m, bigBlind: 20m);

        return (table, game, new BettingUndo());
    }
}