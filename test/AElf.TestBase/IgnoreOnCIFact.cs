using System;

namespace AElf.TestBase;

public sealed class IgnoreOnCIFact : FactAttribute
{
    public IgnoreOnCIFact()
    {
        if (IsOnCI()) Skip = "Ignore on CI running to save execution time.";
    }

    private static bool IsOnCI()
    {
        return Environment.GetEnvironmentVariable("CI_TEST") != null;
    }
}