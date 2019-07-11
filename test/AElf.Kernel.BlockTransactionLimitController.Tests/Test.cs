using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Miner.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AElf.Kernel.BlockTransactionLimitController.Tests
{
    public class Test : ContractTestBase<TestModule>
    {
        private Address ConfigurationContractAddress { get; set; }
        private Configuration.ConfigurationContainer.ConfigurationStub ConfigurationStub;
        private ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);

        private async Task DeployContractsAsync()
        {
            var category = KernelConstants.CodeCoverageRunnerCategory;
            var code = Codes.Single(kv => kv.Key.Split(",").First().EndsWith("Configuration")).Value;
            ConfigurationContractAddress = await DeploySystemSmartContract(category, code,
                ConfigurationSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
            ConfigurationStub =
                GetTester<Configuration.ConfigurationContainer.ConfigurationStub>(ConfigurationContractAddress,
                    DefaultSenderKeyPair);
        }

        [Fact]
        public async Task LimitCanBeSetByExecutingContract()
        {
            await DeployContractsAsync();
            OptionalLogEventListeningService.Enabled = true;
            {
                var limit = await ConfigurationStub.GetBlockTransactionLimit.CallAsync(new Empty());
                Assert.Equal(0, limit.Value);
            }

            await ConfigurationStub.SetBlockTransactionLimit.SendAsync(new Int32Value() {Value = 55});
            {
                var limit = await ConfigurationStub.GetBlockTransactionLimit.CallAsync(new Empty());
                Assert.Equal(55, limit.Value);
            }
            var provider = Application.ServiceProvider.GetRequiredService<IBlockTransactionLimitProvider>();
            Assert.Equal(55, provider.Limit);
        }
    }
}