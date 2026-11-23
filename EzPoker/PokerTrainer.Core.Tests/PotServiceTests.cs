using PokerTrainer.Models;
using PokerTrainer.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokerTrainer.Core.Tests
{
    public class PotServiceTests
    {
        [Fact]
        public void AwardEntirePot_ShouldAddPotToWinner()
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

            bettingService.Call(
                table,
                button);

            bettingService.Call(
                table,
                smallBlind);

            bettingService.Check(
                table,
                bigBlind);

            IPotService potService =
                new PotService();

            decimal totalChipsBefore =
                table.TotalChipsOnTable;

            // Act
            decimal awardedPot =
                potService.AwardEntirePot(
                    table,
                    button.Id);

            // Assert
            Assert.Equal(
                60m,
                awardedPot);

            // Button đã trả 20 và thắng pot 60
            Assert.Equal(
                1040m,
                button.Stack);

            Assert.Equal(
                980m,
                smallBlind.Stack);

            Assert.Equal(
                980m,
                bigBlind.Stack);

            Assert.Null(
                table.CurrentHand);

            Assert.Single(
                table.HandHistory);

            PokerHand completedHand =
                table.HandHistory[0];

            Assert.Equal(
                button.Id,
                completedHand.WinnerId);

            Assert.Equal(
                "Button",
                completedHand.WinnerName);

            Assert.Equal(
                60m,
                completedHand.AwardedPot);

            Assert.Equal(
                totalChipsBefore,
                table.TotalChipsOnTable);

            Assert.All(
                table.Players,
                player =>
                    Assert.Equal(
                        0m,
                        player.TotalCommitted));
        }

        [Fact]
        public void Fold_WhenOnlyOnePlayerRemains_ShouldAwardPotAutomatically()
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

            lifecycleService.StartNewHand(
                table,
                buttonSeat: 1,
                smallBlind: 10,
                bigBlind: 20);

            // Act: Button/SB fold
            gameService.Fold(table);

            // Assert: BB nhận pot 30
            Assert.Equal(
                1010m,
                bigBlind.Stack);

            Assert.Equal(
                990m,
                button.Stack);

            Assert.Null(
                table.CurrentHand);

            Assert.Single(
                table.HandHistory);

            Assert.Equal(
                bigBlind.Id,
                table.HandHistory[0].WinnerId);

            Assert.Equal(
                30m,
                table.HandHistory[0].AwardedPot);
        }
    }
}
