using System.Collections.Generic;

namespace AElf.Contracts.Economic.AEDPoSExtension.Tests
{
    public class TreasuryDistributionInformation
    {
        private readonly Dictionary<EconomicTestBase.SchemeType, DistributionInformation> _information =
            new Dictionary<EconomicTestBase.SchemeType, DistributionInformation>();

        public long TotalAmount { get; set; }

        public DistributionInformation this[EconomicTestBase.SchemeType schemeType]
        {
            get => _information.TryGetValue(schemeType, out var information)
                ? information
                : new DistributionInformation();
            set => _information[schemeType] = value;
        }
    }

    public class DistributionInformation
    {
        public long Amount { get; set; }
        public long TotalShares { get; set; }
    }
}