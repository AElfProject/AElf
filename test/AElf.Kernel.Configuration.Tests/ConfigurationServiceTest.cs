using System.Threading.Tasks;
using AElf.Standards.ACS3;
using AElf.Contracts.Configuration;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Configuration.Tests
{
    public sealed partial class ConfigurationServiceTest : KernelConfigurationTestBase
    {
        private readonly IConfigurationService _configurationService;
        private readonly IBlockTransactionLimitProvider _blockTransactionLimitProvider;
        private readonly IBlockchainService _blockchainService;

        public ConfigurationServiceTest()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _configurationService = GetRequiredService<IConfigurationService>();
            _blockTransactionLimitProvider = GetRequiredService<IBlockTransactionLimitProvider>();
        }

        private async Task InitializeContractsAndSetLib()
        {
            await DeployContractsAsync();
            var chain = await _blockchainService.GetChainAsync();
            await _blockchainService.SetIrreversibleBlockAsync(chain, chain.BestChainHeight, chain.BestChainHash);
        }
        
        [Theory]
        [InlineData(55,true)]
        [InlineData(-50,false)]
        public async Task ProcessConfigurationAsync_Test(int targetLimit,bool isSuccessful)
        {
            var blockTransactionLimitConfigurationProcessor = GetRequiredService<IConfigurationProcessor>();
            var configurationName = blockTransactionLimitConfigurationProcessor.ConfigurationName;
            var targetValue = new Int32Value
            {
                Value = targetLimit
            }.ToByteString();
            var chain = await _blockchainService.GetChainAsync();
            var blockIndex = new BlockIndex
            {
                BlockHeight = chain.BestChainHeight,
                BlockHash = chain.BestChainHash
            };
            await _configurationService.ProcessConfigurationAsync(configurationName, targetValue, blockIndex);
            await _blockchainService.SetIrreversibleBlockAsync(chain, chain.BestChainHeight, chain.BestChainHash);
            var getValue = await _blockTransactionLimitProvider.GetLimitAsync(blockIndex);
            getValue.ShouldBe(isSuccessful ? targetLimit : int.MaxValue);
        }

        [Fact]
        public async Task GetConfigurationDataAsync_Test()
        {
            var blockTransactionLimitConfigurationProcessor = GetRequiredService<IConfigurationProcessor>();
            var configurationName = blockTransactionLimitConfigurationProcessor.ConfigurationName;
            var setValue = 100;
            await InitializeContractsAndSetLib();
            var proposalId = (await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = nameof(ConfigurationStub.SetConfiguration),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = new SetConfigurationInput
                {
                    Key = configurationName,
                    Value = new Int32Value {Value = setValue}.ToByteString()
                }.ToByteString(),
                ToAddress = ConfigurationContractAddress,
                OrganizationAddress = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty())
            })).Output;
            await ParliamentContractStub.Approve.SendAsync(proposalId);
            await ParliamentContractStub.Release.SendAsync(proposalId);
            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            var getValueByteString = await _configurationService.GetConfigurationDataAsync(configurationName, chainContext);
            var getValue = Int32Value.Parser.ParseFrom(getValueByteString);
            getValue.Value.ShouldBe(setValue);
        }
    }
}