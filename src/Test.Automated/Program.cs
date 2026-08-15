using Test.Shared;
using Touchstone.Cli;

// Touchstone CLI runner for the WhatTimeIsIt test suites.
//
// All test cases are defined once in Test.Shared (the central source of truth) and executed here
// through the Touchstone console runner, which renders colored, tabular pass/fail/skip output and
// returns a non-zero exit code if any test fails.
//
// Usage:
//   dotnet run --project Test.Automated
//   dotnet run --project Test.Automated -- --results results.json
//
// Pass "--results <path>" to additionally export structured JSON results.

string? resultsPath = null;
for (int i = 0; i < args.Length - 1; i++)
{
    if (string.Equals(args[i], "--results", StringComparison.OrdinalIgnoreCase))
    {
        resultsPath = args[i + 1];
        break;
    }
}

return await ConsoleRunner.RunAsync(WhatTimeIsItSuites.All, resultsPath: resultsPath);
