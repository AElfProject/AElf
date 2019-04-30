﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using Google.Protobuf;
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
        private readonly IBlockchainExecutingService _blockchainExecutingService;
        public ILogger<ChainCreationService> Logger { get; set; }

        public ILocalEventBus LocalEventBus { get; set; }

        public ChainCreationService(IBlockchainService blockchainService, IBlockExecutingService blockExecutingService,
            IBlockchainExecutingService blockchainExecutingService)
        {
            _blockchainService = blockchainService;
            _blockExecutingService = blockExecutingService;
            _blockchainExecutingService = blockchainExecutingService;
            Logger = NullLogger<ChainCreationService>.Instance;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        /// <summary>
        /// Creates a new chain with the provided and Smart Contract Zero.
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
                    Height = KernelConstants.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Empty,
                    Time = Timestamp.FromDateTime(DateTime.MinValue.ToUniversalTime()),
                    ChainId = _blockchainService.GetChainId()
                };

                var block = await _blockExecutingService.ExecuteBlockAsync(blockHeader, genesisTransactions);
                var chain = await _blockchainService.CreateChainAsync(block);
                await _blockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, BlockAttachOperationStatus.LongestChainFound);

                return await _blockchainService.GetChainAsync();
            }
            catch (Exception e)
            {
                Logger.LogError("CreateNewChainAsync Error: " + e);
                throw;
            }
        }
    }
}