using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CStoValuation.Infrastructure.Persistence;

/// <summary>
/// Stores <see cref="DateTimeOffset"/> values as Unix-millisecond integers. SQLite has no
/// native date type, so by default EF persists a <see cref="DateTimeOffset"/> as text that
/// it cannot translate into SQL <c>ORDER BY</c>/comparison. Converting to a 64-bit integer
/// makes time columns first-class: sortable and filterable in the database. All our
/// timestamps are UTC instants, so collapsing the offset to zero on read is lossless here.
/// </summary>
internal sealed class DateTimeOffsetToUnixMillisecondsConverter : ValueConverter<DateTimeOffset, long>
{
    public DateTimeOffsetToUnixMillisecondsConverter()
        : base(
            value => value.ToUnixTimeMilliseconds(),
            value => DateTimeOffset.FromUnixTimeMilliseconds(value))
    {
    }
}
