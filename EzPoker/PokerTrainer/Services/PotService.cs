using PokerTrainer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokerTrainer.Services
{
    public class PotService : IPotService
    {
        public PotBreakdown Preview(PokerTable table)
        {
            var hand = table.CurrentHand ?? throw new InvalidOperationException("Chưa có hand.");
            var players = table.Players;
            if (players.Any(p => p.TotalCommitted < 0) ||
                players.Sum(p => p.TotalCommitted) != hand.TotalPot)
                throw new InvalidOperationException("Số chip đã góp không khớp với tổng pot.");

            var pots = new List<PotLayer>();
            var refunds = new List<PotPayment>();
            var live = players.Where(p => !p.HasFolded).Select(p => p.Id).ToArray();
            if (live.Length == 0)
                throw new InvalidOperationException("Không có người đủ điều kiện nhận pot.");

            decimal previous = 0;
            foreach (decimal level in players.Select(p => p.TotalCommitted)
                         .Where(amount => amount > 0).Distinct().OrderBy(amount => amount))
            {
                var contributors = players.Where(p => p.TotalCommitted >= level).ToArray();
                decimal amount = (level - previous) * contributors.Length;
                previous = level;
                if (contributors.Length == 1)
                {
                    var player = contributors[0];
                    refunds.Add(new(player.Id, player.Name, amount, null));
                    continue;
                }

                var eligible = contributors.Where(p => !p.HasFolded).Select(p => p.Id).ToArray();
                if (live.Length == 1)
                    eligible = live;
                if (eligible.Length == 0)
                    eligible = pots.Count > 0 ? pots[^1].EligibleIds.ToArray() : live;
                if (pots.Count > 0 && pots[^1].EligibleIds.SequenceEqual(eligible))
                {
                    var last = pots[^1];
                    pots[^1] = last with { Amount = last.Amount + amount };
                    continue;
                }
                pots.Add(new(pots.Count + 1, amount, Array.AsReadOnly(eligible)));
            }
            return new(pots.AsReadOnly(), refunds.AsReadOnly());
        }

        public IReadOnlyList<PotPayment> SettleShowdown(
            PokerTable table, IReadOnlyDictionary<int, Guid[]> winners)
        {
            if (table.CurrentHand?.CurrentStreet != PokerTrainer.Enum.PokerStreet.Showdown)
                throw new InvalidOperationException("Chỉ có thể chọn người thắng tại Showdown.");
            return Settle(table, Preview(table), winners);
        }

        private static IReadOnlyList<PotPayment> Settle(PokerTable table, PotBreakdown breakdown,
            IReadOnlyDictionary<int, Guid[]> selections)
        {
            var hand = table.CurrentHand!;
            if (selections.Keys.Any(number => !breakdown.Pots.Any(p => p.Number == number)))
                throw new ArgumentException("Có pot không tồn tại.", nameof(selections));

            var byId = table.Players.ToDictionary(p => p.Id);
            var payments = new List<PotPayment>(breakdown.Refunds);
            foreach (var pot in breakdown.Pots)
            {
                Guid[] ids = selections.TryGetValue(pot.Number, out var chosen)
                    ? chosen
                    : pot.EligibleIds.Count == 1 ? pot.EligibleIds.ToArray() : Array.Empty<Guid>();
                if (ids.Length == 0 || ids.Distinct().Count() != ids.Length ||
                    ids.Any(id => !pot.EligibleIds.Contains(id)))
                    throw new ArgumentException($"Chọn người thắng hợp lệ cho pot {pot.Number}.");

                // Chip lẻ: trao theo thứ tự ghế ngay sau Button, có vòng lại.
                var ordered = ids.Select(id => byId[id])
                    .OrderBy(p => p.SeatNumber <= hand.ButtonSeat)
                    .ThenBy(p => p.SeatNumber).ToArray();
                // Giữ tương thích với blind cược lẻ của model decimal.
                decimal unit = 1m;
                while (table.Players.Any(p => p.TotalCommitted % unit != 0))
                    unit /= 10m;
                decimal share = decimal.Floor(pot.Amount / unit / ordered.Length) * unit;
                decimal remainder = pot.Amount - share * ordered.Length;
                foreach (var player in ordered)
                {
                    decimal extra = Math.Min(unit, remainder);
                    remainder -= extra;
                    payments.Add(new(player.Id, player.Name, share + extra, pot.Number));
                }
            }

            if (payments.Sum(p => p.Amount) != hand.TotalPot)
                throw new InvalidOperationException("Tổng chip chi trả không khớp với pot.");

            foreach (var payment in payments)
                byId[payment.PlayerId].Stack += payment.Amount;
            hand.MarkSettled(payments);
            ResetCommittedChips(table);
            table.CompleteCurrentHand();
            return hand.Payments;
        }
        public decimal AwardEntirePot(PokerTable table, Guid winnerId)
        {
            ArgumentNullException.ThrowIfNull(table, nameof(table));

            PokerHand hand = table.CurrentHand ?? throw new InvalidOperationException("Không có ván bài hiện tại để trao pot.");

            Player winner = table.Players.FirstOrDefault(p => p.Id == winnerId) ?? throw new ArgumentException("Người chơi chiến thắng không tồn tại trong bàn.", nameof(winnerId));

            if (winner.HasFolded)
            {
                throw new InvalidOperationException("Người chơi chiến thắng đã fold, không thể trao pot.");
            }

            decimal pot = hand.TotalPot;

            if (pot <= 0)
            {
                throw new InvalidOperationException("Pot hiện tại bằng 0, không thể trao pot.");
            }

            var breakdown = Preview(table);
            return Settle(table, breakdown,
                    breakdown.Pots.ToDictionary(layer => layer.Number, _ => new[] { winner.Id }))
                .Where(payment => payment.PotNumber.HasValue).Sum(payment => payment.Amount);
        }

        public decimal AwardToLastRemainingPlayer(PokerTable table)
        {
            ArgumentNullException.ThrowIfNull(table);

            List<Player> remainingPlayers = table.Players.Where(p => !p.HasFolded).ToList();

            if(remainingPlayers.Count != 1)
            {
                throw new InvalidOperationException("Phải có đúng một người chưa fold.");
            }

            return Settle(table, Preview(table), new Dictionary<int, Guid[]>())
                .Where(payment => payment.PotNumber.HasValue).Sum(payment => payment.Amount);
        }

        private static void ResetCommittedChips(PokerTable table)
        {
            foreach (var player in table.Players)
            {
                player.StreetCommitted = 0;
                player.TotalCommitted = 0;
                player.HasActedThisStreet = false;
            }
        }
    }
}
