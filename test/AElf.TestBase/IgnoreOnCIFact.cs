using System;
using Xunit;

namespace AElf.TestBase
{
    public sealed class IgnoreOnCIFact : FactAttribute
    {
        public IgnoreOnCIFact()
        {
            if (IsOnCI())
            {
                Skip = "Ignore on CI running to save execution time.";
            }
        }

        private static bool IsOnCI()
            => Environment.GetEnvironmentVariable("CI_TEST") != null;
    }
}