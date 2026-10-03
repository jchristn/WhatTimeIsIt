<p align="center">
  <img src="assets/logo.png" alt="WhatTimeIsIt Logo" width="192" height="192">
</p>

# WhatTimeIsIt

A comprehensive .NET library for parsing DateTime and DateTimeOffset strings from various database systems and standard formats while preserving microsecond precision and timezone offsets.

<sub>Logo by [Uniconlabs](https://www.flaticon.com/free-icon/real-time_7988691?k=1762392415076)</sub>

## What This Library Does

WhatTimeIsIt provides robust DateTime parsing that handles:

- **Multiple Database Formats**: MySQL, SQL Server, PostgreSQL, Oracle, SQLite datetime strings
- **High Precision**: Preserves up to 7 digits of fractional seconds (100-nanosecond precision)
- **Timezone Awareness**: Handles UTC, timezone offsets, and local times correctly
- **Numeric Timestamps**: Parses Unix timestamps (seconds/milliseconds) and .NET ticks
- **Flexible Formats**: Over 100 pre-configured datetime format patterns
- **Culture Support**: Works with various international date formats
- **Extensible**: Allows custom format arrays for specific use cases

## Installation

```bash
dotnet add reference path/to/WhatTimeIsIt.csproj
```

Or copy the `WhatTimeIsIt` project into your solution.

## How to Use It

### Basic Usage (Static Methods)

The simplest way to parse datetime strings:

```csharp
using WhatTimeIsIt;

// Parse a datetime string using default formats
DateTime dt = DateTimeParser.ParseString("2024-01-15 14:30:45.123456");
Console.WriteLine(dt); // 1/15/2024 2:30:45 PM

// Safe parsing with TryParse
if (DateTimeParser.TryParseString("2024-01-15 14:30:45", out DateTime result))
{
    Console.WriteLine($"Parsed: {result}");
}
else
{
    Console.WriteLine("Failed to parse");
}
```

### Instance Usage with Custom Formats

For repeated parsing with specific formats:

```csharp
using WhatTimeIsIt;

// Create a parser instance
var parser = new DateTimeParser();

// Use default formats
DateTime dt1 = parser.Parse("2024-01-15 14:30:45.123456");

// Set custom formats for your specific needs
parser.Formats = new[]
{
    "yyyy-MM-dd HH:mm:ss.ffffff",
    "dd/MM/yyyy HH:mm:ss",
    "MM/dd/yyyy HH:mm:ss"
};

DateTime dt2 = parser.Parse("15/01/2024 14:30:45");

// Reset to defaults when needed
parser.ResetToDefaults();
```

### Parsing Various Database Formats

```csharp
// SQL Server datetime2(7)
DateTime sqlServer = DateTimeParser.ParseString("2024-01-15 14:30:45.1234567");

// MySQL datetime with microseconds
DateTime mysql = DateTimeParser.ParseString("2024-01-15 14:30:45.123456");

// PostgreSQL timestamp
DateTime postgres = DateTimeParser.ParseString("2024-01-15T14:30:45.123456Z");

// Oracle with period separators
DateTime oracle = DateTimeParser.ParseString("15-Jan-24 14.30.45.123456");

// SQLite ISO 8601
DateTime sqlite = DateTimeParser.ParseString("2024-01-15T14:30:45.123Z");
```

### Parsing Numeric Timestamps

```csharp
// Unix timestamp (seconds since 1970-01-01)
DateTime unixSec = DateTimeParser.ParseString("1705329045");

// Unix timestamp (milliseconds)
DateTime unixMs = DateTimeParser.ParseString("1705329045000");

// .NET Ticks
DateTime ticks = DateTimeParser.ParseString("638405790450000000");
```

### Preserving Microsecond Precision

```csharp
// Parse a datetime with microseconds
DateTime dt = DateTimeParser.ParseString("2024-01-15 14:30:45.123456");

// Access the full precision
long ticks = dt.Ticks;
Console.WriteLine($"Ticks: {ticks}");

// Extract microseconds
int totalMicroseconds = (int)(ticks % 10000000) / 10;
int milliseconds = totalMicroseconds / 1000;
int microseconds = totalMicroseconds % 1000;
Console.WriteLine($"Milliseconds: {milliseconds}, Microseconds: {microseconds}");
```

## DateTimeOffsetParser - Preserving Timezone Offsets

For scenarios where you need to preserve timezone offset information, use `DateTimeOffsetParser` instead of `DateTimeParser`.

### Key Advantage

`DateTimeOffset` can preserve the original timezone offset (like +05:00, -08:00) from the input string, while `DateTime` can only store UTC, Local, or Unspecified.

### Basic Usage

```csharp
using WhatTimeIsIt;

// Parse with timezone preservation
DateTimeOffset dto = DateTimeOffsetParser.ParseString("2024-01-15T14:30:45+05:00");
Console.WriteLine(dto.Offset); // +05:00 - PRESERVED!
Console.WriteLine(dto.DateTime); // 2024-01-15 14:30:45
Console.WriteLine(dto.UtcDateTime); // 2024-01-15 09:30:45 (converted to UTC)
```

### Configuring Default Offset

For datetime strings without timezone information, you can configure the default offset:

```csharp
var parser = new DateTimeOffsetParser();

// Default is UTC (offset +00:00)
parser.DefaultOffset = TimeSpan.Zero;
var utc = parser.Parse("2024-01-15 14:30:45"); // Offset: +00:00

// Set to PST
parser.DefaultOffset = TimeSpan.FromHours(-8);
var pst = parser.Parse("2024-01-15 14:30:45"); // Offset: -08:00

// Set to India Standard Time
parser.DefaultOffset = TimeSpan.FromHours(5).Add(TimeSpan.FromMinutes(30));
var ist = parser.Parse("2024-01-15 14:30:45"); // Offset: +05:30
```

### DateTime vs DateTimeOffset Comparison

```csharp
string input = "2024-01-15T14:30:45+05:00"; // India time

// With DateTime - offset is LOST, converted to local time
DateTime dt = DateTimeParser.ParseString(input);
Console.WriteLine(dt.Kind); // Local
Console.WriteLine(dt); // Converted to your local time (e.g., 01:30:45 in PST)

// With DateTimeOffset - offset is PRESERVED
DateTimeOffset dto = DateTimeOffsetParser.ParseString(input);
Console.WriteLine(dto.Offset); // +05:00 - Original offset preserved!
Console.WriteLine(dto.DateTime); // 2024-01-15 14:30:45 - Original time preserved!
```

### When to Use DateTimeOffsetParser

Use `DateTimeOffsetParser` when:
- ✅ You need to preserve timezone offsets from the source data
- ✅ You're working with data from multiple timezones
- ✅ You need to accurately represent *when* something happened globally
- ✅ You're building systems that handle international datetime data

Use `DateTimeParser` when:
- ✅ You only care about UTC vs Local vs Unspecified
- ✅ You don't need to preserve the original timezone
- ✅ You're working with legacy code that uses `DateTime`
- ✅ The source data doesn't include timezone information

## When NOT to Use This Library

This library may not be the best choice if:

1. **You only need standard .NET parsing**: If you're working with standard ISO 8601 or common .NET formats, `DateTime.Parse()` or `DateTimeOffset.Parse()` are simpler and faster.

2. **You're working with single, well-known formats**: If you know the exact format of your input, `DateTime.ParseExact()` or `DateTimeOffset.ParseExact()` is more efficient.

3. **Performance is critical with known formats**: The library tries multiple format patterns sequentially. For high-performance scenarios with predictable input, direct parsing is faster.

4. **You need localized datetime strings**: This library focuses on database and technical formats rather than user-facing localized datetime strings.

## Example Usage Scenarios

### Scenario 1: Database Migration Tool

```csharp
using WhatTimeIsIt;

public class DatabaseMigrator
{
    private readonly DateTimeParser _parser = new DateTimeParser();

    public DateTime ImportDateTimeFromAnyDatabase(string dateTimeString)
    {
        // Handles datetime strings from MySQL, SQL Server, Oracle, etc.
        return _parser.Parse(dateTimeString);
    }
}
```

### Scenario 2: API Response Parser

```csharp
using WhatTimeIsIt;

public class ApiResponseParser
{
    public void ParseJsonResponse(string json)
    {
        // JSON might contain various datetime formats
        var timestamps = ExtractTimestampsFromJson(json);

        foreach (var timestamp in timestamps)
        {
            if (DateTimeParser.TryParseString(timestamp, out DateTime dt))
            {
                ProcessDateTime(dt);
            }
        }
    }
}
```

### Scenario 3: Log File Parser

```csharp
using WhatTimeIsIt;

public class LogParser
{
    public void ParseLogFile(string logContent)
    {
        var lines = logContent.Split('\n');

        foreach (var line in lines)
        {
            // Extract timestamp from log line (various formats)
            string timestampStr = ExtractTimestamp(line);

            if (DateTimeParser.TryParseString(timestampStr, out DateTime timestamp))
            {
                var logEntry = new LogEntry
                {
                    Timestamp = timestamp,
                    Message = line
                };
                ProcessLogEntry(logEntry);
            }
        }
    }
}
```

### Scenario 4: Custom Format for Legacy System

```csharp
using WhatTimeIsIt;

public class LegacySystemAdapter
{
    private readonly DateTimeParser _parser;

    public LegacySystemAdapter()
    {
        _parser = new DateTimeParser();

        // Configure for specific legacy system formats
        _parser.Formats = new[]
        {
            "yyyyMMddHHmmss",
            "yyyy-MM-dd HH:mm:ss.fff",
            "MMM dd yyyy hh:mm:ss:ffftt"
        };
    }

    public DateTime ParseLegacyDateTime(string legacyDateTime)
    {
        return _parser.Parse(legacyDateTime);
    }
}
```

## Supported Formats

The library supports over 100 datetime format patterns including:

- ISO 8601 variants (with/without T separator)
- Timezone formats (Z, +00, -00, zzz, K)
- Precision levels: 7-digit, 6-digit (microseconds), 3-digit (milliseconds), seconds, minutes, date-only
- Database-specific formats from major RDBMS systems
- Compact formats (yyyyMMdd, yyyyMMddHHmmss)
- 12-hour and 24-hour time formats
- Multiple date separators (-, /, .)
- US and European date ordering

See `DateTimeParser.DefaultFormats` for the complete list.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## Version

Current version: 1.0.3 - See [CHANGELOG.md](CHANGELOG.md) for version history.
