using PokerTrainer.Enum;
using PokerTrainer.Models;
using PokerTrainer.Services;
using Xunit;

namespace PokerTrainer.Core.Tests;

public class HandLifecycleServiceTests
{
    [Fact]
    public void StartNewHand_ThreePlayers_ShouldPostBlinds()
    {
        var table =
            new PokerTable("Test table");

        var button =
            new Player("Button", 1, 1000);

        var smallBlind =
            new Player("Small Blind", 2, 1000);

        var bigBlind =
            new Player("Big Blind", 3, 1000);

        table.AddPlayer(button);
        table.AddPlayer(smallBlind);
        table.AddPlayer(bigBlind);

        var bettingService =
            new BettingService();
        ITurnOrderService turnOrderService =
    new TurnOrderService();


        var lifecycleService =
            new HandLifecycleService(
                bettingService, turnOrderService);

        lifecycleService.StartNewHand(
            table,
            buttonSeat: 1,
            smallBlind: 10,
            bigBlind: 20);

        Assert.NotNull(
            table.CurrentHand);

        Assert.Equal(
            30m,
            table.CurrentPot);

        Assert.Equal(
            990m,
            smallBlind.Stack);

        Assert.Equal(
            980m,
            bigBlind.Stack);

        Assert.Equal(
            button.Id,
            table.CurrentPlayer!.Id);

        Assert.Equal(
            2,
            table.CurrentHand!.Actions.Count);
    }

    [Fact]
    public void MoveToNextStreet_ShouldResetStreetCommitted()
    {
        var table =
            new PokerTable("Test table");

        var button =
            new Player("Button", 1, 1000);

        var smallBlind =
            new Player("Small Blind", 2, 1000);

        var bigBlind =
            new Player("Big Blind", 3, 1000);

        table.AddPlayer(button);
        table.AddPlayer(smallBlind);
        table.AddPlayer(bigBlind);

        var bettingService =
            new BettingService();

        ITurnOrderService turnOrderService = new TurnOrderService();

        var lifecycleService =
            new HandLifecycleService(
                bettingService, turnOrderService);

        lifecycleService.StartNewHand(
            table,
            buttonSeat: 1,
            smallBlind: 10,
            bigBlind: 20);

        bettingService.Call(
            table,
            button);

        turnOrderService.AdvanceAfterAction(
            table,
            reopensBetting: false);

        bettingService.Call(
            table,
            smallBlind);

        turnOrderService.AdvanceAfterAction(
            table,
            reopensBetting: false);

        bettingService.Check(
            table,
            bigBlind);

        turnOrderService.AdvanceAfterAction(
            table,
            reopensBetting: false);

        lifecycleService.MoveToNextStreet(
            table);

        Assert.Equal(
            PokerStreet.Flop,
            table.CurrentHand!.CurrentStreet);

        Assert.All(
            table.Players,
            player =>
                Assert.Equal(
                    0m,
                    player.StreetCommitted));

        Assert.Equal(
            20m,
            button.TotalCommitted);

        Assert.Equal(
            20m,
            smallBlind.TotalCommitted);

        Assert.Equal(
            20m,
            bigBlind.TotalCommitted);

        Assert.Equal(
            60m,
            table.CurrentPot);

        Assert.Equal(
            smallBlind.Id,
            table.CurrentPlayer!.Id);
    }

