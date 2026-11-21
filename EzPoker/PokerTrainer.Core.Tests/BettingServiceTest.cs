using PokerTrainer.Models;
using PokerTrainer.Services;
using System.Net.WebSockets;

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
            
            var service = new BettingService();

            return (table, button, smallBlind, bigBlind, service);
        }
    }
}
