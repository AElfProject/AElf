using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.TestKit;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Token;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable InconsistentNaming
namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    public class AEDPoSExtensionTestBase : ContractTestBase<ContractTestAEDPoSExtensionModule>
    {
        private readonly Dictionary<Hash, string> _systemContractKeyWords = new Dictionary<Hash, string>
        {
            {VoteSmartContractAddressNameProvider.Name, "Vote"},
            {ProfitSmartContractAddressNameProvider.Name, "Profit"},
            {ElectionSmartContractAddressNameProvider.Name, "Election"},
            {ParliamentAuthSmartContractAddressNameProvider.Name, "ParliamentAuth"},
            {TokenSmartContractAddressNameProvider.Name, "MultiToken"},
            {TokenConverterSmartContractAddressNameProvider.Name, "TokenConverter"},
            {TreasurySmartContractAddressNameProvider.Name, "Treasury"},
            {ConsensusSmartContractAddressNameProvider.Name, "AEDPoS"},
            {EconomicSmartContractAddressNameProvider.Name, "Economic"},
            {CrossChainSmartContractAddressNameProvider.Name, "CrossChain"},
            {ReferendumAuthSmartContractAddressNameProvider.Name, "ReferendumAuth"},
        };

        protected IBlockMiningService BlockMiningService =>
            Application.ServiceProvider.GetRequiredService<IBlockMiningService>();

        protected ITestDataProvider TestDataProvider =>
            Application.ServiceProvider.GetRequiredService<ITestDataProvider>();

        protected IBlockchainService BlockchainService =>
            Application.ServiceProvider.GetRequiredService<IBlockchainService>();

        protected ITransactionTraceProvider TransactionTraceProvider =>
            Application.ServiceProvider.GetRequiredService<ITransactionTraceProvider>();

        public Dictionary<Hash, Address> ContractAddresses;

        /// <summary>
        /// Exception will throw if provided system contract name not contained as a Key of _systemContractKeyWords.
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
}