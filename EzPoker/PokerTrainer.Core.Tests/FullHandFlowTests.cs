using PokerTrainer.Enum;
using PokerTrainer.Models;
using PokerTrainer.Services;
using Xunit;

namespace PokerTrainer.Core.Tests;

public class FullHandFlowTests
{
    [Fact]
    public void CompleteHand_ShouldAwardPotAndPreserveTotalChips()
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

        IPotService potService =
            new PotService();

        var lifecycleService =
            new HandLifecycleService(
                bettingService,
                turnOrderService);

        IPokerGameService gameService =
            new PokerGameService(
                bettingService,
                turnOrderService,
                lifecycleService,
                potService);

        decimal totalChipsBefore =
            table.TotalChipsOnTable;

        lifecycleService.StartNewHand(
            table,
            buttonSeat: 1,
            smallBlind: 10,
            bigBlind: 20);

        /*
         * PREFLOP
         *
         * Button call 20
         * SB call thêm 10
         * BB check
         */

        gameService.Call(table);
        gameService.Call(table);
        gameService.Check(table);

        Assert.True(
            turnOrderService
                .IsBettingRoundComplete(table));

        Assert.Equal(
            60m,
            table.CurrentPot);

        gameService.MoveToNextStreet(table);

        Assert.Equal(
            PokerStreet.Flop,
            table.CurrentHand!.CurrentStreet);

        /*
         * FLOP
         *
         * SB check
         * BB check
         * Button bet 40
         * SB call 40
         * BB fold
         */

        gameService.Check(table);
        gameService.Check(table);
        gameService.Bet(table, 40);
        gameService.Call(table);
        gameService.Fold(table);

        Assert.True(
            turnOrderService
                .IsBettingRoundComplete(table));

        Assert.Equal(
            140m,
            table.CurrentPot);

        gameService.MoveToNextStreet(table);

        Assert.Equal(
            PokerStreet.Turn,
            table.CurrentHand!.CurrentStreet);

        /*
         * TURN
         *
         * SB check
         * Button check
         */

        gameService.Check(table);
        gameService.Check(table);

        Assert.True(
            turnOrderService
                .IsBettingRoundComplete(table));

        gameService.MoveToNextStreet(table);

        Assert.Equal(
            PokerStreet.River,
            table.CurrentHand!.CurrentStreet);

        /*
         * RIVER
         *
         * SB check
         * Button check
         */

        gameService.Check(table);
        gameService.Check(table);

        Assert.True(
            turnOrderService
                .IsBettingRoundComplete(table));

        gameService.MoveToNextStreet(table);

        Assert.Equal(
            PokerStreet.Showdown,
            table.CurrentHand!.CurrentStreet);

        /*
         * SHOWDOWN
         *
         * Dealer chọn Button thắng.
         */

        decimal awardedPot =
            gameService.AwardPot(
                table,
                button.Id);

        // Pot phải bằng 140
        Assert.Equal(
            140m,
            awardedPot);

        /*
         * Button:
         * 1000 - 20 preflop - 40 flop + 140 pot
         * = 1080
         */
        Assert.Equal(
            1080m,
            button.Stack);

        /*
         * SB:
         * 1000 - 20 preflop - 40 flop
         * = 940
         */
        Assert.Equal(
            940m,
            smallBlind.Stack);

        /*
         * BB:
         * 1000 - 20 preflop
         * = 980
         */
        Assert.Equal(
            980m,
            bigBlind.Stack);

        // CurrentHand đã được chuyển vào lịch sử
        Assert.Null(
            table.CurrentHand);

        Assert.Single(
            table.HandHistory);

        PokerHand completedHand =
            table.HandHistory[0];

        Assert.Equal(
            PokerStreet.Completed,
            completedHand.CurrentStreet);

        Assert.Equal(
            button.Id,
            completedHand.WinnerId);

        Assert.Equal(
            140m,
            completedHand.AwardedPot);

        // Chip cam kết phải được reset
        Assert.All(
            table.Players,
            player =>
            {
                Assert.Equal(
                    0m,
                    player.StreetCommitted);

                Assert.Equal(
                    0m,
                    player.TotalCommitted);
            });

        // Tổng chip trên bàn không thay đổi
        Assert.Equal(
            totalChipsBefore,
            table.TotalChipsOnTable);
    }
}