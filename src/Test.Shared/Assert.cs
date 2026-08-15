namespace Test.Shared
{
    using System;

    /// <summary>
    /// Exception thrown when a Touchstone test assertion fails. Touchstone captures the full
    /// exception (message, stack trace, inner exceptions) and reports it through every runner.
    /// </summary>
    public sealed class TestAssertionException : Exception
    {
        public TestAssertionException(string message) : base(message) { }
        public TestAssertionException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Minimal, runner-agnostic assertion helpers. A test case "passes" when its delegate returns
    /// without throwing and "fails" when it throws; these helpers throw <see cref="TestAssertionException"/>
    /// with a descriptive message on failure so the reason surfaces identically in the CLI, xUnit, and NUnit.
    /// </summary>
    public static class Assert
    {
        /// <summary>Asserts that a condition is true.</summary>
        public static void True(bool condition, string message)
        {
            if (!condition) throw new TestAssertionException("Expected condition to be true: " + message);
        }

        /// <summary>Asserts that a condition is false.</summary>
        public static void False(bool condition, string message)
        {
            if (condition) throw new TestAssertionException("Expected condition to be false: " + message);
        }

        /// <summary>Asserts two values are equal using the default equality comparer.</summary>
        public static void Equal<T>(T expected, T actual, string message)
        {
            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(expected, actual))
                throw new TestAssertionException($"{message}. Expected <{Fmt(expected)}> but got <{Fmt(actual)}>.");
        }

        /// <summary>
        /// Asserts two <see cref="DateTime"/> values represent the same instant (tick-for-tick).
        /// DateTime equality intentionally ignores <see cref="DateTimeKind"/>; use <see cref="Kind"/> to assert kind.
        /// </summary>
        public static void DateTimeEqual(DateTime expected, DateTime actual, string message)
        {
            if (expected.Ticks != actual.Ticks)
                throw new TestAssertionException(
                    $"{message}. Expected <{expected:yyyy-MM-dd HH:mm:ss.fffffff}> ({expected.Ticks} ticks) " +
                    $"but got <{actual:yyyy-MM-dd HH:mm:ss.fffffff}> ({actual.Ticks} ticks); " +
                    $"delta {actual.Ticks - expected.Ticks} ticks.");
        }

        /// <summary>Asserts the <see cref="DateTimeKind"/> of a parsed value.</summary>
        public static void Kind(DateTimeKind expected, DateTime actual, string message)
        {
            if (actual.Kind != expected)
                throw new TestAssertionException($"{message}. Expected Kind=<{expected}> but got <{actual.Kind}>.");
        }

        /// <summary>
        /// Asserts two <see cref="DateTimeOffset"/> values are equal in BOTH instant and offset.
        /// DateTimeOffset equality alone only compares the instant, so the offset is checked explicitly.
        /// </summary>
        public static void OffsetEqual(DateTimeOffset expected, DateTimeOffset actual, string message)
        {
            if (expected.Offset != actual.Offset)
                throw new TestAssertionException(
                    $"{message}. Offset mismatch: expected <{expected.Offset}> but got <{actual.Offset}> " +
                    $"(expected {expected:yyyy-MM-dd HH:mm:ss.fffffff zzz}, actual {actual:yyyy-MM-dd HH:mm:ss.fffffff zzz}).");

            if (expected.UtcTicks != actual.UtcTicks)
                throw new TestAssertionException(
                    $"{message}. Instant mismatch: expected <{expected:yyyy-MM-dd HH:mm:ss.fffffff zzz}> " +
                    $"but got <{actual:yyyy-MM-dd HH:mm:ss.fffffff zzz}>; delta {actual.UtcTicks - expected.UtcTicks} ticks.");
        }

        /// <summary>Asserts that <paramref name="action"/> throws an exception of type <typeparamref name="TException"/>.</summary>
        public static void Throws<TException>(Action action, string message) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new TestAssertionException(
                    $"{message}. Expected exception of type <{typeof(TException).Name}> but got <{ex.GetType().Name}>: {ex.Message}", ex);
            }

            throw new TestAssertionException($"{message}. Expected exception of type <{typeof(TException).Name}> but none was thrown.");
        }

        private static string Fmt<T>(T value) => value is null ? "null" : value.ToString() ?? "null";
    }
}
