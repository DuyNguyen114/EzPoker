using PokerTrainer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokerTrainer.Services
{
    public static class NextButtonSeatSelector
    {
        public static int Select(PokerTable table, int previousButtonSeat)
        {
            var eligibleSeats = table.Players.Where(p => p.Stack > 0).Select(p => p.SeatNumber).OrderBy(seat => seat).ToArray();

            if(eligibleSeats.Length < 2)
            {
                throw new InvalidOperationException("Cần ít nhất 2 người còn chip để bắt đầu hand.");
            }

            foreach(int seat in eligibleSeats)
            {
                if (seat > previousButtonSeat) return seat;
            }

            return eligibleSeats[0];
        }
    }
}
