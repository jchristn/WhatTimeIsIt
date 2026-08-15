# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

WhatTimeIsIt is a .NET 8.0 library that provides comprehensive DateTime and DateTimeOffset parsing capabilities for various database and standard datetime formats while preserving microsecond precision (up to 7 digits) and timezone offsets. The library handles formats from MySQL, SQLite, SQL Server, Oracle, PostgreSQL, Unix timestamps, and .NET ticks.

## Solution Structure

- **WhatTimeIsIt**: Core library project containing parsers
  - Targets: `netstandard2.0;netstandard2.1;net8.0` class library
  - Main classes:
    - `WhatTimeIsIt.DateTimeParser` in `DateTimeParser.cs`
    - `WhatTimeIsIt.DateTimeOffsetParser` in `DateTimeOffsetParser.cs`

### Testing infrastructure (Touchstone)

Tests are built on [Touchstone](https://github.com/jchristn/touchstone), a runner-agnostic test
descriptor framework. Test cases are defined **once** as descriptor objects and executed through
multiple hosts without changing any test logic.

- **Test.Shared**: The single source of truth for all tests
  - Target: .NET 8.0 class library; references `Touchstone.Core` and the WhatTimeIsIt project
  - `WhatTimeIsItSuites.All` aggregates every `TestSuiteDescriptor`
  - `DateTimeParserSuites` / `DateTimeOffsetParserSuites` define the exhaustive positive/negative cases
  - `Assert` provides runner-agnostic assertion helpers (throwing signals failure)
- **Test.Automated**: Touchstone CLI runner (console app)
  - Target: .NET 8.0 executable; references `Touchstone.Cli` and Test.Shared
  - Runs `ConsoleRunner.RunAsync(WhatTimeIsItSuites.All)`; colored tabular output, non-zero exit on failure
  - `-- --results <path>` additionally exports JSON results
- **Test.Xunit**: Touchstone xUnit adapter (theory-driven; one xUnit test per descriptor)
  - Target: .NET 8.0; references `Touchstone.XunitAdapter` + xUnit + Test.Shared
- **Test.Nunit**: Touchstone NUnit adapter (TestCaseSource-driven; one NUnit test per descriptor)
  - Target: .NET 8.0; references `Touchstone.NunitAdapter` + NUnit + Test.Shared
- **Test.Analysis**: Console application for behavior analysis (NOT a Touchstone test project)
  - Target: .NET 8.0 executable; references the WhatTimeIsIt project
  - Prints a comparative DateTime vs DateTimeOffset behavior analysis; retained as a diagnostic tool

> To add or change coverage, edit the descriptors in **Test.Shared** only. All three runners pick up
> the change automatically. Do not add test logic directly to the runner/adapter projects.

## Build Commands

```bash
# Build the entire solution
dotnet build WhatTimeIsIt.sln

# Build specific projects
dotnet build WhatTimeIsIt\WhatTimeIsIt.csproj
dotnet build Test.Automated\Test.Automated.csproj

# Build in Release mode
dotnet build WhatTimeIsIt.sln -c Release
```

## Running Tests

The same Touchstone descriptors (defined in Test.Shared) can be run three ways:

```bash
# 1. Touchstone CLI runner (colored tabular output, exit code 0/non-zero)
dotnet run --project Test.Automated\Test.Automated.csproj
dotnet run --project Test.Automated\Test.Automated.csproj -- --results results.json

# 2. xUnit via dotnet test
dotnet test Test.Xunit\Test.Xunit.csproj

# 3. NUnit via dotnet test
dotnet test Test.Nunit\Test.Nunit.csproj

# Run every test project in the solution (xUnit + NUnit)
dotnet test WhatTimeIsIt.sln
```

The CLI runner (Test.Automated):
- Exits with code 0 if all tests pass, non-zero if any fail
- Prints per-test PASS/FAIL/SKIP results with runtimes and a failure summary

## Architecture

### DateTimeParser Class (WhatTimeIsIt\DateTimeParser.cs)

The `DateTimeParser` class is the heart of the library and provides both static and instance methods:

**Static Methods:**
- `ParseString(string input)` - Parse using default formats
- `ParseString(string input, string[] formats)` - Parse with custom formats
- `TryParseString(string input, out DateTime result)` - Safe parsing with defaults
- `TryParseString(string input, string[] formats, out DateTime result)` - Safe parsing with custom formats
- `DefaultFormats` property - Returns a clone of default format strings

**Instance Methods:**
- `Parse(string input)` - Parse using instance's configured formats
- `TryParse(string input, out DateTime result)` - Safe parsing with instance formats
- `Formats` property - Get/set custom format array (null/empty reverts to defaults)
- `ResetToDefaults()` - Reset instance to use default formats

**Parsing Strategy:**
1. Numeric detection for Unix timestamps and .NET ticks
2. Explicit format matching with both InvariantCulture and CurrentCulture
3. Special handling for Oracle period-separated formats
4. Fallback to DateTime.Parse with built-in intelligence

**Format Precision Hierarchy:**
Formats are ordered from most precise (7-digit/100-nanosecond) to least precise (date-only), covering:
- 7-digit precision: SQL Server datetime2(7)
- 6-digit precision: Microseconds
- 3-digit precision: Milliseconds
- Seconds, minutes, date-only formats
- Timezone-aware formats (Z, +00, -00, zzz, K)
- Database-specific formats (Oracle with periods, MySQL compact, SQL Server)
- Culture-specific formats (US, European, 12/24-hour)

### DateTimeOffsetParser Class (WhatTimeIsIt\DateTimeOffsetParser.cs)

The `DateTimeOffsetParser` class mirrors `DateTimeParser` but returns `DateTimeOffset` instead of `DateTime`, preserving timezone offset information:

**Static Methods:**
- `ParseString(string input)` - Parse using default formats and UTC offset
- `ParseString(string input, string[] formats, TimeSpan defaultOffset)` - Parse with custom formats and offset
- `TryParseString(string input, out DateTimeOffset result)` - Safe parsing with defaults
- `TryParseString(string input, string[] formats, TimeSpan defaultOffset, out DateTimeOffset result)` - Safe parsing with custom formats

**Instance Methods:**
- `Parse(string input)` - Parse using instance's configured formats and default offset
- `TryParse(string input, out DateTimeOffset result)` - Safe parsing with instance configuration
- `Formats` property - Get/set custom format array
- `DefaultOffset` property - Get/set the offset to use for datetime strings without timezone info
- `ResetToDefaults()` - Reset formats and offset to defaults (UTC)

**Key Differences from DateTimeParser:**
1. **Timezone Preservation**: Preserves timezone offsets like +05:00, -08:00, +05:30
2. **DefaultOffset Property**: Configurable offset for datetime strings without timezone information
3. **Smart Detection**: Detects if input has timezone indicator (Z, +, -) and applies different parsing logic:
   - With timezone: Uses `DateTimeOffset.TryParseExact` to preserve the offset
   - Without timezone: Parses as `DateTime` with `DateTimeKind.Unspecified`, then applies `DefaultOffset`
4. **No Local Time Confusion**: Always uses explicit offsets, never system local time

**Parsing Strategy:**
1. Numeric detection for Unix timestamps (always UTC) and .NET ticks
2. Check for timezone indicators (Z, +, -) in the input string
3. If timezone present: Use DateTimeOffset parsing to preserve it
4. If no timezone: Parse as DateTime with Unspecified kind, apply DefaultOffset
5. Oracle special format handling (with timezone detection)
6. Fallback parsing with timezone detection

### Test Suites

All test descriptors live in **Test.Shared** and are grouped into `TestSuiteDescriptor`s exposed via
`WhatTimeIsItSuites.All` (171 cases total, all passing across the CLI, xUnit, and NUnit runners).

**`Test.Shared\DateTimeParserSuites.cs`** — suites for `DateTimeParser`:
- `Static` / `Instance` methods, `FormatProperty`, `TryParse` variants
- `Formats` — every supported precision (7→1 digit), seconds, minutes, date-only, compact, ISO, culture separators
- `Numeric` — Unix seconds/ms (UTC), .NET ticks (Unspecified), and 8/14-digit compact-vs-Unix disambiguation
- `Timezone` — zero-offset indicators (`Z`/`+00`/`-00`/`+00:00`) assert `Kind=Utc`; non-zero offsets assert the
  machine-independent UTC instant (via `ToUniversalTime()`) since .NET converts them to local time
- `Precision`, `Oracle`, `SqlServer`, `EdgeCases`, `Culture` (en-US/en-GB/de-DE), `Negative` (invalid → `FormatException`, null → `ArgumentNullException`)

**`Test.Shared\DateTimeOffsetParserSuites.cs`** — suites for `DateTimeOffsetParser` (fully deterministic
because offsets are explicit):
- `Static` / `Instance` methods, `FormatProperty`, `TryParse` variants
- `TimezonePreservation` — offsets from `-12:00` to `+14:00` (incl. `+05:30`) preserved, not just converted
- `Formats` (with/without timezone), `Numeric` (Unix → zero offset, ticks → default offset)
- `DefaultOffset` behavior (naive input uses the configured offset; explicit offset overrides it)
- `Precision`, `Oracle`, `EdgeCases`, `Culture`, `Negative`

### Determinism notes for test authors

- `DateTime` equality compares ticks only (ignores `Kind`); assert `Kind` separately when it matters.
- Never assert a fixed local wall-clock value for a `DateTimeParser` result parsed from a non-zero offset —
  it depends on the host timezone. Assert `ToUniversalTime()` (the instant) instead.
- For `DateTimeOffset`, assert both instant and offset (`Assert.OffsetEqual` does both).

## Development Notes

### Precision Handling

The library preserves sub-millisecond precision using `DateTime.Ticks`:
- 1 millisecond = 10,000 ticks
- 1 microsecond = 10 ticks
- `.AddTicks()` is used to add microsecond precision beyond milliseconds

Example: `"2024-01-15 14:30:45.123456"` parses as:
```csharp
new DateTime(2024, 1, 15, 14, 30, 45, 123).AddTicks(4560)
```

### Nullable Reference Types

The project uses `<Nullable>enable</Nullable>` but includes `#pragma warning disable CS8625` in DateTimeParser.cs to suppress warnings about null assignments to the formats array.

### Implicit Usings

Both projects use `<ImplicitUsings>enable</ImplicitUsings>`, providing common System namespaces automatically.

## Working with the Codebase

When modifying DateTimeParser:
- Maintain format order from most precise to least precise
- Test with Test.Automated after changes
- Consider timezone handling (AssumeLocal, AdjustToUniversal)
- Verify precision preservation for sub-millisecond values
- Add corresponding test cases for new formats
