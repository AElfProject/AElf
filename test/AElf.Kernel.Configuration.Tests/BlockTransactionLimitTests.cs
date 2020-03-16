using System.Linq;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using AElf.Contracts.Configuration;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Parliament;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace AElf.Kernel.Configuration.Tests
{
    public sealed class BlockTransactionLimitTests : ContractTestBase<ConfigurationTestModule>
    {
        private Address ConfigurationContractAddress { get; set; }
        private ConfigurationContainer.ConfigurationStub _configurationStub;
        private ParliamentContractContainer.ParliamentContractStub _parliamentContractStub;
        private ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockTransactionLimitProvider _blockTransactionLimitProvider;
        private readonly IBlockchainStateService _blockchainStateService;

        public BlockTransactionLimitTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _blockTransactionLimitProvider = GetRequiredService<IBlockTransactionLimitProvider>();
            _blockchainStateService = GetRequiredService<IBlockchainStateService>();
        }

        private async Task DeployContractsAsync()
        {
            var category = KernelConstants.CodeCoverageRunnerCategory;
            var code = Codes.Single(kv => kv.Key.Split(",").First().EndsWith("Configuration")).Value;
            OptionalLogEventListeningService<IBlockAcceptedLogEventProcessor>.Enabled = true;
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
            const int targetLimit = 55;
            await DeployContractsAsync();
            var proposalId = (await _parliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = "SetConfiguration",
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = new SetConfigurationInput
                {
                    Key = BlockTransactionLimitConfigurationNameProvider.Name,
                    Value = new Int32Value {Value = targetLimit}.ToByteString()
                }.ToByteString(),
                ToAddress = ConfigurationContractAddress,
                OrganizationAddress = await _parliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty())
            })).Output;
            await _parliamentContractStub.Approve.SendAsync(proposalId);

            // Before
            {
                var result = await _configurationStub.GetConfiguration.CallAsync(new StringValue
                {
                    Value = BlockTransactionLimitConfigurationNameProvider.Name
                });
                var limit = new Int32Value();
                limit.MergeFrom(BytesValue.Parser.ParseFrom(result.ToByteString()).Value);
                Assert.Equal(0, limit.Value);
            }

            var txResult = await _parliamentContractStub.Release.SendAsync(proposalId);
            var configurationSet = ConfigurationSet.Parser.ParseFrom(txResult.TransactionResult.Logs
                .First(l => l.Name == nameof(ConfigurationSet)).NonIndexed);
            var limitFromLogEvent = new Int32Value();
            limitFromLogEvent.MergeFrom(configurationSet.Value.ToByteArray());
            Assert.Equal(limitFromLogEvent.Value, targetLimit);

            // After
            {
                var result = await _configurationStub.GetConfiguration.CallAsync(new StringValue
                {
                    Value = BlockTransactionLimitConfigurationNameProvider.Name
                });
                var limit = new Int32Value();
                limit.MergeFrom(BytesValue.Parser.ParseFrom(result.ToByteString()).Value);
                Assert.Equal(targetLimit, limit.Value);
            }
            var chain = await _blockchainService.GetChainAsync();
            await _blockchainStateService.MergeBlockStateAsync(chain.BestChainHeight, chain.BestChainHash);
            var limitNum = await _blockTransactionLimitProvider.GetLimitAsync(
                new ChainContext
                {
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight
                });
            Assert.Equal(55, limitNum);
        }

        [Fact]
        public async Task TransactionLimitSetAndGet_Test()
        {
            var chain = await _blockchainService.GetChainAsync();

            await _blockTransactionLimitProvider.SetLimitAsync(new BlockIndex
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            }, 50);

            var limit = await _blockTransactionLimitProvider.GetLimitAsync(
                new ChainContext
                {
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight
                });
            Assert.Equal(50, limit);
        }
    }
}