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
        public IReadOnlyList<PotPayment> Payments { get; private set; } = Array.Empty<PotPayment>();
        private readonly Dictionary<PokerStreet, PokerCard[]> _boardByStreet = new();
        public Guid HistoryId { get; } = Guid.NewGuid();


        public HandHistoryRecord CreateHistoryRecord()
        {
            return new HandHistoryRecord(
                HistoryId,
                DateTimeOffset.UtcNow,
                HeroSeatNumber,
                HeroHoleCards.ToArray(),
                GetRevealedCards(PokerStreet.Flop).ToArray(),
                GetRevealedCards(PokerStreet.Turn).ToArray(),
                GetRevealedCards(PokerStreet.River).ToArray(),
                CopyShowdownCards());
        }
        private static readonly PokerStreet[] BoardStreets =
        {
            PokerStreet.Flop, PokerStreet.Turn, PokerStreet.River
        };

        private PokerCard[] _heroHoleCards = Array.Empty<PokerCard>();

        public int? HeroSeatNumber { get; private set; }

        public IReadOnlyList<PokerCard> HeroHoleCards => Array.AsReadOnly(_heroHoleCards);
        private readonly Dictionary<int, PokerCard[]> _showdownCardsBySeat = new();
        public IReadOnlyList<PokerCard> GetRevealedCards(PokerStreet street)
        {
            return _boardByStreet.TryGetValue(street, out var cards) ? Array.AsReadOnly(cards) : Array.Empty<PokerCard>();
        }
        public IReadOnlyList<PokerCard> GetBoardThrough(PokerStreet street)
        {
            return BoardStreets.Where(s => s <= street).SelectMany(GetRevealedCards).ToArray();
        }
        public void SetBoardCards(PokerStreet street, params PokerCard[] cards)
        {
            if (!BoardStreets.Contains(street) || street > CurrentStreet || CurrentStreet == PokerStreet.Completed)
            {
                throw new InvalidOperationException("Chỉ nhập bài cho vòng đã đến trong hand đang chơi");
            }

            int expectedCount = street == PokerStreet.Flop ? 3 : 1;

            var otherCards = _boardByStreet.Where(entry => entry.Key != street).SelectMany(entry => entry.Value).Concat(_heroHoleCards);

            ValidateCards(cards, expectedCount, otherCards);
            _boardByStreet[street] = cards.ToArray();
        }

        public void SetHeroCards(int seatNumber, params PokerCard[] cards)
        {
            if(CurrentStreet == PokerStreet.Completed)
            {
                throw new InvalidOperationException("Hand đã kết thúc");
            }
            if (seatNumber <= 0) throw new ArgumentOutOfRangeException(nameof(seatNumber));

            ValidateCards(cards, 2, _boardByStreet.Values.SelectMany(x => x));

            HeroSeatNumber = seatNumber;
            _heroHoleCards = cards.ToArray();
        }

        private static void ValidateCards(PokerCard[] cards, int expectedCount, IEnumerable<PokerCard> otherCards)
        {
            ArgumentNullException.ThrowIfNull(cards);

            if (cards.Length != expectedCount || cards.Any(c => c.Rank < 2 || c.Rank > 14 || !System.Enum.IsDefined(c.Suit)))
            {
                throw new ArgumentException($"Cần đúng {expectedCount} lá bài hợp lệ.", nameof(cards));
            } 

            if(cards.Distinct().Count() != cards.Length || cards.Intersect(otherCards).Any())
            {
                throw new ArgumentException("Có lá bài bị trùng.", nameof(cards));
            }
        }


        internal void MarkSettled(IReadOnlyList<PotPayment> payments)
        {
            Payments = Array.AsReadOnly(payments.ToArray());
            var awards = payments.Where(p => p.PotNumber.HasValue).ToArray();
            var winners = awards.Select(p => p.PlayerId).Distinct().ToArray();
            WinnerId = winners.Length == 1 ? winners[0] : null;
            WinnerName = string.Join(", ", awards.Select(p => p.PlayerName).Distinct());
            AwardedPot = awards.Sum(p => p.Amount);
            CurrentBet = 0;
            CurrentStreet = PokerStreet.Completed;
        }
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

        internal void RestoreBetting(int actionCount, decimal currentBet, decimal minimumRaiseIncrement)
        {
            _actions.RemoveRange(actionCount, _actions.Count - actionCount);

            CurrentBet = currentBet;
            MinimumRaiseIncrement = minimumRaiseIncrement;
        }

        public void SetShowDownCards(int seatNumber, PokerCard first, PokerCard second)
        {
            if (CurrentStreet != PokerStreet.Showdown)
                throw new InvalidOperationException("Chỉ ghi bài đã lật ở Showdown.");

            if (seatNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(seatNumber));

            if (seatNumber == HeroSeatNumber)
                throw new ArgumentException("Bài Hero đã được ghi riêng.");

            if (first == second)
                throw new ArgumentException("Hai lá bài không được trùng nhau.");

            var usedCards = GetBoardThrough(PokerStreet.River)
                .Concat(HeroHoleCards)
                .Concat(_showdownCardsBySeat
                    .Where(entry => entry.Key != seatNumber)
                    .SelectMany(entry => entry.Value))
                .ToHashSet();

            if (usedCards.Contains(first) || usedCards.Contains(second))
                throw new ArgumentException("Lá bài đã được ghi ở board hoặc ghế khác.");

            _showdownCardsBySeat[seatNumber] = [first, second];
        }

        public Dictionary<int, PokerCard[]> CopyShowdownCards()
        {
            return _showdownCardsBySeat.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.ToArray());
        }
    }
}
