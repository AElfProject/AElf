using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.TestBase;
using AElf.Kernel;
using Shouldly;
using Xunit;

namespace AElf.Contracts.DPoS.SideChain
{
    public class ConsensusContractSideChainTests : DPoSSideChainTestBase
    {
        private DPoSSideChainTester TesterManager { get; set; }
        
        public ConsensusContractSideChainTests()
        {
            TesterManager = new DPoSSideChainTester();
            TesterManager.InitialSingleTester();
        }
        
        [Fact]
        public async Task UpdateMainChainConsensus()
        {
            TesterManager.InitialTesters();
            
            var dposInformation = new DPoSInformation
            {
                Behaviour = DPoSBehaviour.NextRound,
                Round = new Round(),
                SenderPublicKey = TesterManager.MinersKeyPairs[0].PublicKey.ToHex()
            };
            
            var transactionResult = await TesterManager.Testers[0].ExecuteContractWithMiningAsync(
                TesterManager.DPoSSideChainContractAddress,
                "UpdateMainChainConsensus", dposInformation);
            //Assert
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }
}