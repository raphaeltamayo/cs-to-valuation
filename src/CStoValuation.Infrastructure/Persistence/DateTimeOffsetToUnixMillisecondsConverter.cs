using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CStoValuation.Infrastructure.Persistence;

internal sealed class DateTimeOffsetToUnixMillisecondsConverter : ValueConverter<DateTimeOffset, long>
{
    public DateTimeOffsetToUnixMillisecondsConverter()
        : base(
            value => value.ToUnixTimeMilliseconds(),
            value => DateTimeOffset.FromUnixTimeMilliseconds(value))
    {
    }
}
