using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Parliament;
using AElf.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.Configuration.Tests
{
    public class KernelConfigurationTestBase : ContractTestBase<ConfigurationTestModule>
    {
        protected Address ConfigurationContractAddress { get; set; }
        internal ConfigurationContainer.ConfigurationStub ConfigurationStub;
        internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub;
        private ECKeyPair DefaultSenderKeyPair => Accounts[0].KeyPair;
        
        protected async Task DeployContractsAsync()
        {
            var category = KernelConstants.CodeCoverageRunnerCategory;
            var code = Codes.Single(kv => kv.Key.Split(",").First().EndsWith("Configuration")).Value;
            OptionalLogEventProcessingService<IBlockAcceptedLogEventProcessor>.Enabled = true;
            ConfigurationContractAddress = await DeploySystemSmartContract(category, code,
                ConfigurationSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
            ConfigurationStub =
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
            ParliamentContractStub = GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(parliamentContractAddress,
                DefaultSenderKeyPair);
            
            await ParliamentContractStub.Initialize.SendAsync(new InitializeInput
            {
                ProposerAuthorityRequired = true,
                PrivilegedProposer = Address.FromPublicKey(DefaultSenderKeyPair.PublicKey)
            });
        }
    }
}