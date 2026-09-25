using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KCC.Contributions.Data;

// SQLite keeps a DateTime as text with no zone, so it reads back Unspecified, and a browser would take the JSON for
// local time. Every time this store writes is UTC.
internal sealed class UtcDateTimeConverter()
    : ValueConverter<DateTime, DateTime>(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
