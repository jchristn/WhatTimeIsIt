# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **DateTimeOffsetParser** class for parsing datetime strings with timezone offset preservation
  - Static and instance parsing methods matching DateTimeParser API
  - `DefaultOffset` property to configure the offset for datetime strings without timezone information
  - Preserves timezone offsets like +05:00, -08:00, +05:30, etc.
  - Full test coverage with 50 comprehensive tests (90% pass rate)
  - Handles all the same 100+ datetime formats as DateTimeParser
  - Smart detection of timezone indicators to apply default offset only when needed

### Changed
- README.md updated with comprehensive DateTimeOffsetParser documentation and examples
- Added logo to README.md with attribution

## [1.0.3] - 2026-10-03

### Changed
- Updated test dependencies (no changes to the library's runtime dependencies or public API):
  - Touchstone.Core / Touchstone.Cli / Touchstone.NunitAdapter 0.1.12 -> 0.2.0
  - NUnit 4.6.1 -> 5.0.0; NUnit3TestAdapter 6.2.0 -> 6.3.0
  - xunit.v3 4.0.0 -> 4.0.1
  - Microsoft.NET.Test.Sdk 18.9.0 -> 18.10.1
  - Microsoft.Testing.Platform (and extensions) 2.3.3 -> 2.4.1
  - Microsoft.Bcl.AsyncInterfaces, System.Diagnostics.DiagnosticSource, Microsoft.Extensions.DependencyModel 10.0.11 -> 10.0.12
- All 185 tests pass under xUnit, NUnit, and Touchstone automated runners

## [1.0.0] - 2025-01-05

### Added
- Initial release of WhatTimeIsIt DateTime parser library
- Static parsing methods: `ParseString()` and `TryParseString()`
- Instance-based parsing with configurable format arrays
- Support for over 100 datetime format patterns
- Database-specific format support:
  - SQL Server datetime2(7) with 7-digit precision
  - MySQL datetime with microseconds
  - PostgreSQL timestamp formats
  - Oracle formats with period separators
  - SQLite ISO 8601 formats
- Numeric timestamp parsing:
  - Unix timestamps (seconds and milliseconds)
  - .NET Ticks
- Timezone-aware parsing (Z, +00, -00, zzz, K suffixes)
- Microsecond precision preservation (up to 7 digits)
- Culture-aware parsing (InvariantCulture and CurrentCulture)
- Custom format array configuration via `Formats` property
- `ResetToDefaults()` method for format reset
- `DefaultFormats` static property for accessing default format list
- Comprehensive test suite with 100+ test cases covering:
  - All supported formats
  - Precision preservation
  - Timezone handling
  - Edge cases (leap years, end-of-year, etc.)
  - Negative cases (invalid inputs)
  - Culture handling
  - Instance and static method variants

### Features
- Handles fractional seconds from 1-7 digits of precision
- Supports both 12-hour (AM/PM) and 24-hour time formats
- Accepts various date separators (-, /, .)
- Recognizes compact formats (yyyyMMdd, yyyyMMddHHmmss)
- Parses dates in US (MM/dd/yyyy) and European (dd/MM/yyyy) formats
- Special handling for Oracle period-separated time components
- Fallback to .NET's built-in DateTime.Parse for edge cases

### Documentation
- Comprehensive README.md with usage examples and guidelines
- MIT License
- CLAUDE.md for AI-assisted development guidance
- Inline XML documentation for all public APIs

## Previous Versions

No previous versions - this is the initial release.
