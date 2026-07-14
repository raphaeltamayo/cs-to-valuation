namespace CStoValuation.Core.Models;

/// <summary>How much a portfolio's net value has moved over some look-back window.</summary>
public sealed record PerformanceDelta(decimal ChangeAmount, decimal ChangePercent);
