using PokerTrainer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokerTrainer.Mobile.ViewModels
{
    public class PlayerSeatViewModel
    {
        public Player Player { get; }
        public string Name => Player.Name;
        public decimal Stack => Player.Stack;
        public string StackText => $"{Player.Stack:0} chip";
        public bool HasFolded => Player.HasFolded;
        public bool IsAllIn => Player.IsAllIn;
        public bool IsCurrentPlayer { get; set; }
        public string Position { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public string StatusText
        {
            get
            {
                if (HasFolded)
                    return "Folded";
                if (IsAllIn)
                    return "All-in";
                if(IsCurrentPlayer)
                    return "Your turn";
                return $"{Player.Stack:0} chip";
            }
        }
        public PlayerSeatViewModel(Player player, string position)
        {
            Player =
            player ??
            throw new ArgumentNullException(
                nameof(player));

            Position = position;
        }
    }
}
