using PokerTrainer.Models;
using PokerTrainer.Services;
using System.Dynamic;
using System.Net.WebSockets;
using Xunit;

namespace PokerTrainer.Core.Tests
{
    public class BettingServiceTest
    {
        private static (PokerTable table, Player button, Player smallBlind, Player bigBlind, BettingService service)CreateTable()
        {
            var table = new PokerTable("Test table");

            var button = new Player("Button", 1, 1000);
            var smallBlind = new Player("Small Blind", 2, 1000);
            var bigBlind = new Player("Big Blind", 3, 1000);

            table.AddPlayer(button);
            table.AddPlayer(smallBlind);
            table.AddPlayer(bigBlind);

            var hand = new PokerHand(1, 1, 10, 20);

            table.StartHand(hand);

            var service = new BettingService();

            return (table, button, smallBlind, bigBlind, service);
        }

        //Test Post blind

        [Fact]
        public void PostBlinds_ShouldUpdateStackAndPot()
        {
            var setup = CreateTable();

            setup.service.PostSmallBlind(setup.table, setup.smallBlind);

            setup.service.PostBigBlind(setup.table, setup.bigBlind);

            Assert.Equal(990, setup.smallBlind.Stack);

            Assert.Equal(980, setup.bigBlind.Stack);

            Assert.Equal(10, setup.smallBlind.StreetCommitted);
            Assert.Equal(20, setup.bigBlind.StreetCommitted);

            Assert.Equal(30, setup.table.CurrentPot);

            Assert.Equal(20, setup.table.CurrentHand!.CurrentBet);
        }

        // Test Small Blind Call
        [Fact]
        public void Call_SmallBlind_ShouldOnlyAddDifference()
        {
            var setup = CreateTable();

            setup.service.PostSmallBlind(setup.table, setup.smallBlind);
            setup.service.PostBigBlind(setup.table, setup.bigBlind);

            decimal callAmount = setup.service.GetCallAmount(setup.table, setup.smallBlind);

            Assert.Equal(10, callAmount);

            setup.service.Call(setup.table, setup.smallBlind);

            Assert.Equal(980, setup.smallBlind.Stack);

            Assert.Equal(20, setup.smallBlind.StreetCommitted);

            Assert.Equal(20, setup.smallBlind.TotalCommitted);

            Assert.Equal(40, setup.table.CurrentPot);
        }

        // Test Button raise len 60
        [Fact]
        public void RaiseTo_ButtonRaiseTo60_ShouldUpdateBetAndStack()
        {
            var setup = CreateTable();

            setup.service.PostSmallBlind(setup.table, setup.smallBlind);

            setup.service.PostBigBlind(setup.table, setup.bigBlind);

            setup.service.RaiseTo(setup.table, setup.button, 60);

            PokerHand hand = setup.table.CurrentHand;

            Assert.Equal(940, setup.button.Stack);

            Assert.Equal(60, hand.CurrentBet);

            Assert.Equal(40, hand.MinimumRaiseIncrement);

            Assert.Equal(100, hand.MinimumRaiseTo);

            Assert.Equal(90, setup.table.CurrentPot);
        }

        // Test BB  raise lai len 180
        [Fact]
        public void RaiseTo_BigBlindReraises_ShouldOnlyDeductMissingAmount()
        {
            var setup = CreateTable();

            setup.service.PostSmallBlind(
                setup.table,
                setup.smallBlind);

            setup.service.PostBigBlind(
                setup.table,
                setup.bigBlind);

            setup.service.RaiseTo(
                setup.table,
                setup.button,
                totalStreetBet: 60);

            setup.service.RaiseTo(setup.table, setup.bigBlind, totalStreetBet: 180);

            PokerHand hand = setup.table.CurrentHand;   

            // BB da dat 20 nen chi them 160
            Assert.Equal(820, setup.bigBlind.Stack);

            Assert.Equal(180, setup.bigBlind.StreetCommitted);

            Assert.Equal(180, hand.CurrentBet);

            Assert.Equal(120, hand.MinimumRaiseIncrement);

            Assert.Equal(300, hand.MinimumRaiseTo);

            Assert.Equal(250, setup.table.CurrentPot);
        }

        //Test khong duoc check khi can call
        [Fact]
        public void Check_WhenFacingBet_ShouldThrowException()
        {
            var setup = CreateTable();

            setup.service.PostSmallBlind(
                setup.table,
                setup.smallBlind);

            setup.service.PostBigBlind(
                setup.table,
                setup.bigBlind);

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(
                    () => setup.service.Check(
                        setup.table,
                        setup.button));

            Assert.Contains(
                "Không thể check",
                exception.Message);
        }

        // Test Bet không được dùng toàn bộ stack
        [Fact]
        public void Bet_WithEntireStack_ShouldThrowException()
        {
            var setup = CreateTable();

            PokerHand hand =
                setup.table.CurrentHand!;

            hand.MoveToStreet(
                Enum.PokerStreet.Flop);

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(
                    () => setup.service.Bet(
                        setup.table,
                        setup.button,
                        amount: 1000));

            Assert.Contains(
                "All-in",
                exception.Message);
        }
    }
}
