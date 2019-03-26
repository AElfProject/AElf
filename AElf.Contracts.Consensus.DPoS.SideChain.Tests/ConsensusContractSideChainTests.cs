using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
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

        //TODO: UpdateMainChainConsensus default parameter check should be update, round info should check.
        //BODY: System.NullReferenceException: Object reference not set to an instance of an object.
        //at AElf.Contracts.Consensus.DPoS.SideChain.ConsensusContract.UpdateMainChainConsensus(DPoSInformation input)
        //at AElf.Types.CSharp.UnaryServerCallHandler`2.Execute(Byte[] input) in /Users/ericshu/GitHub/AElf/AElf.Runtime.CSharp/ServerCallHandler.cs:line 57
        //at AElf.Runtime.CSharp.Executive.ExecuteMainTransaction() in /Users/ericshu/GitHub/AElf/AElf.Runtime.CSharp/Executive.cs:line 139 
        [Fact(Skip = "Default check cannot works.")]
        public async Task UpdateMainChainConsensus_WithDefaultDPosInformation()
        {
            TesterManager.InitialTesters();
            
            //input = new DPoSInformation()
            var dposInformation = new DPoSInformation();
            var transactionResult = await TesterManager.Testers[0].ExecuteContractWithMiningAsync(
                TesterManager.DPoSSideChainContractAddress,
                "UpdateMainChainConsensus", dposInformation);

            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        
        [Fact]
        public async Task UpdateMainChainConsensus_WithoutChange()
        {
            TesterManager.InitialTesters();
            
            //term number < main chain term number
            var dposInformation = new DPoSInformation
            {
                Behaviour = DPoSBehaviour.NextRound,
                Round = new Round(),
                SenderPublicKey = TesterManager.MinersKeyPairs[0].PublicKey.ToHex()
            };
            var transactionResult = await TesterManager.Testers[0].ExecuteContractWithMiningAsync(
                TesterManager.DPoSSideChainContractAddress,
                "UpdateMainChainConsensus", dposInformation);
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        
        [Fact]
        public async Task UpdateMainChainConsensus_Success()
        {
            TesterManager.InitialTesters();
            
            var dposInformation = new DPoSInformation
            {
                Behaviour = DPoSBehaviour.NextRound,
                Round = new Round()
                {
                    TermNumber = 2
                },
                SenderPublicKey = TesterManager.MinersKeyPairs[0].PublicKey.ToHex()
            };
            
            var transactionResult = await TesterManager.Testers[0].ExecuteContractWithMiningAsync(
                TesterManager.DPoSSideChainContractAddress,
                "UpdateMainChainConsensus", dposInformation);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task UpdateMainChainConsensus_FirstRoundInfo()
        {
            TesterManager.InitialTesters();
            
            var dposInformation = new DPoSInformation
            {
                Behaviour = DPoSBehaviour.NextRound,
                Round = TesterManager.MinersKeyPairs.Select(p => p.PublicKey.ToHex()).ToList().ToMiners()
                    .GenerateFirstRoundOfNewTerm(DPoSSideChainTester.MiningInterval),
                SenderPublicKey = TesterManager.MinersKeyPairs[0].PublicKey.ToHex()
            };
            
            var transactionResult = await TesterManager.Testers[0].ExecuteContractWithMiningAsync(
                TesterManager.DPoSSideChainContractAddress,
                "UpdateMainChainConsensus", dposInformation);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }
}