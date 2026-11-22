using PokerTrainer.Models;
using PokerTrainer.Services;
using Xunit;

namespace PokerTrainer.Core.Tests;

public class TurnOrderServiceTests
{
    private static (
        PokerTable Table,
        Player Player1,
        Player Player2,
        Player Button,
        Player SmallBlind,
        Player BigBlind,
        IBettingService BettingService,
        ITurnOrderService TurnOrderService)
        CreateFivePlayerTable()
    {
        var table =
            new PokerTable("Five-player table");

        var player1 =
            new Player("Player 1", 1, 1000);

        var player2 =
            new Player("Player 2", 2, 1000);

        var button =
            new Player("Button", 3, 1000);

        var smallBlind =
            new Player("Small Blind", 4, 1000);

        var bigBlind =
            new Player("Big Blind", 5, 1000);

        table.AddPlayer(player1);
        table.AddPlayer(player2);
        table.AddPlayer(button);
        table.AddPlayer(smallBlind);
        table.AddPlayer(bigBlind);

        IBettingService bettingService =
            new BettingService();

        var lifecycleService =
            new HandLifecycleService(
                bettingService);

        ITurnOrderService turnOrderService =
            new TurnOrderService();

        lifecycleService.StartNewHand(
            table,
            buttonSeat: 3,
            smallBlind: 10,
            bigBlind: 20);

        return (
            table,
            player1,
            player2,
            button,
            smallBlind,
            bigBlind,
            bettingService,
            turnOrderService);
    }

    [Fact]
    public void StartNewHand_FivePlayers_ShouldStartLeftOfBigBlind()
    {
        // Arrange
        var setup =
            CreateFivePlayerTable();

        // Act
        Player? currentPlayer =
            setup.Table.CurrentPlayer;

        // Assert
        Assert.NotNull(currentPlayer);

        Assert.Equal(
            setup.Player1.Id,
            currentPlayer.Id);

        Assert.Equal(
            30m,
            setup.Table.CurrentPot);
    }

    [Fact]
    public void AdvanceAfterCall_ShouldMoveToNextPlayer()
    {
        // Arrange
        var setup =
            CreateFivePlayerTable();

        Assert.Equal(
            setup.Player1.Id,
            setup.Table.CurrentPlayer!.Id);

        // Act: Player 1 call 20
        setup.BettingService.Call(
            setup.Table,
            setup.Player1);

        setup.TurnOrderService.AdvanceAfterAction(
            setup.Table,
            reopensBetting: false);

        // Assert
        Assert.True(
            setup.Player1.HasActedThisStreet);

        Assert.Equal(
            20m,
            setup.Player1.StreetCommitted);

        Assert.Equal(
            setup.Player2.Id,
            setup.Table.CurrentPlayer!.Id);
    }

    [Fact]
    public void AdvanceAfterFold_ShouldSkipFoldedPlayers()
    {
        // Arrange
        var setup =
            CreateFivePlayerTable();

        // Player 1 fold
        setup.BettingService.Fold(
            setup.Table,
            setup.Player1);

        setup.TurnOrderService.AdvanceAfterAction(
            setup.Table,
            reopensBetting: false);

        Assert.Equal(
            setup.Player2.Id,
            setup.Table.CurrentPlayer!.Id);

        // Act: Player 2 cũng fold
        setup.BettingService.Fold(
            setup.Table,
            setup.Player2);

        setup.TurnOrderService.AdvanceAfterAction(
            setup.Table,
            reopensBetting: false);

        // Assert: lượt chuyển sang Button, không quay lại người đã fold
        Assert.True(
            setup.Player1.HasFolded);

        Assert.True(
            setup.Player2.HasFolded);

        Assert.Equal(
            setup.Button.Id,
            setup.Table.CurrentPlayer!.Id);
    }

