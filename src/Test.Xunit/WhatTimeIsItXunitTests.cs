namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Shared;
    using Touchstone.Core;
    using global::Xunit;

    /// <summary>
    /// xUnit host for the shared WhatTimeIsIt test descriptors. The theory-driven pattern surfaces one
    /// xUnit test per Touchstone case, so <c>dotnet test</c> reports each case individually. Test logic
    /// lives entirely in Test.Shared; this class only adapts it to the xUnit runner.
    /// </summary>
    public sealed class WhatTimeIsItXunitTests
    {
        public static TheoryData<TestCaseDescriptor> TestCases()
        {
            TheoryData<TestCaseDescriptor> data = new TheoryData<TestCaseDescriptor>();
            foreach (TestSuiteDescriptor suite in WhatTimeIsItSuites.All)
            {
                foreach (TestCaseDescriptor testCase in suite.Cases)
                {
                    if (!testCase.Skip)
                        data.Add(testCase);
                }
            }
            return data;
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task RunTest(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None);
        }
    }
}
