namespace PokerTrainer.Models;

public sealed record PotLayer(int Number, decimal Amount, IReadOnlyList<Guid> EligibleIds);
public sealed record PotPayment(Guid PlayerId, string PlayerName, decimal Amount, int? PotNumber);
public sealed record PotBreakdown(IReadOnlyList<PotLayer> Pots, IReadOnlyList<PotPayment> Refunds);
