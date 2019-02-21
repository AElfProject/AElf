using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Helpers;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.ChainController.Application
{
    public class ChainCreationService : IChainCreationService
    {
        private readonly ISmartContractService _smartContractService;
        private readonly IBlockchainService _blockchainService;
        public ILogger<ChainCreationService> Logger { get; set; }

        public ChainCreationService(ISmartContractService smartContractService, IBlockchainService blockchainService)
        {
            _smartContractService = smartContractService;
            _blockchainService = blockchainService;
            Logger = NullLogger<ChainCreationService>.Instance;
        }

        /// <summary>
        /// Creates a new chain with the provided ChainId and Smart Contract Zero.
        /// </summary>
        /// <returns>The new chain async.</returns>
        /// <param name="chainId">The new chain id which will be derived from the creator address.</param>
        /// <param name="smartContractRegistrationList">The smart contract registration containing the code of the SmartContractZero.</param>
        public async Task<Chain> CreateNewChainAsync(int chainId,
            List<SmartContractRegistration> smartContractRegistrationList)
        {
            try
            {
                // TODO: Centralize this function in Hash class
                // SmartContractZero address can be derived from ChainId

//                var zeroRegistration = smartContractRegistrationList.Find(s => s.SerialNumber == 0);
//                await _smartContractService.DeployZeroContractAsync(chainId, zeroRegistration);

                /*
                foreach (var reg in smartContractRegistration)
                {
                    if (reg.SerialNumber != 0)
                    {
                        await _smartContractService.DeploySystemContractAsync(chainId, reg);
                    }
                }*/

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