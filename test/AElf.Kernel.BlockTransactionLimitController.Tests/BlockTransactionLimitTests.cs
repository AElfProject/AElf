using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Miner.Application;
using AElf.Types;
using AElf.Contracts.Configuration;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AElf.Kernel.BlockTransactionLimitController.Tests
{
    public sealed class BlockTransactionLimitTests : ContractTestBase<BlockTransactionLimitTestModule>
    {
        private Address ConfigurationContractAddress { get; set; }
        private ConfigurationContainer.ConfigurationStub ConfigurationStub;
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
            ConfigurationContractAddress = await DeploySystemSmartContract(category, code,
                ConfigurationSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
            ConfigurationStub =
                GetTester<ConfigurationContainer.ConfigurationStub>(ConfigurationContractAddress,
                    DefaultSenderKeyPair);
        }

        [Fact]
        public async Task LimitCanBeSetByExecutingContract_Test()
        {
            await DeployContractsAsync();
            OptionalLogEventListeningService<IBestChainFoundLogEventHandler>.Enabled = true;
            {
                var limit = await ConfigurationStub.GetBlockTransactionLimit.CallAsync(new Empty());
                Assert.Equal(0, limit.Value);
            }

            // TODO: Figure out why To is null.
            await ConfigurationStub.SetBlockTransactionLimit.SendWithExceptionAsync(new Int32Value {Value = 55});
            {
                var limit = await ConfigurationStub.GetBlockTransactionLimit.CallAsync(new Empty());
                Assert.Equal(0, limit.Value);
            }
            var provider = Application.ServiceProvider.GetRequiredService<IBlockTransactionLimitProvider>();
            var chain = await _blockchainService.GetChainAsync();
            var limitNum = provider.GetLimit(new ChainContext
                {BlockHash = chain.BestChainHash, BlockHeight = chain.BestChainHeight});
            Assert.Equal(0, limitNum);
        }
    }
}