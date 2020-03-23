using System.Linq;
using AElf.TestBase;
using AElf.Types;

namespace AElf.Kernel.Proposal.Tests
{
    public class ProposalTestBase : AElfIntegratedTest<ProposalTestModule>
    {
        public static Address NormalAddress = SampleAddress.AddressList.Last();
        public static Address ParliamentContractFakeAddress = SampleAddress.AddressList.First();
    }
}