using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.Infrastructure;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus
{
    public class ConsensusServiceTests : ConsensusTestBase
    {
        private IConsensusService _consensusService;
        private ConsensusControlInformation _consensusControlInformation;
        public ConsensusServiceTests()
        {
            _consensusService = GetRequiredService<IConsensusService>();
            _consensusControlInformation = GetRequiredService<ConsensusControlInformation>();
        }

        [Fact]
        public async Task ValidateConsensusBeforeExecutionAsync()
        {
            var chainContext = new ChainContext
            {
                BlockHash = Hash.Generate(),
                BlockHeight = 100
            };
            var consensusExtraData = ByteString.CopyFromUtf8("test data").ToByteArray();
            var result = await _consensusService.ValidateConsensusBeforeExecutionAsync(chainContext, consensusExtraData);
            
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task ValidateConsensusAfterExecutionAsync()
        {
            var chainContext = new ChainContext
            {
                BlockHash = Hash.Generate(),
                BlockHeight = 100
            };
            var consensusExtraData = ByteString.CopyFromUtf8("test data").ToByteArray();
            var result = await _consensusService.ValidateConsensusAfterExecutionAsync(chainContext, consensusExtraData);
            
            result.ShouldBeTrue();
        }
        
        [Fact]
        public async Task GetNewConsensusInformationAsync()
        {
            var chainContext = new ChainContext
            {
                BlockHash = Hash.Generate(),
                BlockHeight = 100
            };
            var bytes = await _consensusService.GetInformationToUpdateConsensusAsync(chainContext);
            var dposTriggerInformation = DPoSTriggerInformation.Parser.ParseFrom(bytes);
            
            dposTriggerInformation.ShouldNotBeNull();
        }

        [Fact]
        public async Task GenerateConsensusTransactionsAsync()
        {
            var chainContext = new ChainContext
            {
                BlockHash = Hash.Generate(),
                BlockHeight = 100
            };
            var transactions = await _consensusService.GenerateConsensusTransactionsAsync(chainContext);
            
            transactions.ShouldNotBeNull();
            transactions.Count().ShouldBe(3);
            
            transactions.Select(t =>t.RefBlockNumber).ShouldAllBe(x => x == 100);
            
            var prefix = ByteString.CopyFrom(chainContext.BlockHash.Take(4).ToArray());
            transactions.Select(t =>t.RefBlockPrefix).ShouldAllBe( p => p == prefix);
        }
    }
}