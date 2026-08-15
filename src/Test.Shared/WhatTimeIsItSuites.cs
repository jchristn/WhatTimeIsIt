namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Touchstone.Core;

    /// <summary>
    /// Central source of truth for every WhatTimeIsIt test. All runners (the Touchstone CLI runner in
    /// Test.Automated, the xUnit adapter in Test.Xunit, and the NUnit adapter in Test.Nunit) execute
    /// exactly these descriptors, so the coverage is defined once and shared everywhere.
    /// </summary>
    public static class WhatTimeIsItSuites
    {
        /// <summary>Every test suite exposed by the library test project.</summary>
        public static IReadOnlyList<TestSuiteDescriptor> All
        {
            get
            {
                List<TestSuiteDescriptor> suites = new List<TestSuiteDescriptor>();
                suites.AddRange(DateTimeParserSuites.All);
                suites.AddRange(DateTimeOffsetParserSuites.All);
                return suites;
            }
        }

        /// <summary>
        /// Convenience factory for a synchronous test case. The body runs to completion; throwing signals failure.
        /// </summary>
        internal static TestCaseDescriptor Case(string suiteId, string caseId, string displayName, Action body)
        {
            return new TestCaseDescriptor(
                suiteId: suiteId,
                caseId: caseId,
                displayName: displayName,
                executeAsync: ct =>
                {
                    body();
                    return Task.CompletedTask;
                });
        }
    }
}
