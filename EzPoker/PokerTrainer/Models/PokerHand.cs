using PokerTrainer.Enum;
using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Text;

namespace PokerTrainer.Models
{
    // Đại diện cho một ván bài từ lúc thu blind đến khi trao pot
    public class PokerHand
    {
        private readonly List<PokerAction> _actions = new List<PokerAction>();
        public Guid Id { get; init; } = Guid.NewGuid();
        public int HandNumber { get;}
        public PokerStreet CurrentStreet { get; private set; }

        //Số ghế đang giữ button
        public int ButtonSeat { get; }
        public decimal SmallBlind { get; }
        public decimal BigBlind { get; }

        //Mức cược cao nhất trong street hiện tại
        public decimal CurrentBet { get; private set; }
        // Kích thước lần bet/raise gần nhất
        public decimal MinimumRaiseIncrement { get; private set; }
        public decimal MinimumRaiseTo => CurrentBet + MinimumRaiseIncrement;
        public IReadOnlyList<PokerAction> Actions => _actions.AsReadOnly();
        public decimal TotalPot => _actions.Sum(action => action.AmountAdded);
        public Guid? WinnerId { get; private set; }
        public string? WinnerName { get; private set; }
        public decimal AwardedPot { get; private set; }
        public PokerHand(int handNumber, int buttonSeat, decimal smallBlind, decimal bigBlind)
        {
            if (handNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(handNumber),
                    "Số ván bài phải lớn hơn 0.");
            }
            if (buttonSeat < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(buttonSeat),
                    "Số ghế button phải lớn hơn hoặc bằng 1.");
            }
            if (smallBlind <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(smallBlind),
                    "Small blind phải lớn hơn 0.");
            }
            if (bigBlind <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bigBlind),
                    "Big blind phải lớn hơn 0.");
            }

            HandNumber = handNumber;
            ButtonSeat = buttonSeat;
            SmallBlind = smallBlind;
            BigBlind = bigBlind;
            CurrentStreet = PokerStreet.PreFlop;
            CurrentBet = 0;
            MinimumRaiseIncrement = bigBlind;
        }

        public decimal GetPotForStreet(PokerStreet street)
        {
            return _actions
                .Where(action => action.Street == street)
                .Sum(action => action.AmountAdded);
        }

        public void AddAction(PokerAction action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action), "Hành động không được null.");
            }
            if (action.Street != CurrentStreet)
            {
                throw new InvalidOperationException(
                    $"Hành động không hợp lệ cho street hiện tại. Street hiện tại: {CurrentStreet}, Street của hành động: {action.Street}");
            }
            _actions.Add(action);

            // Cập nhật mức cược cao nhất nếu cần
            /*if (action.TotalStreetBet > CurrentBet)
            {
                CurrentBet = action.TotalStreetBet;
            }*/
        }

        public void UpdateCurrentBet(decimal newBet)
        {
            if (newBet < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(newBet),
                    "Mức cược mới không được âm.");
            }
            CurrentBet = newBet;
        }

        public void MoveToStreet(PokerStreet newStreet)
        {
            if (newStreet <= CurrentStreet)
            {
                throw new InvalidOperationException(
                    $"Không thể chuyển sang street {newStreet} từ street {CurrentStreet}. Street mới phải lớn hơn street hiện tại.");
            }
            CurrentStreet = newStreet;

            // Sang vòng mới cập nhật lại bet
            CurrentBet = 0;
            MinimumRaiseIncrement = BigBlind;
        }

        private bool IsValidNextStreet(PokerStreet nextStreet)
        {
            return CurrentStreet switch
            {
                PokerStreet.PreFlop =>
                    nextStreet == PokerStreet.Flop,

                PokerStreet.Flop =>
                    nextStreet == PokerStreet.Turn,

                PokerStreet.Turn =>
                    nextStreet == PokerStreet.River,

                PokerStreet.River =>
                    nextStreet == PokerStreet.Showdown,

                PokerStreet.Showdown =>
                    nextStreet == PokerStreet.Completed,

                _ => false
            };
        }

        public void RegisterBet(decimal betAmount)
        {
            if(CurrentBet > 0)
            {
                throw new InvalidOperationException(
                    "Đã có mức cược trong vòng, hãy dùng raise.");
            }    

            if(betAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(betAmount),
                    "Mức cược phải lớn hơn 0.");
            }

            CurrentBet = betAmount;
            MinimumRaiseIncrement = betAmount;
        }

        public void RegisterRaise(decimal newTotalBet)
        {
            if (CurrentBet <= 0)
            {
                throw new InvalidOperationException(
                    "Chưa có mức cược trong vòng, hãy dùng bet.");
            }
            if (newTotalBet <= CurrentBet)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(newTotalBet),
                    "Mức raise phải lớn hơn mức cược hiện tại.");
            }
            
            decimal raiseSize = newTotalBet - CurrentBet;

            CurrentBet = newTotalBet;

            // Short all-in không làm thay đổi mức full raise gần nhất
            if(raiseSize >= MinimumRaiseIncrement)
            {
                MinimumRaiseIncrement = raiseSize;
            }
        }

        public void MarkCompleted(Guid winnerId, string winnerName, decimal awardedPot)
        {
            if (CurrentStreet == PokerStreet.Completed)
            {
                throw new InvalidOperationException(
                    "Hand đã kết thúc.");
            }

            if (winnerId == Guid.Empty)
            {
                throw new ArgumentException(
                    "WinnerId không hợp lệ.",
                    nameof(winnerId));
            }

            if (string.IsNullOrWhiteSpace(winnerName))
            {
                throw new ArgumentException(
                    "Tên người thắng không hợp lệ.",
                    nameof(winnerName));
            }

            if (awardedPot <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(awardedPot),
                    "Pot trao thưởng phải lớn hơn 0.");
            }

            WinnerId = winnerId;
            WinnerName = winnerName.Trim();
            AwardedPot = awardedPot;

            CurrentBet = 0;
            CurrentStreet = PokerStreet.Completed;
        }
    }
}
