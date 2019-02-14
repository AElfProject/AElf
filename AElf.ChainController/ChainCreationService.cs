using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.SmartContract;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.ChainController
{
    public class ChainCreationService : IChainCreationService
    {
        private readonly IChainService _chainService;
        private readonly ISmartContractService _smartContractService;
        public ILogger<ChainCreationService> Logger {get;set;}

        public ChainCreationService(IChainService chainService, ISmartContractService smartContractService)
        {
            _chainService = chainService;
            _smartContractService = smartContractService;
            Logger = NullLogger<ChainCreationService>.Instance;
        }

        /// <summary>
        /// Creates a new chain with the provided ChainId and Smart Contract Zero.
        /// </summary>
        /// <returns>The new chain async.</returns>
        /// <param name="chainId">The new chain id which will be derived from the creator address.</param>
        /// <param name="smartContractRegistration">The smart contract registration containing the code of the SmartContractZero.</param>
        public async Task<IChain> CreateNewChainAsync(int chainId, List<SmartContractRegistration> smartContractRegistration)
        {
            try
            {
                // TODO: Centralize this function in Hash class
                // SmartContractZero address can be derived from ChainId

                var zeroRegistration = smartContractRegistration.Find(s => s.SerialNumber == 0);
                await _smartContractService.DeployZeroContractAsync(chainId, zeroRegistration);
                
                foreach (var reg in smartContractRegistration)
                {
                    if (reg.SerialNumber != 0)
                    {
                        await _smartContractService.DeploySystemContractAsync(chainId, reg);
                    }
                }

                var builder = new GenesisBlockBuilder();
                builder.Build(chainId);

                // add block to storage
                var blockchain = _chainService.GetBlockChain(chainId);
                await blockchain.AddBlocksAsync(new List<IBlock> {builder.Block});
                var chain = new Chain
                {
                    GenesisBlockHash = await blockchain.GetCurrentBlockHashAsync(),
                    Id = chainId
                };
                return chain;
            }
            catch (Exception e)
            {
                Logger.LogError("CreateNewChainAsync Error: " + e);
                throw;
            }
        }
    }
}