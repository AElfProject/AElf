using System;
using System.Linq;

namespace AElf.Common
{
    /// <summary>
    /// Unit test may not have token contract properly deployed, so the fee transaction should be skipped. 
    /// </summary>
    public static class UnitTestDetector
    {
        static UnitTestDetector()
        {
            string testAssemblyName = "xunit.runner";
            IsInUnitTest = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName.StartsWith(testAssemblyName));
        }

        public static bool IsInUnitTest { get; private set; }
    }
}