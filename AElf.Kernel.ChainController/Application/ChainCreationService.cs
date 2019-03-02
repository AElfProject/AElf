using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Helpers;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Type = System.Type;

namespace AElf.Kernel.ChainController.Application
{
    public class ChainCreationService : IChainCreationService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockExecutingService _blockExecutingService;
        public ILogger<ChainCreationService> Logger { get; set; }

        public ChainCreationService(IBlockchainService blockchainService, IBlockExecutingService blockExecutingService)
        {
            _blockchainService = blockchainService;
            _blockExecutingService = blockExecutingService;
            Logger = NullLogger<ChainCreationService>.Instance;
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
                    Height = ChainConsts.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Genesis,
                    Time = Timestamp.FromDateTime(DateTime.UtcNow)
                };

                var block = await _blockExecutingService.ExecuteBlockAsync(blockHeader, genesisTransactions);

                await _blockchainService.CreateChainAsync(block);
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