using System.Linq;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Miner.Application;
using AElf.Types;
using AElf.Contracts.Configuration;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Parliament;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AElf.Kernel.BlockTransactionLimitController.Tests
{
    public sealed class BlockTransactionLimitTests : ContractTestBase<BlockTransactionLimitTestModule>
    {
        private Address ConfigurationContractAddress { get; set; }
        private ConfigurationContainer.ConfigurationStub _configurationStub;
        private ParliamentContractContainer.ParliamentContractStub _parliamentContractStub;
        private ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        private readonly IBlockchainService _blockchainService;

        public BlockTransactionLimitTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
        }

        private async Task DeployContractsAsync()
        {
            var category = KernelConstants.CodeCoverageRunnerCategory;
            var code = Codes.Single(kv => kv.Key.Split(",").First().EndsWith("Configuration")).Value;
            OptionalLogEventListeningService<IBlockAcceptedLogEventHandler>.Enabled = true;
            ConfigurationContractAddress = await DeploySystemSmartContract(category, code,
                ConfigurationSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
            _configurationStub =
                GetTester<ConfigurationContainer.ConfigurationStub>(ConfigurationContractAddress,
                    DefaultSenderKeyPair);
            
            var consensusContractCode = Codes.Single(kv => kv.Key.Split(",").First().EndsWith("Consensus.AEDPoS")).Value;
            var consensusContractAddress = await DeploySystemSmartContract(category, consensusContractCode,
                ConsensusSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
            var consensusStub = GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(consensusContractAddress,
                DefaultSenderKeyPair);

            await consensusStub.FirstRound.SendAsync(
                new MinerList
                {
                    Pubkeys = {ByteString.CopyFrom(DefaultSenderKeyPair.PublicKey)}
                }.GenerateFirstRoundOfNewTerm(4000, TimestampHelper.GetUtcNow())
            );
            
            var parliamentContractCode = Codes.Single(kv => kv.Key.Split(",").First().EndsWith("Parliament")).Value;
            var parliamentContractAddress = await DeploySystemSmartContract(category, parliamentContractCode,
                ParliamentSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
            _parliamentContractStub = GetTester<ParliamentContractContainer.ParliamentContractStub>(parliamentContractAddress,
                DefaultSenderKeyPair);
            
            await _parliamentContractStub.Initialize.SendAsync(new InitializeInput
            {
                ProposerAuthorityRequired = true,
                PrivilegedProposer = Address.FromPublicKey(DefaultSenderKeyPair.PublicKey)
            });
            var chain = await _blockchainService.GetChainAsync();
            await _blockchainService.SetIrreversibleBlockAsync(chain, chain.BestChainHeight, chain.BestChainHash);
        }

        [Fact]
        public async Task LimitCanBeSetByExecutingContract_Test()
        {
            await DeployContractsAsync();
            var proposalId = (await _parliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = "SetBlockTransactionLimit",
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = new Int32Value {Value = 55}.ToByteString(),
                ToAddress = ConfigurationContractAddress,
                OrganizationAddress = await _parliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty())
            })).Output;
            await _parliamentContractStub.Approve.SendAsync(proposalId);
           
            {
                var limit = await _configurationStub.GetBlockTransactionLimit.CallAsync(new Empty());
                Assert.Equal(0, limit.Value);
            }
            await _parliamentContractStub.Release.SendAsync(proposalId);
            {
                var limit = await _configurationStub.GetBlockTransactionLimit.CallAsync(new Empty());
                Assert.Equal(55, limit.Value);
            }
            var provider = Application.ServiceProvider.GetRequiredService<IBlockTransactionLimitProvider>();
            var chain = await _blockchainService.GetChainAsync();
            var limitNum = await provider.GetLimitAsync(new ChainContext
                {BlockHash = chain.BestChainHash, BlockHeight = chain.BestChainHeight});
            Assert.Equal(55, limitNum);
        }
    }
}