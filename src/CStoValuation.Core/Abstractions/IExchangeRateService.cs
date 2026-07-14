namespace CStoValuation.Core.Abstractions;

public interface IExchangeRateService
{
    Task<decimal> ConvertAsync(
        decimal amount, string fromCurrency, string toCurrency, CancellationToken cancellationToken = default);
}