    [Fact]
    public void StartNewHand_HeadsUp_ShouldUseButtonAsSmallBlind()
    {
        // Arrange
        var table =
            new PokerTable("Heads-up table");

        var button =
            new Player("Button", 1, 1000);

        var bigBlind =
            new Player("Big Blind", 2, 1000);

        table.AddPlayer(button);
        table.AddPlayer(bigBlind);

        IBettingService bettingService =
            new BettingService();

        ITurnOrderService turnOrderService =
    new TurnOrderService();

        var lifecycleService =
            new HandLifecycleService(
                bettingService, turnOrderService);

        // Act
        lifecycleService.StartNewHand(
            table,
            buttonSeat: 1,
            smallBlind: 10,
            bigBlind: 20);

        // Assert: Button phải đăng Small Blind
        Assert.Equal(
            990m,
            button.Stack);

        Assert.Equal(
            10m,
            button.StreetCommitted);

        // Người còn lại đăng Big Blind
        Assert.Equal(
            980m,
            bigBlind.Stack);

        Assert.Equal(
            20m,
            bigBlind.StreetCommitted);

        Assert.Equal(
            30m,
            table.CurrentPot);

        // Heads-up: Button/SB hành động trước Preflop
        Assert.Equal(
            button.Id,
            table.CurrentPlayer!.Id);
    }


    // Test khong duoc chuyen street som
    [Fact]
    public void MoveToNextStreet_WhenRoundNotComplete_ShouldThrow()
    {
        // Arrange
        var table =
            new PokerTable("Test table");

        var button =
            new Player("Button", 1, 1000);

        var smallBlind =
            new Player("Small Blind", 2, 1000);

        var bigBlind =
            new Player("Big Blind", 3, 1000);

        table.AddPlayer(button);
        table.AddPlayer(smallBlind);
        table.AddPlayer(bigBlind);

        IBettingService bettingService =
            new BettingService();

        ITurnOrderService turnOrderService =
            new TurnOrderService();

        var lifecycleService =
            new HandLifecycleService(
                bettingService,
                turnOrderService);

        lifecycleService.StartNewHand(
            table,
            buttonSeat: 1,
            smallBlind: 10,
            bigBlind: 20);

        // Chưa ai hành động

        // Act
        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    lifecycleService.MoveToNextStreet(
                        table));

        // Assert
        Assert.Contains(
            "chưa kết thúc",
            exception.Message);

        Assert.Equal(
            PokerStreet.PreFlop,
            table.CurrentHand!.CurrentStreet);
    }


    // duoc chuyen street khi vong cuoc hoan thanh
    [Fact]
    public void MoveToNextStreet_WhenRoundComplete_ShouldMoveToFlop()
    {
        // Arrange
        var table =
            new PokerTable("Test table");

        var button =
            new Player("Button", 1, 1000);

        var smallBlind =
            new Player("Small Blind", 2, 1000);

        var bigBlind =
            new Player("Big Blind", 3, 1000);

        table.AddPlayer(button);
        table.AddPlayer(smallBlind);
        table.AddPlayer(bigBlind);

        IBettingService bettingService =
            new BettingService();

        ITurnOrderService turnOrderService =
            new TurnOrderService();

        var lifecycleService =
            new HandLifecycleService(
                bettingService,
                turnOrderService);

        lifecycleService.StartNewHand(
            table,
            buttonSeat: 1,
            smallBlind: 10,
            bigBlind: 20);

        // Button call
        bettingService.Call(
            table,
            button);

        turnOrderService.AdvanceAfterAction(
            table,
            reopensBetting: false);

        // SB call thêm 10
        bettingService.Call(
            table,
            smallBlind);

        turnOrderService.AdvanceAfterAction(
            table,
            reopensBetting: false);

        // BB check
        bettingService.Check(
            table,
            bigBlind);

        turnOrderService.AdvanceAfterAction(
            table,
            reopensBetting: false);

        Assert.True(
            turnOrderService
                .IsBettingRoundComplete(table));

        // Act
        lifecycleService.MoveToNextStreet(
            table);

        // Assert
        Assert.Equal(
            PokerStreet.Flop,
            table.CurrentHand!.CurrentStreet);

        Assert.Equal(
            0m,
            table.CurrentHand.CurrentBet);

        Assert.All(
            table.Players,
            player =>
                Assert.Equal(
                    0m,
                    player.StreetCommitted));

        Assert.Equal(
            smallBlind.Id,
            table.CurrentPlayer!.Id);
    }
}