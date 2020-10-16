using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS0;
using AElf.ContractDeployer;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Miner;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.ContractTestBase.ContractTestKit
{
    public class ContractTestKit<TModule> where TModule : AbpModule
    {
        private IReadOnlyDictionary<string, byte[]> _codes;

        public IReadOnlyDictionary<string, byte[]> Codes => _codes ??= ContractsDeployer.GetContractCodes<TModule>();

        protected IAbpApplication Application { get; }
        
        protected IReadOnlyList<Account> Accounts => SampleAccount.Accounts;

        public ISmartContractAddressService ContractAddressService =>
            Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();

        public Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();
        
        public Account DefaultAccount => Accounts[0];

        protected int InitialCoreDataCenterCount = 5;

        public Dictionary<Hash, Address> SystemContractAddresses { get; } = new Dictionary<Hash, Address>();
        
        private readonly IContractTestService _contractTestService;

        public ContractTestKit(IAbpApplication application)
        {
            Application = application;
            _contractTestService = Application.ServiceProvider.GetRequiredService<IContractTestService>();
            AsyncHelper.RunSync(InitSystemContractAddressesAsync);
        }
        
        private async Task InitSystemContractAddressesAsync()
        {
            var blockchainService = Application.ServiceProvider.GetService<IBlockchainService>();
            var chain = await blockchainService.GetChainAsync();
            var block = await blockchainService.GetBlockByHashAsync(chain.GenesisBlockHash);
            var transactionResultManager = Application.ServiceProvider.GetService<ITransactionResultManager>();
            var transactionResults =
                await transactionResultManager.GetTransactionResultsAsync(block.Body.TransactionIds, block.GetHash());
            foreach (var transactionResult in transactionResults)
            {
                Assert.True(transactionResult.Status == TransactionResultStatus.Mined, transactionResult.Error);
                var relatedLogs = transactionResult.Logs.Where(l => l.Name == nameof(ContractDeployed)).ToList();
                if (!relatedLogs.Any()) break;
                foreach (var relatedLog in relatedLogs)
                {
                    var eventData = new ContractDeployed();
                    eventData.MergeFrom(relatedLog);
                    SystemContractAddresses[eventData.Name] = eventData.Address;
                }
            }
        }

        public T GetTester<T>(Address contractAddress, ECKeyPair senderKey = null) where T : ContractStubBase, new()
        {
            return _contractTestService.GetTester<T>(contractAddress, senderKey ?? DefaultAccount.KeyPair);
        }
        
        /// <summary>
        /// Mine a block with given normal txs and system txs.
        /// Normal txs will use tx pool while system txs not.
        /// </summary>
        /// <param name="txs"></param>
        /// <param name="blockTime"></param>
        /// <returns></returns>
        public async Task<BlockExecutedSet> MineAsync(List<Transaction> txs, Timestamp blockTime = null)
        {
            return await _contractTestService.MineAsync(txs, blockTime);
        }
        
        public async Task<TransactionResult> ExecuteTransactionWithMiningAsync(Transaction transaction, Timestamp blockTime = null)
        {
            return await _contractTestService.ExecuteTransactionWithMiningAsync(transaction,blockTime);
        }
    }
}