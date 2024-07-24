using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.EconomicSystem;
using AElf.GovernmentSystem;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Configuration;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.Token;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable InconsistentNaming
namespace AElf.ContractTestKit.AEDPoSExtension;

public class AEDPoSExtensionTestBase : ContractTestBase<ContractTestAEDPoSExtensionModule>
{
    private readonly Dictionary<Hash, string> _systemContractKeyWords = new()
    {
        { VoteSmartContractAddressNameProvider.Name, "Vote" },
        { ProfitSmartContractAddressNameProvider.Name, "Profit" },
        { ElectionSmartContractAddressNameProvider.Name, "Election" },
        { ParliamentSmartContractAddressNameProvider.Name, "Parliament" },
        { TokenSmartContractAddressNameProvider.Name, "MultiToken" },
        { TokenConverterSmartContractAddressNameProvider.Name, "TokenConverter" },
        { TreasurySmartContractAddressNameProvider.Name, "Treasury" },
        { ConsensusSmartContractAddressNameProvider.Name, "AEDPoS" },
        { EconomicSmartContractAddressNameProvider.Name, "Economic" },
        { SmartContractConstants.CrossChainContractSystemHashName, "CrossChain" },
        { ReferendumSmartContractAddressNameProvider.Name, "Referendum" },
        { AssociationSmartContractAddressNameProvider.Name, "Association" },
        { TokenHolderSmartContractAddressNameProvider.Name, "TokenHolder" },
        { ConfigurationSmartContractAddressNameProvider.Name, "Configuration" }
    };

    public Dictionary<Hash, Address> ContractAddresses;

    protected IBlockMiningService BlockMiningService =>
        Application.ServiceProvider.GetRequiredService<IBlockMiningService>();

    protected ITestDataProvider TestDataProvider =>
        Application.ServiceProvider.GetRequiredService<ITestDataProvider>();

    protected IBlockchainService BlockchainService =>
        Application.ServiceProvider.GetRequiredService<IBlockchainService>();

    protected ITransactionTraceProvider TransactionTraceProvider =>
        Application.ServiceProvider.GetRequiredService<ITransactionTraceProvider>();

    /// <summary>
    ///     Exception will throw if provided system contract name not contained as a Key of _systemContractKeyWords.
    /// </summary>
    /// <param name="systemContractNames"></param>
    /// <returns></returns>
    protected async Task<Dictionary<Hash, Address>> DeploySystemSmartContracts(
        IEnumerable<Hash> systemContractNames)
    {
        return await BlockMiningService.DeploySystemContractsAsync(systemContractNames.ToDictionary(n => n,
            n => Codes.Single(c => c.Key.Contains(_systemContractKeyWords[n])).Value));
    }
}