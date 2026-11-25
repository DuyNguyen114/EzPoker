using PokerTrainer.Enum;
using PokerTrainer.Models;
using PokerTrainer.Services;

namespace PokerTrainer.Core.Tests;

public class SidePotTests
{
    [Fact]
    public void Showdown_SplitsMainPot_AwardsSidePot_AndRefundsUncalledChips()
    {
        var table = AtShowdown(100, 250, 400, 100);
        var a = table.Players[0];
        var b = table.Players[1];
        var c = table.Players[2];
        var folded = table.Players[3];
        folded.HasFolded = true;
        var service = new PotService();
        var preview = service.Preview(table);

        Assert.Equal(new[] { 400m, 300m }, preview.Pots.Select(p => p.Amount));
        Assert.DoesNotContain(folded.Id, preview.Pots[0].EligibleIds);
        Assert.DoesNotContain(a.Id, preview.Pots[1].EligibleIds);
        Assert.Equal(150m, Assert.Single(preview.Refunds).Amount);

        var originalStacks = table.Players.Select(p => p.Stack).ToArray();
        Assert.Throws<ArgumentException>(() => service.SettleShowdown(table,
            new Dictionary<int, Guid[]> { [1] = [a.Id], [2] = [a.Id] }));
        Assert.Equal(originalStacks, table.Players.Select(p => p.Stack));
        Assert.Empty(table.HandHistory);

        var payments = service.SettleShowdown(table,
            new Dictionary<int, Guid[]> { [1] = [a.Id, b.Id], [2] = [c.Id] });
        Assert.Equal(850m, payments.Sum(p => p.Amount));
        Assert.Equal(1100m, a.Stack);
        Assert.Equal(950m, b.Stack);
        Assert.Equal(1050m, c.Stack);
        Assert.Equal(900m, folded.Stack);
        Assert.Equal(4000m, table.TotalChipsOnTable);
        Assert.Null(table.CurrentHand);
        Assert.Equal(700m, Assert.Single(table.HandHistory).AwardedPot);
    }

    [Fact]
    public void Showdown_GivesOddChipToFirstTiedSeatAfterButton()
    {
        var table = AtShowdown(5, 5, 5);
        var a = table.Players[0];
        var b = table.Players[1];
        new PotService().SettleShowdown(table,
            new Dictionary<int, Guid[]> { [1] = [a.Id, b.Id] });
        Assert.Equal(1002m, a.Stack);
        Assert.Equal(1003m, b.Stack);
    }

    [Fact]
    public void StartHeadsUp_WithShortSmallBlind_DoesNotGiveTurnToAllInPlayer()
    {
        var table = new PokerTable("Test");
        table.AddPlayer(new Player("Short SB", 1, 5m));
        table.AddPlayer(new Player("BB", 2, 100m));
        var lifecycle = new HandLifecycleService(new BettingService(), new TurnOrderService());

        lifecycle.StartNewHand(table, 1, 10m, 20m);

        Assert.True(table.Players[0].IsAllIn);
        Assert.Null(table.CurrentPlayer);
        Assert.Equal(25m, table.CurrentPot);
        var preview = new PotService().Preview(table);
        Assert.Equal(10m, Assert.Single(preview.Pots).Amount);
        Assert.Equal(15m, Assert.Single(preview.Refunds).Amount);
    }

    private static PokerTable AtShowdown(params decimal[] commitments)
    {
        var table = new PokerTable("Test");
        for (int i = 0; i < commitments.Length; i++)
            table.AddPlayer(new Player($"P{i + 1}", i + 1, 1000m - commitments[i]));
        var hand = new PokerHand(1, 1, 1m, 2m);
        table.StartHand(hand);
        for (int i = 0; i < commitments.Length; i++)
        {
            var player = table.Players[i];
            player.TotalCommitted = commitments[i];
            hand.AddAction(new PokerAction(player.Id, player.Name,
                PokerStreet.PreFlop, PokerActionType.Call, commitments[i], commitments[i]));
        }
        hand.MoveToStreet(PokerStreet.Showdown);
        return table;
    }
}
