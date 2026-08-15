namespace Test.Nunit
{
    using System.Collections;
    using System.Threading;
    using System.Threading.Tasks;
    using global::NUnit.Framework;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.NunitAdapter;

    /// <summary>
    /// NUnit host for the shared WhatTimeIsIt test descriptors. The TestCaseSource pattern surfaces one
    /// NUnit test per Touchstone case. Test logic lives entirely in Test.Shared; this class only adapts
    /// it to the NUnit runner.
    /// </summary>
    [TestFixture]
    public sealed class WhatTimeIsItNunitTests
    {
        private static IEnumerable TestCases()
        {
            return new TouchstoneTestCaseSource(WhatTimeIsItSuites.All);
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public async Task RunTest(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None);
        }
    }
}
