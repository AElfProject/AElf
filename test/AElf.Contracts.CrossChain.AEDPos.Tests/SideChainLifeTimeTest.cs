using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Contracts.TestKit;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.Contracts.CrossChain.AEDPos.Tests
{
    public class SideChainLifeTimeTest : CrossChainContractTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public SideChainLifeTimeTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }
        
        [Fact]
        public async Task Create_SideChain()
        {
            //_testOutputHelper.WriteLine("create");
            await InitializeCrossChainContractAsync();
            long lockedTokenAmount = 10;
            await ApproveBalanceAsync(lockedTokenAmount);
            
            // Create proposal and approve
            var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount, ByteString.CopyFromUtf8("Test"));
            await ApproveWithMinersAsync(proposalId);
            await ReleaseProposalAsync(proposalId);
        }
        

    }
}