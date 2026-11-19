using System;
using System.Collections.Generic;
using System.Text;

namespace PokerTrainer.Models
{
    public class Player
    {
        public Guid Id { get; init; } = Guid.NewGuid(); 
        public string Name { get; set; } = string.Empty;

        public int SeatNumber { get; set; }
        public decimal Stack { get; set; }
        public bool HasFolded { get; set; }
        public bool IsAllIn { get; set; }
        public decimal StreetCommitted { get; set; }
        public decimal TotalCommitted { get; set; }
    }
}
