using PokerTrainer.Models;
using PokerTrainer.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokerTrainer.Core.Tests
{
    public class PokerGameServiceTests
    {
        [Fact]
        public void Call_ShouldUpdateStackAndMoveToNextPlayer()
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

            IPokerGameService gameService =
                new PokerGameService(
                    bettingService,
                    turnOrderService,
                    lifecycleService);

            lifecycleService.StartNewHand(
                table,
                buttonSeat: 1,
                smallBlind: 10,
                bigBlind: 20);

            Assert.Equal(
                button.Id,
                table.CurrentPlayer!.Id);

            // Act
            gameService.Call(table);

            // Assert
            Assert.Equal(
                980m,
                button.Stack);

            Assert.Equal(
                20m,
                button.StreetCommitted);

            Assert.True(
                button.HasActedThisStreet);

            Assert.Equal(
                smallBlind.Id,
                table.CurrentPlayer!.Id);

            Assert.Equal(
                50m,
                table.CurrentPot);
        }

        [Fact]
        public void CompletePreflop_ShouldAllowMovingToFlop()
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

            IPokerGameService gameService =
                new PokerGameService(
                    bettingService,
                    turnOrderService,
                    lifecycleService);

            lifecycleService.StartNewHand(
                table,
                buttonSeat: 1,
                smallBlind: 10,
                bigBlind: 20);

            // Act: BTN call, SB call, BB check
            gameService.Call(table);
            gameService.Call(table);
            gameService.Check(table);

            Assert.True(
                turnOrderService
                    .IsBettingRoundComplete(table));

            Assert.Null(
                table.CurrentPlayer);

            gameService.MoveToNextStreet(table);

            // Assert
            Assert.Equal(
                Enum.PokerStreet.Flop,
                table.CurrentHand!.CurrentStreet);

            Assert.Equal(
                smallBlind.Id,
                table.CurrentPlayer!.Id);

            Assert.Equal(
                60m,
                table.CurrentPot);
        }
    }
}
