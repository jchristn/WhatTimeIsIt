namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Touchstone.Core;
    using WhatTimeIsIt;
    using static Test.Shared.WhatTimeIsItSuites;

    /// <summary>
    /// Exhaustive positive and negative coverage for <see cref="DateTimeOffsetParser"/>. Unlike
    /// <see cref="DateTimeParser"/>, these results are fully deterministic because offsets are explicit.
    /// </summary>
    public static class DateTimeOffsetParserSuites
    {
        public static IReadOnlyList<TestSuiteDescriptor> All => new List<TestSuiteDescriptor>
        {
            StaticMethods(),
            InstanceMethods(),
            TimezonePreservation(),
            Formats(),
            Numeric(),
            DefaultOffsetBehavior(),
            Precision(),
            OracleFormats(),
            EdgeCases(),
            FormatProperty(),
            CultureHandling(),
            TryParseMethods(),
            Negative(),
        };

        private static TestSuiteDescriptor StaticMethods()
        {
            const string s = "DateTimeOffsetParser.Static";
            return new TestSuiteDescriptor(s, "DateTimeOffsetParser: static methods", new List<TestCaseDescriptor>
            {
                Case(s, "parse-utc", "ParseString with Z suffix -> zero offset", () =>
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.Zero),
                        DateTimeOffsetParser.ParseString("2024-01-15T14:30:45Z"), "UTC static parse")),

                Case(s, "parse-plus5", "ParseString with +05:00 offset", () =>
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.FromHours(5)),
                        DateTimeOffsetParser.ParseString("2024-01-15T14:30:45+05:00"), "+05:00 static parse")),

                Case(s, "parse-custom-formats", "ParseString(input, formats, offset) with custom format", () =>
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.Zero),
                        DateTimeOffsetParser.ParseString("2024/01/15 14:30:45", new[] { "yyyy/MM/dd HH:mm:ss" }, TimeSpan.Zero),
                        "custom format static parse")),

                Case(s, "parse-custom-offset-applied", "Custom default offset applied to naive input", () =>
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.FromHours(3)),
                        DateTimeOffsetParser.ParseString("2024-01-15 14:30:45", DateTimeOffsetParser.DefaultFormats, TimeSpan.FromHours(3)),
                        "custom default offset")),

                Case(s, "tryparse-custom-formats-null-uses-defaults", "TryParseString(input, null, offset, out result) falls back to defaults", () =>
                {
                    bool ok = DateTimeOffsetParser.TryParseString("2024-01-15 14:30:45", null!, TimeSpan.FromHours(2), out DateTimeOffset r);
                    Assert.True(ok, "null formats should use defaults");
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.FromHours(2)), r, "null formats value");
                }),

                Case(s, "tryparse-custom-formats-empty-uses-defaults", "TryParseString(input, empty, offset, out result) falls back to defaults", () =>
                {
                    bool ok = DateTimeOffsetParser.TryParseString("2024-01-15 14:30:45", Array.Empty<string>(), TimeSpan.FromHours(-7), out DateTimeOffset r);
                    Assert.True(ok, "empty formats should use defaults");
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.FromHours(-7)), r, "empty formats value");
                }),
            });
        }

        private static TestSuiteDescriptor InstanceMethods()
        {
            const string s = "DateTimeOffsetParser.Instance";
            return new TestSuiteDescriptor(s, "DateTimeOffsetParser: instance methods", new List<TestCaseDescriptor>
            {
                Case(s, "instance-utc", "Instance Parse with UTC input", () =>
                {
                    DateTimeOffsetParser parser = new DateTimeOffsetParser();
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.Zero),
                        parser.Parse("2024-01-15T14:30:45Z"), "instance UTC");
                }),

                Case(s, "instance-custom-formats-and-offset", "Instance Parse with custom formats and PST default offset", () =>
                {
                    DateTimeOffsetParser parser = new DateTimeOffsetParser
                    {
                        Formats = new[] { "dd-MMM-yyyy HH:mm:ss" },
                        DefaultOffset = TimeSpan.FromHours(-8)
                    };
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.FromHours(-8)),
                        parser.Parse("15-Jan-2024 14:30:45"), "instance custom formats + offset");
                }),

                Case(s, "instance-reset", "ResetToDefaults restores formats and zero offset", () =>
                {
                    DateTimeOffsetParser parser = new DateTimeOffsetParser
                    {
                        Formats = new[] { "dd-MMM-yyyy HH:mm:ss" },
                        DefaultOffset = TimeSpan.FromHours(-8)
                    };
                    parser.ResetToDefaults();
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.Zero),
                        parser.Parse("2024-01-15 14:30:45"), "parse after reset");
                }),
            });
        }

        private static TestSuiteDescriptor TimezonePreservation()
        {
            const string s = "DateTimeOffsetParser.TimezonePreservation";
            (string Input, TimeSpan Offset)[] cases = new[]
            {
                ("2024-01-15T14:30:45+00:00", TimeSpan.Zero),
                ("2024-01-15T14:30:45+05:30", TimeSpan.FromMinutes(330)),
                ("2024-01-15T14:30:45-08:00", TimeSpan.FromHours(-8)),
                ("2024-01-15T14:30:45+09:00", TimeSpan.FromHours(9)),
                ("2024-01-15T14:30:45-05:00", TimeSpan.FromHours(-5)),
                ("2024-01-15T14:30:45+01:00", TimeSpan.FromHours(1)),
                ("2024-01-15T14:30:45+14:00", TimeSpan.FromHours(14)),
                ("2024-01-15T14:30:45-12:00", TimeSpan.FromHours(-12)),
            };

            List<TestCaseDescriptor> descriptors = new List<TestCaseDescriptor>();
            foreach ((string input, TimeSpan offset) in cases)
            {
                string local = input;
                TimeSpan off = offset;
                descriptors.Add(Case(s, "tz-" + descriptors.Count, "Offset preserved: " + local, () =>
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, off),
                        DateTimeOffsetParser.ParseString(local), "offset for '" + local + "'")));
            }

            return new TestSuiteDescriptor(s, "DateTimeOffsetParser: timezone offset preservation", descriptors);
        }

        private static TestSuiteDescriptor Formats()
        {
            const string s = "DateTimeOffsetParser.Formats";
            Dictionary<string, DateTimeOffset> cases = new Dictionary<string, DateTimeOffset>
            {
                // 7-digit precision with timezones
                ["2024-01-15T14:30:45.1234567Z"] = new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.Zero).AddTicks(1234567),
                ["2024-01-15T14:30:45.1234567+05:00"] = new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.FromHours(5)).AddTicks(1234567),

                // 6-digit precision with timezones
                ["2024-01-15T14:30:45.123456Z"] = new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.Zero).AddTicks(1234560),
                ["2024-01-15T14:30:45.123456-08:00"] = new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.FromHours(-8)).AddTicks(1234560),

                // 3-digit precision with timezones
                ["2024-01-15T14:30:45.123Z"] = new DateTimeOffset(2024, 1, 15, 14, 30, 45, 123, TimeSpan.Zero),
                ["2024-01-15T14:30:45.123+01:00"] = new DateTimeOffset(2024, 1, 15, 14, 30, 45, 123, TimeSpan.FromHours(1)),

                // Seconds precision with timezone
                ["2024-01-15T14:30:45Z"] = new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.Zero),
                ["2024-01-15T14:30:45+00:00"] = new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.Zero),

                // No timezone -> default (zero) offset
                ["2024-01-15 14:30:45"] = new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.Zero),
                ["2024-01-15T14:30:45"] = new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.Zero),

                // RFC 1123 (default formats present in the list). 2024-01-15 is a Monday.
                ["Mon, 15 Jan 2024 14:30:45"] = new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.Zero),
                ["Mon, 15 Jan 2024 14:30:45 +05:00"] = new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.FromHours(5)),

                // Date-only
                ["2024-01-15"] = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero),
            };

            List<TestCaseDescriptor> descriptors = new List<TestCaseDescriptor>();
            int i = 0;
            foreach (KeyValuePair<string, DateTimeOffset> kvp in cases)
            {
                string input = kvp.Key;
                DateTimeOffset expected = kvp.Value;
                descriptors.Add(Case(s, "fmt-" + (i++), "Format: " + input, () =>
                    Assert.OffsetEqual(expected, DateTimeOffsetParser.ParseString(input), "Format '" + input + "'")));
            }

            return new TestSuiteDescriptor(s, "DateTimeOffsetParser: supported format patterns", descriptors);
        }

        private static TestSuiteDescriptor Numeric()
        {
            const string s = "DateTimeOffsetParser.Numeric";
            long ticks = new DateTime(2024, 1, 15, 14, 30, 45).Ticks;
            return new TestSuiteDescriptor(s, "DateTimeOffsetParser: numeric inputs", new List<TestCaseDescriptor>
            {
                Case(s, "unix-seconds", "Unix seconds -> UTC (zero offset)", () =>
                    Assert.OffsetEqual(DateTimeOffset.FromUnixTimeSeconds(1705329045),
                        DateTimeOffsetParser.ParseString("1705329045"), "unix seconds")),

                Case(s, "unix-milliseconds", "Unix milliseconds -> UTC (zero offset)", () =>
                    Assert.OffsetEqual(DateTimeOffset.FromUnixTimeMilliseconds(1705329045000),
                        DateTimeOffsetParser.ParseString("1705329045000"), "unix ms")),

                Case(s, "dotnet-ticks", ".NET ticks -> default (zero) offset", () =>
                    Assert.OffsetEqual(new DateTimeOffset(new DateTime(ticks), TimeSpan.Zero),
                        DateTimeOffsetParser.ParseString(ticks.ToString()), "ticks")),
            });
        }

        private static TestSuiteDescriptor DefaultOffsetBehavior()
        {
            const string s = "DateTimeOffsetParser.DefaultOffset";
            return new TestSuiteDescriptor(s, "DateTimeOffsetParser: DefaultOffset behavior", new List<TestCaseDescriptor>
            {
                Case(s, "default-utc", "Naive input uses zero default offset", () =>
                {
                    DateTimeOffsetParser parser = new DateTimeOffsetParser { DefaultOffset = TimeSpan.Zero };
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.Zero),
                        parser.Parse("2024-01-15 14:30:45"), "default UTC");
                }),

                Case(s, "default-pst", "Naive input uses configured PST default offset", () =>
                {
                    DateTimeOffsetParser parser = new DateTimeOffsetParser { DefaultOffset = TimeSpan.FromHours(-8) };
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.FromHours(-8)),
                        parser.Parse("2024-01-15 14:30:45"), "default PST");
                }),

                Case(s, "explicit-overrides-default", "Explicit offset in input overrides default offset", () =>
                {
                    DateTimeOffsetParser parser = new DateTimeOffsetParser { DefaultOffset = TimeSpan.FromHours(-8) };
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.FromHours(5)),
                        parser.Parse("2024-01-15T14:30:45+05:00"), "explicit overrides default");
                }),
            });
        }

        private static TestSuiteDescriptor Precision()
        {
            const string s = "DateTimeOffsetParser.Precision";
            return new TestSuiteDescriptor(s, "DateTimeOffsetParser: sub-millisecond precision", new List<TestCaseDescriptor>
            {
                Case(s, "microseconds", "Microsecond precision preserved with offset", () =>
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.Zero).AddTicks(1234560),
                        DateTimeOffsetParser.ParseString("2024-01-15T14:30:45.123456Z"), "microseconds")),

                Case(s, "hundred-nanoseconds", "100-ns precision preserved with offset", () =>
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.Zero).AddTicks(1234567),
                        DateTimeOffsetParser.ParseString("2024-01-15T14:30:45.1234567Z"), "100-ns")),

                Case(s, "microseconds-with-offset", "Microsecond precision preserved with non-zero offset", () =>
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.FromHours(-8)).AddTicks(1234560),
                        DateTimeOffsetParser.ParseString("2024-01-15T14:30:45.123456-08:00"), "microseconds + offset")),
            });
        }

        private static TestSuiteDescriptor OracleFormats()
        {
            const string s = "DateTimeOffsetParser.Oracle";
            return new TestSuiteDescriptor(s, "DateTimeOffsetParser: Oracle formats", new List<TestCaseDescriptor>
            {
                Case(s, "oracle-24h-micros", "Oracle 24-hour with microseconds (naive -> zero offset)", () =>
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.Zero).AddTicks(1234560),
                        DateTimeOffsetParser.ParseString("15-Jan-24 14.30.45.123456"), "Oracle 24h micros")),

                Case(s, "oracle-period-fallback", "Oracle period-separated time falls back (naive -> zero offset)", () =>
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.Zero),
                        DateTimeOffsetParser.ParseString("15-Jan-24 14.30.45"), "Oracle period fallback")),
            });
        }

        private static TestSuiteDescriptor EdgeCases()
        {
            const string s = "DateTimeOffsetParser.EdgeCases";
            return new TestSuiteDescriptor(s, "DateTimeOffsetParser: edge cases", new List<TestCaseDescriptor>
            {
                Case(s, "leap-year", "Leap-year date with Z", () =>
                    Assert.OffsetEqual(new DateTimeOffset(2024, 2, 29, 0, 0, 0, TimeSpan.Zero),
                        DateTimeOffsetParser.ParseString("2024-02-29T00:00:00Z"), "leap year")),

                Case(s, "end-of-year", "End of year with microseconds and Z", () =>
                    Assert.OffsetEqual(new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero).AddTicks(9999990),
                        DateTimeOffsetParser.ParseString("2024-12-31T23:59:59.999999Z"), "end of year")),

                Case(s, "start-of-year", "Start of year with Z", () =>
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                        DateTimeOffsetParser.ParseString("2024-01-01T00:00:00Z"), "start of year")),
            });
        }

        private static TestSuiteDescriptor FormatProperty()
        {
            const string s = "DateTimeOffsetParser.FormatProperty";
            return new TestSuiteDescriptor(s, "DateTimeOffsetParser: Formats property behavior", new List<TestCaseDescriptor>
            {
                Case(s, "null-uses-defaults", "Formats=null reverts to defaults", () =>
                {
                    DateTimeOffsetParser parser = new DateTimeOffsetParser { Formats = null! };
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.Zero),
                        parser.Parse("2024-01-15T14:30:45Z"), "null formats");
                }),

                Case(s, "empty-uses-defaults", "Formats=empty reverts to defaults", () =>
                {
                    DateTimeOffsetParser parser = new DateTimeOffsetParser { Formats = Array.Empty<string>() };
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.Zero),
                        parser.Parse("2024-01-15T14:30:45Z"), "empty formats");
                }),

                Case(s, "default-formats-is-clone", "DefaultFormats returns an independent clone", () =>
                {
                    string[] defaults = DateTimeOffsetParser.DefaultFormats;
                    Assert.True(defaults.Length > 0, "defaults present");
                    defaults[0] = "modified";
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.Zero).AddTicks(1234567),
                        DateTimeOffsetParser.ParseString("2024-01-15T14:30:45.1234567Z"), "clone independence");
                }),
            });
        }

        private static TestSuiteDescriptor CultureHandling()
        {
            const string s = "DateTimeOffsetParser.Culture";
            return new TestSuiteDescriptor(s, "DateTimeOffsetParser: culture handling", new List<TestCaseDescriptor>
            {
                Case(s, "en-US", "US-culture date (MM/dd/yyyy)", () => WithCulture("en-US", () =>
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero),
                        DateTimeOffsetParser.ParseString("01/15/2024"), "en-US"))),

                Case(s, "en-GB", "UK-culture date (dd/MM/yyyy)", () => WithCulture("en-GB", () =>
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero),
                        DateTimeOffsetParser.ParseString("15/01/2024"), "en-GB"))),
            });
        }

        private static TestSuiteDescriptor TryParseMethods()
        {
            const string s = "DateTimeOffsetParser.TryParse";
            return new TestSuiteDescriptor(s, "DateTimeOffsetParser: TryParse variants", new List<TestCaseDescriptor>
            {
                Case(s, "static-valid", "Static TryParseString succeeds on UTC input", () =>
                {
                    bool ok = DateTimeOffsetParser.TryParseString("2024-01-15T14:30:45Z", out DateTimeOffset r);
                    Assert.True(ok, "should succeed");
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.Zero), r, "value");
                }),

                Case(s, "static-invalid", "Static TryParseString fails and yields MinValue", () =>
                {
                    bool ok = DateTimeOffsetParser.TryParseString("invalid-date", out DateTimeOffset r);
                    Assert.False(ok, "should fail");
                    Assert.OffsetEqual(DateTimeOffset.MinValue, r, "MinValue on failure");
                }),

                Case(s, "static-null", "Static TryParseString returns false for null", () =>
                    Assert.False(DateTimeOffsetParser.TryParseString(null!, out _), "null should fail")),

                Case(s, "static-whitespace", "Static TryParseString returns false and MinValue for whitespace", () =>
                {
                    bool ok = DateTimeOffsetParser.TryParseString("   ", out DateTimeOffset r);
                    Assert.False(ok, "whitespace should fail");
                    Assert.OffsetEqual(DateTimeOffset.MinValue, r, "MinValue for whitespace");
                }),

                Case(s, "instance-valid", "Instance TryParse preserves offset", () =>
                {
                    DateTimeOffsetParser parser = new DateTimeOffsetParser();
                    bool ok = parser.TryParse("2024-01-15T14:30:45+05:00", out DateTimeOffset r);
                    Assert.True(ok, "should succeed");
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.FromHours(5)), r, "value");
                }),

                Case(s, "instance-invalid", "Instance TryParse fails on invalid input", () =>
                {
                    DateTimeOffsetParser parser = new DateTimeOffsetParser();
                    Assert.False(parser.TryParse("not-a-date", out _), "should fail");
                }),

                Case(s, "custom-formats-offset", "TryParseString with custom formats and default offset", () =>
                {
                    // A naive (timezone-less) input parsed with a custom format receives the supplied default offset.
                    bool ok = DateTimeOffsetParser.TryParseString("2024/01/15 14:30:45", new[] { "yyyy/MM/dd HH:mm:ss" }, TimeSpan.FromHours(2), out DateTimeOffset r);
                    Assert.True(ok, "should succeed");
                    Assert.OffsetEqual(new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.FromHours(2)), r, "value");
                }),
            });
        }

        private static TestSuiteDescriptor Negative()
        {
            const string s = "DateTimeOffsetParser.Negative";
            List<TestCaseDescriptor> descriptors = new List<TestCaseDescriptor>
            {
                Case(s, "null-throws-ane", "ParseString(null) throws ArgumentNullException", () =>
                    Assert.Throws<ArgumentNullException>(() => DateTimeOffsetParser.ParseString(null!), "null input")),
            };

            string[] invalid = new[]
            {
                "",
                "   ",
                "not-a-date",
                "2024-13-01",           // invalid month
                "2024-01-32",           // invalid day
                "2024-02-30",           // invalid day for February
                "2023-02-29",           // not a leap year
                "2024-01-15 25:00:00",  // invalid hour
                "2024-01-15 14:60:00",  // invalid minute
                "2024-01-15 14:30:60",  // invalid second
                "abc123xyz",
                "2024/01/15/14/30/45",  // too many separators
                "2024--01--15",
                "99999999999999999999999999", // numeric overflow (not a valid tick/Unix value)
            };

            int i = 0;
            foreach (string bad in invalid)
            {
                string local = bad;
                descriptors.Add(Case(s, "invalid-" + (i++), "Invalid input throws FormatException: '" + bad + "'", () =>
                    Assert.Throws<FormatException>(() => DateTimeOffsetParser.ParseString(local), "invalid input '" + local + "'")));
            }

            return new TestSuiteDescriptor(s, "DateTimeOffsetParser: negative cases", descriptors);
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
