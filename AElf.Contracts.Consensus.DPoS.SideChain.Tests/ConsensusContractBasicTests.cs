using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Kernel;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.DPoS.SideChain
{
    public class DPoSSideChainTests : DPoSSideChainTestBase
    {
        private DPoSSideChainTester TesterManager { get; set; }

        public DPoSSideChainTests()
        {
            TesterManager = new DPoSSideChainTester();
            TesterManager.InitialSingleTester();
        }

        [Fact]
        public async Task Validation_ConsensusAfterExecution_Success()
        {
            TesterManager.InitialTesters();
            
            var dposInformation = new DPoSHeaderInformation();

            var validationResult = await TesterManager.Testers[0].ValidateConsensusAfterExecutionAsync(dposInformation);
            validationResult.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task Get_ConsensusCommand_Success()
        {
            TesterManager.InitialTesters();
            
            var input = new CommandInput
            {
                PublicKey = ByteString.CopyFrom(TesterManager.Testers[0].KeyPair.PublicKey)
            };

            var consensusCommand = await TesterManager.Testers[0].GetConsensusCommandAsync();
            consensusCommand.ShouldNotBeNull();

            var behavior = DPoSHint.Parser.ParseFrom(consensusCommand.Hint.ToByteArray()).Behaviour;
            behavior.ShouldBe(DPoSBehaviour.UpdateValue);
        }

        [Fact]
        public async Task InitialConsensus_Success()
        {
            
        }
    }
}