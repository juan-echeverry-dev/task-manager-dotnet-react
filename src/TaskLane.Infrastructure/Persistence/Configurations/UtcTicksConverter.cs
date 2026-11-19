using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TaskLane.Infrastructure.Persistence.Configurations;

/// <summary>
/// Stores a UTC <see cref="DateTimeOffset"/> as its tick count. SQLite cannot order
/// <see cref="DateTimeOffset"/> values stored as text, and the built-in binary converter drops
/// sub-millisecond precision; ticks are lossless and sortable. The domain guarantees UTC values.
/// </summary>
internal sealed class UtcTicksConverter() : ValueConverter<DateTimeOffset, long>(
    value => value.UtcTicks,
    ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
