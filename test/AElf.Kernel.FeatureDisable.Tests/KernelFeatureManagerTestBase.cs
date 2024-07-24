using AElf.Contracts.Configuration;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Parliament;
using AElf.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Configuration;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.FeatureDisable.Tests;

public class KernelFeatureDisableTestBase : ContractTestBase<FeatureDisableTestModule>
{
    internal ConfigurationContainer.ConfigurationStub ConfigurationStub;
    internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub;
    internal Address ParliamentContractAddress { get; set; }
    internal Address ConfigurationContractAddress { get; set; }
    internal ECKeyPair DefaultSenderKeyPair => Accounts[0].KeyPair;

    protected async Task DeployContractsAsync()
    {
        const int category = KernelConstants.CodeCoverageRunnerCategory;
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
                Pubkeys = { ByteString.CopyFrom(DefaultSenderKeyPair.PublicKey) }
            }.GenerateFirstRoundOfNewTerm(4000, TimestampHelper.GetUtcNow())
        );

        var parliamentContractCode = Codes.Single(kv => kv.Key.Split(",").First().EndsWith("Parliament")).Value;
        ParliamentContractAddress = await DeploySystemSmartContract(category, parliamentContractCode,
            ParliamentSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
        ParliamentContractStub = GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(
            ParliamentContractAddress,
            DefaultSenderKeyPair);

        await ParliamentContractStub.Initialize.SendAsync(new InitializeInput
        {
            ProposerAuthorityRequired = true,
            PrivilegedProposer = Address.FromPublicKey(DefaultSenderKeyPair.PublicKey)
        });
    }
}