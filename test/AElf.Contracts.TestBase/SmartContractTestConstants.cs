using AElf.Kernel;

namespace AElf.Contracts.TestBase
{
    public static class SmartContractTestConstants
    {
        /// <summary>
        /// 30 means use default assembly loader context, for code coverage
        /// </summary>
        public const int TestRunnerCategory = KernelConstants.CodeCoverageRunnerCategory;

        public const int ResourceSupply = 10000;

        public const string Consensus = "Consensus.AEDPoS";
        public const string MultiToken = "MultiToken";
        public const string CrossChain = "CrossChain";
        public const string Parliament = "Parliament";
        public const string Configuration = "Configuration";
        public const string Association = "Association";
        public const string Referendum = "Referendum";
    }
}