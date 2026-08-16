namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Touchstone.Core;
    using WhatTimeIsIt;
    using static Test.Shared.WhatTimeIsItSuites;

    /// <summary>
    /// Exhaustive positive and negative coverage for <see cref="DateTimeParser"/>.
    /// </summary>
    public static class DateTimeParserSuites
    {
        // Canonical instant used throughout: 2024-01-15 14:30:45 with varying sub-second precision.
        private static readonly DateTime Base = new DateTime(2024, 1, 15, 14, 30, 45);

        public static IReadOnlyList<TestSuiteDescriptor> All => new List<TestSuiteDescriptor>
        {
            StaticMethods(),
            InstanceMethods(),
            Formats(),
            Numeric(),
            Timezone(),
            Precision(),
            OracleFormats(),
            SqlServerFormats(),
            EdgeCases(),
            FormatProperty(),
            CultureHandling(),
            TryParseMethods(),
            Negative(),
        };

        private static TestSuiteDescriptor StaticMethods()
        {
            const string s = "DateTimeParser.Static";
            return new TestSuiteDescriptor(s, "DateTimeParser: static methods", new List<TestCaseDescriptor>
            {
                Case(s, "parse-microseconds", "ParseString preserves microseconds", () =>
                    Assert.DateTimeEqual(new DateTime(2024, 1, 15, 14, 30, 45, 123).AddTicks(4560),
                        DateTimeParser.ParseString("2024-01-15 14:30:45.123456"), "ParseString microseconds")),

                Case(s, "parse-custom-formats", "ParseString(input, formats) with a custom format", () =>
                    Assert.DateTimeEqual(new DateTime(2024, 1, 15, 14, 30, 45),
                        DateTimeParser.ParseString("2024/01/15 14:30:45", new[] { "yyyy/MM/dd HH:mm:ss" }),
                        "ParseString custom format")),

                Case(s, "parse-custom-formats-null-uses-defaults", "ParseString(input, null) falls back to defaults", () =>
                    Assert.DateTimeEqual(Base, DateTimeParser.ParseString("2024-01-15 14:30:45", null!),
                        "null formats should use defaults")),

                Case(s, "parse-custom-formats-empty-uses-defaults", "ParseString(input, empty) falls back to defaults", () =>
                    Assert.DateTimeEqual(Base, DateTimeParser.ParseString("2024-01-15 14:30:45", Array.Empty<string>()),
                        "empty formats should use defaults")),

                Case(s, "tryparse-custom-formats-null-uses-defaults", "TryParseString(input, null, out result) falls back to defaults", () =>
                {
                    bool ok = DateTimeParser.TryParseString("2024-01-15 14:30:45", null!, out DateTime r);
                    Assert.True(ok, "null formats should use defaults");
                    Assert.DateTimeEqual(Base, r, "null formats value");
                }),

                Case(s, "tryparse-custom-formats-empty-uses-defaults", "TryParseString(input, empty, out result) falls back to defaults", () =>
                {
                    bool ok = DateTimeParser.TryParseString("2024-01-15 14:30:45", Array.Empty<string>(), out DateTime r);
                    Assert.True(ok, "empty formats should use defaults");
                    Assert.DateTimeEqual(Base, r, "empty formats value");
                }),

                Case(s, "leading-trailing-whitespace", "ParseString trims surrounding whitespace", () =>
                    Assert.DateTimeEqual(Base, DateTimeParser.ParseString("   2024-01-15 14:30:45   "),
                        "whitespace should be trimmed")),
            });
        }

        private static TestSuiteDescriptor InstanceMethods()
        {
            const string s = "DateTimeParser.Instance";
            return new TestSuiteDescriptor(s, "DateTimeParser: instance methods", new List<TestCaseDescriptor>
            {
                Case(s, "instance-default", "Instance Parse with default formats", () =>
                {
                    DateTimeParser parser = new DateTimeParser();
                    Assert.DateTimeEqual(Base, parser.Parse("2024-01-15 14:30:45"), "instance default parse");
                }),

                Case(s, "instance-custom-formats", "Instance Parse honors custom Formats", () =>
                {
                    DateTimeParser parser = new DateTimeParser { Formats = new[] { "dd-MMM-yyyy HH:mm:ss" } };
                    Assert.DateTimeEqual(Base, parser.Parse("15-Jan-2024 14:30:45"), "instance custom format parse");
                }),

                Case(s, "instance-reset", "ResetToDefaults restores default formats", () =>
                {
                    DateTimeParser parser = new DateTimeParser { Formats = new[] { "dd-MMM-yyyy HH:mm:ss" } };
                    parser.ResetToDefaults();
                    Assert.DateTimeEqual(Base, parser.Parse("2024-01-15 14:30:45"), "parse after reset");
                }),

                Case(s, "instance-formats-getter-defaults", "Formats getter returns defaults when unset", () =>
                {
                    DateTimeParser parser = new DateTimeParser();
                    Assert.True(parser.Formats.Length > 0, "default formats should be non-empty");
                }),
            });
        }

        private static TestSuiteDescriptor Formats()
        {
            const string s = "DateTimeParser.Formats";

            // input => expected instant (all Kind-agnostic; compared tick-for-tick)
            Dictionary<string, DateTime> cases = new Dictionary<string, DateTime>
            {
                // 7-digit precision (SQL Server datetime2(7))
                ["2024-01-15 14:30:45.1234567"] = new DateTime(2024, 1, 15, 14, 30, 45, 123).AddTicks(4567),
                ["2024-01-15T14:30:45.1234567"] = new DateTime(2024, 1, 15, 14, 30, 45, 123).AddTicks(4567),
                ["2024-01-15 14:30:45.1234567Z"] = new DateTime(2024, 1, 15, 14, 30, 45, 123).AddTicks(4567),
                ["2024-01-15T14:30:45.1234567Z"] = new DateTime(2024, 1, 15, 14, 30, 45, 123).AddTicks(4567),

                // 6-digit precision (microseconds)
                ["2024-01-15 14:30:45.123456"] = new DateTime(2024, 1, 15, 14, 30, 45, 123).AddTicks(4560),
                ["2024-01-15T14:30:45.123456"] = new DateTime(2024, 1, 15, 14, 30, 45, 123).AddTicks(4560),
                ["2024-01-15 14:30:45.123456Z"] = new DateTime(2024, 1, 15, 14, 30, 45, 123).AddTicks(4560),

                // 5-digit precision
                ["2024-01-15 14:30:45.12345"] = new DateTime(2024, 1, 15, 14, 30, 45, 123).AddTicks(4500),
                ["2024-01-15T14:30:45.12345"] = new DateTime(2024, 1, 15, 14, 30, 45, 123).AddTicks(4500),

                // 4-digit precision
                ["2024-01-15 14:30:45.1234"] = new DateTime(2024, 1, 15, 14, 30, 45, 123).AddTicks(4000),
                ["2024-01-15T14:30:45.1234"] = new DateTime(2024, 1, 15, 14, 30, 45, 123).AddTicks(4000),

                // 3-digit precision (milliseconds)
                ["2024-01-15 14:30:45.123"] = new DateTime(2024, 1, 15, 14, 30, 45, 123),
                ["2024-01-15T14:30:45.123"] = new DateTime(2024, 1, 15, 14, 30, 45, 123),
                ["01/15/2024 14:30:45.123"] = new DateTime(2024, 1, 15, 14, 30, 45, 123),
                ["15/01/2024 14:30:45.123"] = new DateTime(2024, 1, 15, 14, 30, 45, 123),

                // 2-digit precision
                ["2024-01-15 14:30:45.12"] = new DateTime(2024, 1, 15, 14, 30, 45, 120),
                ["2024-01-15T14:30:45.12"] = new DateTime(2024, 1, 15, 14, 30, 45, 120),

                // 1-digit precision
                ["2024-01-15 14:30:45.1"] = new DateTime(2024, 1, 15, 14, 30, 45, 100),
                ["2024-01-15T14:30:45.1"] = new DateTime(2024, 1, 15, 14, 30, 45, 100),

                // Seconds precision, various separators/cultures
                ["2024-01-15 14:30:45"] = Base,
                ["2024-01-15T14:30:45"] = Base,
                ["2024/01/15 14:30:45"] = Base,
                ["2024.01.15 14:30:45"] = Base,
                ["15/01/2024 14:30:45"] = Base,
                ["01/15/2024 14:30:45"] = Base,
                ["15.01.2024 14:30:45"] = Base,
                ["15-01-2024 14:30:45"] = Base,
                ["15-Jan-2024 14:30:45"] = Base,
                ["2024-01-15 2:30:45 PM"] = Base,
                ["2024-01-15 02:30:45 PM"] = Base,

                // RFC 1123 (default format present in the list). 2024-01-15 is a Monday.
                ["Mon, 15 Jan 2024 14:30:45"] = Base,

                // Compact formats
                ["20240115T143045Z"] = Base,
                ["20240115T143045"] = Base,
                ["20240115143045"] = Base,
                ["20240115 14:30:45"] = Base,

                // Minutes precision
                ["2024-01-15 14:30"] = new DateTime(2024, 1, 15, 14, 30, 0),
                ["2024-01-15T14:30"] = new DateTime(2024, 1, 15, 14, 30, 0),
                ["2024/01/15 14:30"] = new DateTime(2024, 1, 15, 14, 30, 0),

                // Date-only formats
                ["2024-01-15"] = new DateTime(2024, 1, 15),
                ["2024/01/15"] = new DateTime(2024, 1, 15),
                ["01/15/2024"] = new DateTime(2024, 1, 15),
                ["15/01/2024"] = new DateTime(2024, 1, 15),
                ["15-Jan-2024"] = new DateTime(2024, 1, 15),
                ["15-Jan-24"] = new DateTime(2024, 1, 15),
                ["20240115"] = new DateTime(2024, 1, 15),
                ["2024.01.15"] = new DateTime(2024, 1, 15),
                ["15.01.2024"] = new DateTime(2024, 1, 15),
            };

            List<TestCaseDescriptor> descriptors = new List<TestCaseDescriptor>();
            int i = 0;
            foreach (KeyValuePair<string, DateTime> kvp in cases)
            {
                string input = kvp.Key;
                DateTime expected = kvp.Value;
                descriptors.Add(Case(s, "fmt-" + (i++), "Format: " + input, () =>
                    Assert.DateTimeEqual(expected, DateTimeParser.ParseString(input), "Format '" + input + "'")));
            }

            return new TestSuiteDescriptor(s, "DateTimeParser: supported format patterns", descriptors);
        }

        private static TestSuiteDescriptor Numeric()
        {
            const string s = "DateTimeParser.Numeric";
            long ticks = Base.Ticks; // 638409258450000000 (18 digits)

            return new TestSuiteDescriptor(s, "DateTimeParser: numeric inputs (Unix / ticks)", new List<TestCaseDescriptor>
            {
                Case(s, "dotnet-ticks", ".NET ticks (18 digits) -> Unspecified", () =>
                {
                    DateTime r = DateTimeParser.ParseString(ticks.ToString());
                    Assert.DateTimeEqual(Base, r, ".NET ticks value");
                    Assert.Kind(DateTimeKind.Unspecified, r, ".NET ticks kind");
                }),

                Case(s, "unix-seconds", "Unix timestamp seconds (10 digits) -> UTC", () =>
                {
                    DateTime r = DateTimeParser.ParseString("1705329045");
                    Assert.DateTimeEqual(new DateTime(2024, 1, 15, 14, 30, 45, DateTimeKind.Utc), r, "Unix seconds value");
                    Assert.Kind(DateTimeKind.Utc, r, "Unix seconds kind");
                }),

                Case(s, "unix-milliseconds", "Unix timestamp milliseconds (13 digits) -> UTC", () =>
                {
                    DateTime r = DateTimeParser.ParseString("1705329045000");
                    Assert.DateTimeEqual(new DateTime(2024, 1, 15, 14, 30, 45, DateTimeKind.Utc), r, "Unix ms value");
                    Assert.Kind(DateTimeKind.Utc, r, "Unix ms kind");
                }),

                Case(s, "eight-digit-not-unix", "8-digit number is a compact date, not a Unix timestamp", () =>
                    Assert.DateTimeEqual(new DateTime(2024, 1, 15), DateTimeParser.ParseString("20240115"),
                        "20240115 should parse as a date")),

                Case(s, "fourteen-digit-not-unix", "14-digit number is a compact datetime, not a Unix timestamp", () =>
                    Assert.DateTimeEqual(Base, DateTimeParser.ParseString("20240115143045"),
                        "20240115143045 should parse as a datetime")),
            });
        }

        private static TestSuiteDescriptor Timezone()
        {
            const string s = "DateTimeParser.Timezone";
            DateTime utcInstant = new DateTime(2024, 1, 15, 14, 30, 45, DateTimeKind.Utc);

            List<TestCaseDescriptor> descriptors = new List<TestCaseDescriptor>();

            // Zero-offset indicators must yield Kind=Utc with the wall-clock time preserved.
            foreach (string input in new[]
            {
                "2024-01-15T14:30:45Z",
                "2024-01-15 14:30:45Z",
                "2024-01-15T14:30:45+00",
                "2024-01-15T14:30:45-00",
                "2024-01-15T14:30:45+00:00",
            })
            {
                string local = input;
                descriptors.Add(Case(s, "utc-" + descriptors.Count, "UTC indicator -> Kind=Utc: " + local, () =>
                {
                    DateTime r = DateTimeParser.ParseString(local);
                    Assert.DateTimeEqual(utcInstant, r, "value for '" + local + "'");
                    Assert.Kind(DateTimeKind.Utc, r, "kind for '" + local + "'");
                }));
            }

            // Non-zero offsets are converted to local time by .NET, so assert the machine-independent UTC instant.
            (string Input, DateTime ExpectedUtc)[] offsets = new[]
            {
                ("2024-01-15T14:30:45+05:00", new DateTime(2024, 1, 15, 9, 30, 45, DateTimeKind.Utc)),
                ("2024-01-15T14:30:45-08:00", new DateTime(2024, 1, 15, 22, 30, 45, DateTimeKind.Utc)),
                ("2024-01-15 14:30:45+05:30", new DateTime(2024, 1, 15, 9, 0, 45, DateTimeKind.Utc)),
            };
            foreach ((string input, DateTime expectedUtc) in offsets)
            {
                string local = input;
                DateTime exp = expectedUtc;
                descriptors.Add(Case(s, "off-" + descriptors.Count, "Non-zero offset preserves instant: " + local, () =>
                {
                    DateTime r = DateTimeParser.ParseString(local);
                    Assert.DateTimeEqual(exp, r.ToUniversalTime(), "UTC instant for '" + local + "'");
                }));
            }

            return new TestSuiteDescriptor(s, "DateTimeParser: timezone handling", descriptors);
        }

        private static TestSuiteDescriptor Precision()
        {
            const string s = "DateTimeParser.Precision";
            return new TestSuiteDescriptor(s, "DateTimeParser: sub-millisecond precision", new List<TestCaseDescriptor>
            {
                Case(s, "microseconds", "Microsecond precision is preserved (.123456)", () =>
                    Assert.DateTimeEqual(new DateTime(2024, 1, 15, 14, 30, 45, 123).AddTicks(4560),
                        DateTimeParser.ParseString("2024-01-15 14:30:45.123456"), "microsecond precision")),

                Case(s, "hundred-nanoseconds", "100-ns (7-digit) precision is preserved (.1234567)", () =>
                    Assert.DateTimeEqual(new DateTime(2024, 1, 15, 14, 30, 45, 123).AddTicks(4567),
                        DateTimeParser.ParseString("2024-01-15 14:30:45.1234567"), "100-ns precision")),

                Case(s, "milliseconds", "Millisecond precision is preserved (.123)", () =>
                    Assert.DateTimeEqual(new DateTime(2024, 1, 15, 14, 30, 45, 123),
                        DateTimeParser.ParseString("2024-01-15 14:30:45.123"), "millisecond precision")),
            });
        }

        private static TestSuiteDescriptor OracleFormats()
        {
            const string s = "DateTimeParser.Oracle";
            DateTime micros = new DateTime(2024, 1, 15, 14, 30, 45, 123).AddTicks(4560);
            return new TestSuiteDescriptor(s, "DateTimeParser: Oracle formats", new List<TestCaseDescriptor>
            {
                Case(s, "oracle-12h-micros", "Oracle 12-hour with microseconds (dd-MMM-yy hh.mm.ss.ffffff tt)", () =>
                    Assert.DateTimeEqual(micros, DateTimeParser.ParseString("15-Jan-24 02.30.45.123456 PM"), "Oracle 12h micros")),

                Case(s, "oracle-24h-micros", "Oracle 24-hour with microseconds (dd-MMM-yy HH.mm.ss.ffffff)", () =>
                    Assert.DateTimeEqual(micros, DateTimeParser.ParseString("15-Jan-24 14.30.45.123456"), "Oracle 24h micros")),

                Case(s, "oracle-period-fallback", "Oracle period-separated time falls back to normalized parse", () =>
                    Assert.DateTimeEqual(Base, DateTimeParser.ParseString("15-Jan-24 14.30.45"), "Oracle period fallback")),
            });
        }

        private static TestSuiteDescriptor SqlServerFormats()
        {
            const string s = "DateTimeParser.SqlServer";
            DateTime millis = new DateTime(2024, 1, 15, 14, 30, 45, 123);
            return new TestSuiteDescriptor(s, "DateTimeParser: SQL Server formats", new List<TestCaseDescriptor>
            {
                Case(s, "sqlserver-12h", "SQL Server 'MMM dd yyyy hh:mm:ss:ffftt'", () =>
                    Assert.DateTimeEqual(millis, DateTimeParser.ParseString("Jan 15 2024 02:30:45:123PM"), "SQL Server 12h")),

                Case(s, "sqlserver-12h-single-digit-hour", "SQL Server double-space single-digit hour", () =>
                    Assert.DateTimeEqual(millis, DateTimeParser.ParseString("Jan 15 2024  2:30:45:123PM"), "SQL Server single-digit hour")),

                Case(s, "sqlserver-24h", "SQL Server 'MMM dd yyyy HH:mm:ss:fff'", () =>
                    Assert.DateTimeEqual(millis, DateTimeParser.ParseString("Jan 15 2024 14:30:45:123"), "SQL Server 24h")),
            });
        }

        private static TestSuiteDescriptor EdgeCases()
        {
            const string s = "DateTimeParser.EdgeCases";
            return new TestSuiteDescriptor(s, "DateTimeParser: edge cases", new List<TestCaseDescriptor>
            {
                Case(s, "leap-year", "Leap-year date (2024-02-29)", () =>
                    Assert.DateTimeEqual(new DateTime(2024, 2, 29), DateTimeParser.ParseString("2024-02-29"), "leap year")),

                Case(s, "end-of-year", "End of year with microseconds", () =>
                    Assert.DateTimeEqual(new DateTime(2024, 12, 31, 23, 59, 59, 999).AddTicks(9990),
                        DateTimeParser.ParseString("2024-12-31 23:59:59.999999"), "end of year")),

                Case(s, "start-of-year", "Start of year (zeros in sub-second)", () =>
                    Assert.DateTimeEqual(new DateTime(2024, 1, 1, 0, 0, 0),
                        DateTimeParser.ParseString("2024-01-01 00:00:00.000000"), "start of year")),

                Case(s, "single-digit-month-day", "Single-digit month/day with 12-hour time", () =>
                    Assert.DateTimeEqual(new DateTime(2024, 1, 5, 15, 30, 45),
                        DateTimeParser.ParseString("1/5/2024 3:30:45 PM"), "single digit m/d")),
            });
        }

        private static TestSuiteDescriptor FormatProperty()
        {
            const string s = "DateTimeParser.FormatProperty";
            return new TestSuiteDescriptor(s, "DateTimeParser: Formats property behavior", new List<TestCaseDescriptor>
            {
                Case(s, "null-uses-defaults", "Formats=null reverts to defaults", () =>
                {
                    DateTimeParser parser = new DateTimeParser { Formats = null! };
                    Assert.DateTimeEqual(Base, parser.Parse("2024-01-15 14:30:45"), "null formats");
                }),

                Case(s, "empty-uses-defaults", "Formats=empty reverts to defaults", () =>
                {
                    DateTimeParser parser = new DateTimeParser { Formats = Array.Empty<string>() };
                    Assert.DateTimeEqual(Base, parser.Parse("2024-01-15 14:30:45"), "empty formats");
                }),

                Case(s, "custom-restricts", "Custom single format parses matching input", () =>
                {
                    DateTimeParser parser = new DateTimeParser { Formats = new[] { "yyyy-MM-dd" } };
                    Assert.DateTimeEqual(new DateTime(2024, 1, 15), parser.Parse("2024-01-15"), "custom date-only");
                }),

                Case(s, "default-formats-is-clone", "DefaultFormats returns an independent clone", () =>
                {
                    string[] defaults = DateTimeParser.DefaultFormats;
                    Assert.True(defaults.Length > 0, "defaults present");
                    defaults[0] = "modified";
                    // Mutating the returned array must not affect subsequent parses.
                    Assert.DateTimeEqual(new DateTime(2024, 1, 15, 14, 30, 45, 123).AddTicks(4567),
                        DateTimeParser.ParseString("2024-01-15 14:30:45.1234567"), "clone independence");
                }),
            });
        }

        private static TestSuiteDescriptor CultureHandling()
        {
            const string s = "DateTimeParser.Culture";
            return new TestSuiteDescriptor(s, "DateTimeParser: culture handling", new List<TestCaseDescriptor>
            {
                Case(s, "en-US", "US-culture date (MM/dd/yyyy)", () => WithCulture("en-US", () =>
                    Assert.DateTimeEqual(new DateTime(2024, 1, 15), DateTimeParser.ParseString("01/15/2024"), "en-US"))),

                Case(s, "en-GB", "UK-culture date (dd/MM/yyyy)", () => WithCulture("en-GB", () =>
                    Assert.DateTimeEqual(new DateTime(2024, 1, 15), DateTimeParser.ParseString("15/01/2024"), "en-GB"))),

                Case(s, "de-DE", "German-culture date (dd.MM.yyyy)", () => WithCulture("de-DE", () =>
                    Assert.DateTimeEqual(new DateTime(2024, 1, 15), DateTimeParser.ParseString("15.01.2024"), "de-DE"))),
            });
        }

        private static TestSuiteDescriptor TryParseMethods()
        {
            const string s = "DateTimeParser.TryParse";
            return new TestSuiteDescriptor(s, "DateTimeParser: TryParse variants", new List<TestCaseDescriptor>
            {
                Case(s, "static-valid", "Static TryParseString succeeds on valid input", () =>
                {
                    bool ok = DateTimeParser.TryParseString("2024-01-15 14:30:45", out DateTime r);
                    Assert.True(ok, "should succeed");
                    Assert.DateTimeEqual(Base, r, "value");
                }),

                Case(s, "static-invalid", "Static TryParseString fails and yields MinValue", () =>
                {
                    bool ok = DateTimeParser.TryParseString("invalid-date", out DateTime r);
                    Assert.False(ok, "should fail");
                    Assert.DateTimeEqual(DateTime.MinValue, r, "MinValue on failure");
                }),

                Case(s, "static-null", "Static TryParseString returns false for null", () =>
                {
                    bool ok = DateTimeParser.TryParseString(null!, out DateTime r);
                    Assert.False(ok, "null should fail");
                    Assert.DateTimeEqual(DateTime.MinValue, r, "MinValue for null");
                }),

                Case(s, "static-empty", "Static TryParseString returns false for empty", () =>
                    Assert.False(DateTimeParser.TryParseString("", out _), "empty should fail")),

                Case(s, "static-whitespace", "Static TryParseString returns false and MinValue for whitespace", () =>
                {
                    bool ok = DateTimeParser.TryParseString("   ", out DateTime r);
                    Assert.False(ok, "whitespace should fail");
                    Assert.DateTimeEqual(DateTime.MinValue, r, "MinValue for whitespace");
                }),

                Case(s, "instance-valid", "Instance TryParse succeeds on valid input", () =>
                {
                    DateTimeParser parser = new DateTimeParser();
                    bool ok = parser.TryParse("2024-01-15", out DateTime r);
                    Assert.True(ok, "should succeed");
                    Assert.DateTimeEqual(new DateTime(2024, 1, 15), r, "value");
                }),

                Case(s, "instance-invalid", "Instance TryParse fails on invalid input", () =>
                {
                    DateTimeParser parser = new DateTimeParser();
                    Assert.False(parser.TryParse("not-a-date", out _), "should fail");
                }),

                Case(s, "custom-formats", "TryParseString with custom formats", () =>
                {
                    bool ok = DateTimeParser.TryParseString("15-Jan-2024", new[] { "dd-MMM-yyyy" }, out DateTime r);
                    Assert.True(ok, "should succeed");
                    Assert.DateTimeEqual(new DateTime(2024, 1, 15), r, "value");
                }),
            });
        }

        private static TestSuiteDescriptor Negative()
        {
            const string s = "DateTimeParser.Negative";
            List<TestCaseDescriptor> descriptors = new List<TestCaseDescriptor>
            {
                Case(s, "null-throws-ane", "ParseString(null) throws ArgumentNullException", () =>
                    Assert.Throws<ArgumentNullException>(() => DateTimeParser.ParseString(null!), "null input")),
            };

            string[] invalid = new[]
            {
                "",
                "   ",
                "not-a-date",
                "2024-13-01",       // invalid month
                "2024-01-32",       // invalid day
                "2024-02-30",       // invalid day for February
                "2023-02-29",       // not a leap year
                "2024-01-15 25:00:00", // invalid hour
                "2024-01-15 14:60:00", // invalid minute
                "2024-01-15 14:30:60", // invalid second
                "abc123xyz",
                "2024/01/15/14/30/45", // too many separators
                "2024--01--15",
                "99999999999999999999999999", // numeric overflow
            };

            int i = 0;
            foreach (string bad in invalid)
            {
                string local = bad;
                descriptors.Add(Case(s, "invalid-" + (i++), "Invalid input throws FormatException: '" + bad + "'", () =>
                    Assert.Throws<FormatException>(() => DateTimeParser.ParseString(local), "invalid input '" + local + "'")));
            }

            return new TestSuiteDescriptor(s, "DateTimeParser: negative cases", descriptors);
        }

        private static void WithCulture(string culture, Action body)
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo(culture);
                body();
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }
    }
}