    [Fact]
    public void FullRaise_ShouldReopenActionForPreviousPlayers()
    {
        // Arrange
        var setup =
            CreateFivePlayerTable();

        // Player 1 call
        setup.BettingService.Call(
            setup.Table,
            setup.Player1);

        setup.TurnOrderService.AdvanceAfterAction(
            setup.Table,
            reopensBetting: false);

        // Player 2 call
        setup.BettingService.Call(
            setup.Table,
            setup.Player2);

        setup.TurnOrderService.AdvanceAfterAction(
            setup.Table,
            reopensBetting: false);

        Assert.Equal(
            setup.Button.Id,
            setup.Table.CurrentPlayer!.Id);

        Assert.True(
            setup.Player1.HasActedThisStreet);

        Assert.True(
            setup.Player2.HasActedThisStreet);

        // Act: Button raise từ 20 lên 60
        setup.BettingService.RaiseTo(
            setup.Table,
            setup.Button,
            totalStreetBet: 60);

        setup.TurnOrderService.AdvanceAfterAction(
            setup.Table,
            reopensBetting: true);

        // Assert: cú raise mở lại action cho những người đã call
        Assert.False(
            setup.Player1.HasActedThisStreet);

        Assert.False(
            setup.Player2.HasActedThisStreet);

        Assert.True(
            setup.Button.HasActedThisStreet);

        Assert.Equal(
            60m,
            setup.Table.CurrentHand!.CurrentBet);

        Assert.Equal(
            setup.SmallBlind.Id,
            setup.Table.CurrentPlayer!.Id);
    }

    [Fact]
    public void EveryoneCallsAndBigBlindChecks_ShouldCompleteRound()
    {
        // Arrange
        var setup =
            CreateFivePlayerTable();

        // Player 1 call 20
        setup.BettingService.Call(
            setup.Table,
            setup.Player1);

        setup.TurnOrderService.AdvanceAfterAction(
            setup.Table,
            reopensBetting: false);

        // Player 2 call 20
        setup.BettingService.Call(
            setup.Table,
            setup.Player2);

        setup.TurnOrderService.AdvanceAfterAction(
            setup.Table,
            reopensBetting: false);

        // Button call 20
        setup.BettingService.Call(
            setup.Table,
            setup.Button);

        setup.TurnOrderService.AdvanceAfterAction(
            setup.Table,
            reopensBetting: false);

        // Small Blind đã đặt 10, call thêm 10
        setup.BettingService.Call(
            setup.Table,
            setup.SmallBlind);

        setup.TurnOrderService.AdvanceAfterAction(
            setup.Table,
            reopensBetting: false);

        Assert.Equal(
            setup.BigBlind.Id,
            setup.Table.CurrentPlayer!.Id);

        // Act: BB đã đủ 20 nên check
        setup.BettingService.Check(
            setup.Table,
            setup.BigBlind);

        setup.TurnOrderService.AdvanceAfterAction(
            setup.Table,
            reopensBetting: false);

        // Assert
        Assert.True(
            setup.TurnOrderService
                .IsBettingRoundComplete(
                    setup.Table));

        Assert.Null(
            setup.Table.CurrentPlayer);

        Assert.Equal(
            -1,
            setup.Table.CurrentPlayerIndex);

        Assert.Equal(
            100m,
            setup.Table.CurrentPot);

        Assert.All(
            setup.Table.Players,
            player =>
                Assert.Equal(
                    20m,
                    player.StreetCommitted));
    }

    [Fact]
    public void OnlyOnePlayerRemains_ShouldCompleteRound()
    {
        // Arrange
        var setup =
            CreateFivePlayerTable();

        setup.Player2.HasFolded = true;
        setup.Button.HasFolded = true;
        setup.SmallBlind.HasFolded = true;
        setup.BigBlind.HasFolded = true;

        // Act
        bool isComplete =
            setup.TurnOrderService
                .IsBettingRoundComplete(
                    setup.Table);

        // Assert
        Assert.True(isComplete);
    }
}