using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Kernel;
using AElf.Kernel.Consensus.Application;
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
        public async Task NormalBlock_Validate_ConsensusBeforeExecution_Success()
        {
            TesterManager.InitialTesters();

            var inValue = Hash.Generate();
            var triggerInformationForNormalBlock =
                TesterManager.GetTriggerInformationForNormalBlock(TesterManager.Testers[1].KeyPair.PublicKey.ToHex(), inValue);

            var newInformation =
                await TesterManager.Testers[1].GetNewConsensusInformationAsync(triggerInformationForNormalBlock);

            // Act
            var validationResult = await TesterManager.Testers[0].ValidateConsensusBeforeExecutionAsync(newInformation);

            // Assert
            Assert.True(validationResult?.Success);
        }

        [Fact]
        public async Task Validation_ConsensusAfterExecution_Success()
        {
            TesterManager.InitialTesters();
            
            var dposInformation = new DPoSHeaderInformation();

            var validationResult = await TesterManager.Testers[0].ValidateConsensusAfterExecutionAsync(dposInformation);
            validationResult.Success.ShouldBeTrue();
        }
    }
}