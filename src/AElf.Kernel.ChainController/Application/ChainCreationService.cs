﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.ChainController.Application
{
    public class ChainCreationService : IChainCreationService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly IBlockExecutionResultProcessingService _blockExecutionResultProcessingService;
        private readonly IBlockchainExecutingService _blockchainExecutingService;
        public ILogger<ChainCreationService> Logger { get; set; }

        public ILocalEventBus LocalEventBus { get; set; }

        public ChainCreationService(IBlockchainService blockchainService, IBlockExecutingService blockExecutingService,
            IBlockExecutionResultProcessingService blockExecutionResultProcessingService
            , IBlockchainExecutingService blockchainExecutingService)
        {
            _blockchainService = blockchainService;
            _blockExecutingService = blockExecutingService;
            _blockExecutionResultProcessingService = blockExecutionResultProcessingService;
            _blockchainExecutingService = blockchainExecutingService;
            Logger = NullLogger<ChainCreationService>.Instance;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        /// <summary>
        /// Creates a new chain with the provided genesis transactions and Smart Contract Zero.
        /// </summary>
        /// <returns>The new chain async.</returns>
        /// <param name="">The new chain id which will be derived from the creator address.</param>
        /// <param name="genesisTransactions">The transactions to be executed in the genesis block.</param>
        public async Task<Chain> CreateNewChainAsync(IEnumerable<Transaction> genesisTransactions)
        {
            try
            {
                var blockHeader = new BlockHeader
                {
                    Height = AElfConstants.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Empty,
                    Time = new Timestamp {Seconds = 0},
                    ChainId = _blockchainService.GetChainId()
                };

                var transactions = genesisTransactions.ToList();

                var block = await _blockExecutingService.ExecuteBlockAsync(blockHeader, transactions);
                var chain = await _blockchainService.CreateChainAsync(block.Block, transactions);
                var blockExecutionResult = await _blockchainExecutingService.ExecuteBlocksAsync(new[] {block.Block});
                await _blockExecutionResultProcessingService.ProcessBlockExecutionResultAsync(chain, blockExecutionResult);

                return await _blockchainService.GetChainAsync();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Create new chain failed.");
                throw;
            }
        }
    }
}