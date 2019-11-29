using System.Linq;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Miner.Application;
using AElf.Types;
using AElf.Contracts.Configuration;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.ParliamentAuth;
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
        private ParliamentAuthContractContainer.ParliamentAuthContractStub _parliamentAuthStub;
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
            
            var parliamentAuthContractCode = Codes.Single(kv => kv.Key.Split(",").First().EndsWith("ParliamentAuth")).Value;
            var parliamentAuthContractAddress = await DeploySystemSmartContract(category, parliamentAuthContractCode,
                ParliamentAuthSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
            _parliamentAuthStub = GetTester<ParliamentAuthContractContainer.ParliamentAuthContractStub>(parliamentAuthContractAddress,
                DefaultSenderKeyPair);
            
            await _parliamentAuthStub.Initialize.SendAsync(new InitializeInput
            {
                GenesisOwnerReleaseThreshold = 1,
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
            var proposalId = (await _parliamentAuthStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = "SetBlockTransactionLimit",
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = new Int32Value {Value = 55}.ToByteString(),
                ToAddress = ConfigurationContractAddress,
                OrganizationAddress = await _parliamentAuthStub.GetGenesisOwnerAddress.CallAsync(new Empty())
            })).Output;
            await _parliamentAuthStub.Approve.SendAsync(new ApproveInput
            {
                ProposalId = proposalId
            });
           
            {
                var limit = await _configurationStub.GetBlockTransactionLimit.CallAsync(new Empty());
                Assert.Equal(0, limit.Value);
            }
            await _parliamentAuthStub.Release.SendAsync(proposalId);
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