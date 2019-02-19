using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Services;
using AElf.SmartContract;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.ChainController
{
    public class ChainCreationService : IChainCreationService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractService _smartContractService;
        public ILogger<ChainCreationService> Logger {get;set;}

        public ChainCreationService(IBlockchainService blockchainService, ISmartContractService smartContractService)
        {
            _blockchainService = blockchainService;
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

                await _blockchainService.CreateChainAsync(chainId, builder.Block);
                return await _blockchainService.GetChainAsync(chainId);

            }
            catch (Exception e)
            {
                Logger.LogError("CreateNewChainAsync Error: " + e);
                throw;
            }
        }
    }
}