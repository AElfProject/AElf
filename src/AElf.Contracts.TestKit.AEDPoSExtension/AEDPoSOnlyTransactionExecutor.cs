using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus;
using AElf.Kernel.Miner;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    // ReSharper disable once InconsistentNaming
    public class AEDPoSOnlyTransactionExecutor : ITransactionExecutor
    {
        private readonly IServiceProvider _serviceProvider;

        public AEDPoSOnlyTransactionExecutor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Only AEDPoS transactions can get executed.
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async Task ExecuteAsync(Transaction transaction)
        {
            var contractAddressService = _serviceProvider.GetRequiredService<ISmartContractAddressService>();
            var zeroContractAddress = contractAddressService.GetZeroSmartContractAddress();
            var consensusContractAddress =
                contractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);
            if (zeroContractAddress != transaction.To && consensusContractAddress != transaction.To)
            {
                return;
            }

            var blockchainService = _serviceProvider.GetRequiredService<IBlockchainService>();
            var preBlock = await blockchainService.GetBestChainLastBlockHeaderAsync();
            var miningService = _serviceProvider.GetRequiredService<IMiningService>();
            var blockAttachService = _serviceProvider.GetRequiredService<IBlockAttachService>();
            var blockTimeProvider = _serviceProvider.GetRequiredService<IBlockTimeProvider>();

            var block = await miningService.MineAsync(
                new RequestMiningDto
                {
                    PreviousBlockHash = preBlock.GetHash(), PreviousBlockHeight = preBlock.Height,
                    BlockExecutionTime = TimestampHelper.DurationFromMilliseconds(int.MaxValue)
                },
                new List<Transaction> {transaction},
                blockTimeProvider.GetBlockTime());

            await blockchainService.AddTransactionsAsync(new List<Transaction> {transaction});
            await blockchainService.AddBlockAsync(block);
            await blockAttachService.AttachBlockAsync(block);
        }

        public async Task<ByteString> ReadAsync(Transaction transaction)
        {
            var blockchainService = _serviceProvider.GetRequiredService<IBlockchainService>();
            var transactionReadOnlyExecutionService =
                _serviceProvider.GetRequiredService<ITransactionReadOnlyExecutionService>();
            var blockTimeProvider = _serviceProvider.GetRequiredService<IBlockTimeProvider>();

            var preBlock = await blockchainService.GetBestChainLastBlockHeaderAsync();
            var transactionTrace = await transactionReadOnlyExecutionService.ExecuteAsync(new ChainContext
                {
                    BlockHash = preBlock.GetHash(),
                    BlockHeight = preBlock.Height
                },
                transaction,
                blockTimeProvider.GetBlockTime());

            return transactionTrace.ReturnValue;
        }
    }
}